using System.Collections.ObjectModel;
using Common;
using Common.Protobuf;
using DamageLogger.Configuration;
using DamageLogger.Core.Ability;
using DamageLogger.Core.Combat;
using DamageLogger.Core.Entity;
using DamageLogger.Core.Logging;
using DamageLogger.Core.System;
using DamageLogger.Data;
using DNToolKit.AnimeGame;
using DNToolKit.AnimeGame.Models;
using DNToolKit.Configuration;
using Google.Protobuf.Collections;
using Serilog;

namespace DamageLogger;

public class DamageLogger
{
    private readonly DamageLoggerConfig _config;
    private readonly ConsoleLogger _consoleLogger;
    private readonly FileLogger _fileLogger;
    private readonly DNToolKit.DNToolKit _dnToolKit;
    private readonly EntityManager _entityManager;
    private readonly CommandManager _commandManager;

    private readonly string _configPath;
    private uint _firstHitTimestamp;
    private uint _lastHitTimestamp;
    private bool _isLogging = true;

    private uint TotalLoggingTimeMs => _lastHitTimestamp - _firstHitTimestamp;
    private float TotalLoggingTimeSeconds => (float)TotalLoggingTimeMs / 1000;
    
    public bool PlayerLoggedIn { get; private set; }
    public bool IsLogging
    {
        get => _isLogging;
        set
        {
            _isLogging = value;
            _consoleLogger.IsLogging = value;
        }
    }

    public DamageLogger(string configPath)
    {
        _configPath = configPath;
        _config = ConfigurationProvider.LoadConfig<DamageLoggerConfig>(configPath);
        ConfigurationProvider.SaveConfig(_config);
        Log.Information("Config file is at: {Path}", 
            Path.Combine(Directory.GetCurrentDirectory(), configPath));
        
        _commandManager = new CommandManager(this);
        _consoleLogger = new ConsoleLogger(_config, _commandManager.CommandsText);
        _fileLogger = new FileLogger(_config);

        _entityManager = new EntityManager();
        _entityManager.AvatarSwapped += OnAvatarSwapped;
        _entityManager.PreTeamUpdate += OnPreTeamUpdate;
        _entityManager.PostTeamUpdate += OnPostTeamUpdate;

        _dnToolKit = new DNToolKit.DNToolKit(_config);
        _dnToolKit.KeyFound += OnKeyFound;
        _dnToolKit.Disconnected += OnDisconnection;
        _dnToolKit.PacketReceived += OnPacketReceived;
        _dnToolKit.PacketReceived += _entityManager.OnPacketReceived;
        ResourceLoader.LoadAll();
    }

    public void Run()
    {
        Console.CancelKeyPress += (_, __) => Close();
        _dnToolKit.RunAsync().ConfigureAwait(false);
        _commandManager.RunLoop();
    }

    public void Close()
    {
        _dnToolKit.Close();
        _commandManager.Close();
    }

    public void RenderDamageBreakdown()
    {
        if (_entityManager.CurrentTeam.Count == 0) return;
        _consoleLogger.RenderDamageBreakdown(_entityManager.CurrentTeam, TotalLoggingTimeSeconds);
        if (_fileLogger.AbsFilePath is null) return;
        Log.Information("Logs written to: {LogFilePath}", _fileLogger.AbsFilePath);
    }

    public void ResetCurrentLog()
    {
        foreach (var avatar in _entityManager.CurrentTeam) avatar.CombatManager.Reset();
        _consoleLogger.UpdateCurrentTeamDamageText(_entityManager.CurrentTeam, TotalLoggingTimeSeconds);
        Log.Information("Stats for current team has been reset");
        _fileLogger.UpdateTeam(_entityManager.CurrentTeam);
    }

    public void ReloadConfig()
    {
        var newConfig = ConfigurationProvider.LoadConfig<DamageLoggerConfig>(_configPath);
        _config.Update(newConfig);
        Log.Information("Config reloaded");
    }

    private bool IsEntityFiltered(BaseEntity? entity)
    {
        var shouldLog = _config.DamageToEntityFilters;
        return (entity is AvatarEntity && 
                (!shouldLog.ToAvatars || !_entityManager.CurrentTeam.Contains(entity)))
               || (entity is MonsterEntity && !shouldLog.ToMonsters)
               || (entity is GadgetEntity && !shouldLog.ToGadgets)
               || (entity is WeaponEntity && !shouldLog.ToWeapons);
    }

    private void ProcessEvtBeingHitInfo(EvtBeingHitInfo info)
    {
        var receiveTime = DateTime.Now;
        if (!IsLogging) return;
        
        var attackResult = info.AttackResult!;
        var defender = _entityManager.GetEntity(attackResult.DefenseId);
        if (defender is null || IsEntityFiltered(defender)) return;
        
        var ability = attackResult.AbilityIdentifier;
        var caster = ability is not null
            ? _entityManager.GetEntity(ability.AbilityCasterId)
            : null;
        
        BaseEntity? attacker = null;
        ReactionInfo? reactionInfo = null;
        
        // This reaction source resolution method is still hugely experimental
        if (caster == _entityManager.LevelEntity)
        {
            var modifierOwner = _entityManager.GetEntity(ability!.ModifierOwnerId);
            reactionInfo = modifierOwner?.AbilityManager
                .GetReactionInfo(ability.InstancedAbilityId, ability.InstancedModifierId);

            uint? reactionSourceId = reactionInfo is not null
                // Use the SourceCasterId (updated by UpdateBaseReactionDamage)
                ? modifierOwner?.AbilityManager.GetReactionSourceId(reactionInfo) 
                  // If not set (e.g. when electro-charged bounces),
                  // use the one from ApplyEntityId (updated by ModifierChange)
                  ?? _entityManager.GetEntity(reactionInfo.ApplyEntityId)?
                      .AbilityManager.GetReactionSourceId(reactionInfo)
                  // If still not set, fallback to just using the ApplyEntityId
                  ?? reactionInfo.ApplyEntityId
                // Finally, just give up (it shouldn't get here)
                : null;
            
            // Some more stuff to mention: the flaw with solely relying on ApplyEntityId is that
            // in the case of electro-charged bouncing, it will point to the immediate entity it bounced from
            // rather than the source of the damage. The SourceCasterId given by UpdateBaseReactionDamage
            // seems to be more reliable, but it still doesn't perfectly track some electro-charged ticks.
            // However, it does seem that the game server does its own damage (re)calculation, so in theory,
            // being able to perfectly map reaction damage to the actual source should be possible.
            
            if (reactionSourceId is not null)
                attacker = _entityManager.GetEntity(reactionSourceId.Value);
        }
        
        attacker ??= _entityManager.GetEntity(attackResult.AttackerId);
        if (attacker is null) return;
        var originalAttacker = attacker;
        attacker = _entityManager.GetRootOwnerEntity(attacker);
        // Some late hits can appear when the team has already switched, so we prevent that
        // Usually happens in abyss (going from the 1st to 2nd half)
        if (attacker is AvatarEntity && !_entityManager.CurrentTeam.Contains(attacker)) return;
        
        // On first hit
        if (_firstHitTimestamp == 0)
        {
            _firstHitTimestamp = attackResult.AttackTimestampMs;
            _fileLogger.UpdateTeam(_entityManager.CurrentTeam);
            if (_config.ConsoleLoggingMode == DamageLoggerConfig.ConsoleLogMode.Table)
                _consoleLogger.RenderHitInfoHeader();
        }
        _lastHitTimestamp = attackResult.AttackTimestampMs;
        
        string? damageSource = null;
        var attackType = AttackType.Unknown;
        
        if (reactionInfo is not null)
        {
            damageSource = reactionInfo.AbilityName;
            attackType = AttackType.Reaction;
        }
        else if (originalAttacker != attacker)
        {
            damageSource = originalAttacker.Name;
            attackType = AttackType.Gadget;
        }
        else if (attackResult.HashedAnimEventId != 0)
        {
            damageSource = GameData.GetStringFromHash(attackResult.HashedAnimEventId);
            attackType = AttackType.Normal;
        }
        else if (ability is not null)
        {
            damageSource = caster is GadgetEntity ? caster.Name 
                : attacker.AbilityManager.GetAbility(ability.InstancedAbilityId)?.AbilityName;
            attackType = caster is GadgetEntity ? AttackType.Gadget 
                : damageSource is not null ? AttackType.Ability : AttackType.Unknown;
        }
        damageSource ??= "Unknown";
        
        attacker.CombatManager.DealDamage(attackResult.Damage, damageSource);
        defender.CombatManager.TakeDamage(attackResult.Damage);
        _consoleLogger.UpdateCurrentTeamDamageText(_entityManager.CurrentTeam, TotalLoggingTimeSeconds);
        
        var hitInfo = new HitInfo(attacker, attackType, damageSource, defender, attackResult, receiveTime);
        _consoleLogger.RenderHitInfo(hitInfo);
        _fileLogger.LogHitInfo(hitInfo);
    }

    private void ProcessCombatInvocations(RepeatedField<CombatInvokeEntry> invokes)
    {
        foreach (var entry in invokes)
        {
            switch (entry.ArgumentType)
            {
                case CombatTypeArgument.CombatEvtBeingHit:
                    ProcessEvtBeingHitInfo(EvtBeingHitInfo.Parser.ParseFrom(entry.CombatData));
                    break;
            }
        }
    }
    
    private void ProcessAbilityMetaAddNewAbility(AbilityInvokeEntry entry)
    {
        var data = AbilityMetaAddAbility.Parser.ParseFrom(entry.AbilityData);
        _entityManager.GetEntity(entry.EntityId)?.AbilityManager.AddAbility(data);
    }

    private void ProcessAbilityMetaModifierChange(AbilityInvokeEntry entry)
    {
        var head = entry.Head;
        if (head is null 
            || entry.AbilityData.IsEmpty 
            || head.TargetId is not GameData.LevelEntityId) // Only reactions will pass this
            return;
        var data = AbilityMetaModifierChange.Parser.ParseFrom(entry.AbilityData);
        _entityManager.GetEntity(entry.EntityId)?.AbilityManager.UpdateReactionModifier(head, data);
    }

    private void ProcessAbilityMetaUpdateBaseReactionDamage(AbilityInvokeEntry entry)
    {
        var data = AbilityMetaUpdateBaseReactionDamage.Parser.ParseFrom(entry.AbilityData);
        _entityManager.GetEntity(entry.EntityId)?.AbilityManager.UpdateReactionSource(data);
    }

    private void ProcessAbilityMetaTriggerElementReaction(AbilityInvokeEntry entry)
    {
        var data = AbilityMetaTriggerElementReaction.Parser.ParseFrom(entry.AbilityData);
        _entityManager.GetEntity(entry.EntityId)?.AbilityManager.UpdateReactionSource(data);
    }

    private void ProcessAbilityInvocations(RepeatedField<AbilityInvokeEntry> invokes)
    {
        foreach (var entry in invokes)
        {
            switch (entry.ArgumentType)
            {
                case AbilityInvokeArgument.AbilityMetaAddNewAbility:
                    ProcessAbilityMetaAddNewAbility(entry);
                    break;
                case AbilityInvokeArgument.AbilityMetaModifierChange:
                    ProcessAbilityMetaModifierChange(entry);
                    break;
                case AbilityInvokeArgument.AbilityMetaUpdateBaseReactionDamage:
                    ProcessAbilityMetaUpdateBaseReactionDamage(entry);
                    break;
                case AbilityInvokeArgument.AbilityMetaTriggerElementReaction:
                    // May or may not be needed
                    //ProcessAbilityMetaTriggerElementReaction(entry);
                    break;
            }
        }
    }

    private void OnKeyFound(object? _, long __)
    {
        Log.Information("If you don't see a player log-in message right after this, please re-login");
    }

    private void OnDisconnection(object? _, EventArgs __)
    {
        PlayerLoggedIn = false;
    }

    private void OnPacketReceived(object? _, AnimeGamePacket packet)
    {
        if (packet.ProtoBuf is null) return;
        switch (packet.PacketType)
        {
            case Opcode.PlayerLoginRsp:
                PlayerLoggedIn = true;
                Log.Information("Player logged in!");
                break;
            case Opcode.ClientAbilityChangeNotify:
                ProcessAbilityInvocations(((ClientAbilityChangeNotify)packet.ProtoBuf).Invokes);
                break;
            case Opcode.AbilityInvocationsNotify:
                ProcessAbilityInvocations(((AbilityInvocationsNotify)packet.ProtoBuf).Invokes);
                break;
            case Opcode.CombatInvocationsNotify:
                if (packet.Sender != Sender.Server) break;
                ProcessCombatInvocations(((CombatInvocationsNotify)packet.ProtoBuf).InvokeList);
                break;
        }
    }
    
    private void OnAvatarSwapped(object? _, AvatarEntity avatar)
    {
        if (_config.LogCharacterSwap)
            Log.Information("Swap to {Avatar}", avatar.Name);
    }

    private void OnPreTeamUpdate(object? _, ReadOnlyCollection<AvatarEntity> previousTeam)
    {
        if (_firstHitTimestamp != 0)
            RenderDamageBreakdown();
        _firstHitTimestamp = 0;
        _lastHitTimestamp = 0;
    }

    private void OnPostTeamUpdate(object? _, ReadOnlyCollection<AvatarEntity> newTeam)
    {
        Log.Information("Team Update: {Team}",
            string.Join(", ", newTeam.Select(avatar => avatar.Name)));
        _consoleLogger.UpdateCurrentTeamDamageText(newTeam, TotalLoggingTimeSeconds);
    }
}