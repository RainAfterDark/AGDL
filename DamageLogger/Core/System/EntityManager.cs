using System.Collections.ObjectModel;
using Common;
using Common.Protobuf;
using DamageLogger.Core.Entity;
using DamageLogger.Data;
using DNToolKit.AnimeGame;

namespace DamageLogger.Core.System;

public class EntityManager
{
    private readonly Dictionary<uint, BaseEntity> _entities = new();
    private readonly List<AvatarEntity> _currentTeam = new();

    // There should only be one instance of these entities
    public readonly BasicEntity LevelEntity = new("Level");
    public readonly BasicEntity TeamEntity = new("Team");
    public readonly BasicEntity MpLevelEntity = new("MPLevel");

    public event EventHandler<BaseEntity>? EntityAdded;
    public event EventHandler<AvatarEntity>? AvatarSwapped;
    public event EventHandler<ReadOnlyCollection<AvatarEntity>>? PreTeamUpdate;
    public event EventHandler<ReadOnlyCollection<AvatarEntity>>? PostTeamUpdate;
    
    public AvatarEntity? CurrentAvatar { get; private set; }
    public ReadOnlyCollection<AvatarEntity> CurrentTeam => _currentTeam.AsReadOnly();

    private void AddEntity(BaseEntity entity)
    {
        _entities[entity.EntityId] = entity;
        EntityAdded?.Invoke(this, entity);
    }

    public ProtEntityType GetEntityType(uint entityId)
    {
        return (ProtEntityType)(entityId >> 24);
    }

    public BaseEntity? GetEntity(uint entityId)
    {
        if (entityId == GameData.LevelEntityId)
            return LevelEntity;
        switch (GetEntityType(entityId))
        {
            case ProtEntityType.Team:
                return TeamEntity;
            case ProtEntityType.MpLevel:
                return MpLevelEntity;
            default:
                _entities.TryGetValue(entityId, out var entity);
                return entity;
        }
    }

    public BaseEntity GetRootOwnerEntity(BaseEntity entity)
    {
        while (true)
        {
            if (entity.OwnerId is null) return entity;
            var owner = GetEntity(entity.OwnerId.Value);
            if (owner is null) return entity;
            entity = owner;
        }
    }

    private void ProcessSceneTeamUpdateNotify(SceneTeamUpdateNotify notify)
    {
        PreTeamUpdate?.Invoke(this, CurrentTeam);
        _currentTeam.Clear();
        foreach (var avatar in notify.SceneTeamAvatarList)
        {
            if (avatar.AbilityControlBlock is null || 
                avatar.AbilityControlBlock.AbilityEmbryoList.Count == 0)
                return;
            var avatarEntity = new AvatarEntity(avatar);
            AddEntity(avatarEntity);
            _currentTeam.Add(avatarEntity);
            AddEntity(new WeaponEntity(avatar.SceneEntityInfo.Avatar.Weapon, avatar.EntityId));
        }
        PostTeamUpdate?.Invoke(this, CurrentTeam);
    }

    private void ProcessSceneEntityAppearNotify(SceneEntityAppearNotify notify)
    {
        foreach (var entity in notify.EntityList)
        {
            switch (entity.EntityCase)
            {
                case SceneEntityInfo.EntityOneofCase.Avatar:
                    CurrentAvatar = GetEntity(entity.EntityId) as AvatarEntity;
                    if (CurrentAvatar is not null)
                        AvatarSwapped?.Invoke(this, CurrentAvatar);
                    break;
                case SceneEntityInfo.EntityOneofCase.Monster:
                    AddEntity(new MonsterEntity(entity.EntityId, entity.Monster));
                    foreach (var weapon in entity.Monster.WeaponList)
                        AddEntity(new WeaponEntity(weapon, entity.EntityId));
                    break;
                case SceneEntityInfo.EntityOneofCase.Gadget:
                    AddEntity(new GadgetEntity(entity.EntityId, entity.Gadget.GadgetId, entity.Gadget.OwnerEntityId));
                    break;
            }
        }
    }

    private void ProcessEvtCreateGadgetNotify(EvtCreateGadgetNotify notify)
    {
        AddEntity(new GadgetEntity(notify.EntityId, notify.ConfigId, notify.OwnerEntityId));
    }

    private void ProcessAbilityChangeNotify(AbilityChangeNotify notify)
    {
        GetEntity(notify.EntityId)?.AbilityManager.UpdateAbilities(notify.AbilityControlBlock);
    }

    public void OnPacketReceived(object? _, AnimeGamePacket packet)
    {
        if (packet.ProtoBuf is null) return;
        switch (packet.PacketType)
        {
            case Opcode.SceneTeamUpdateNotify:
                ProcessSceneTeamUpdateNotify((SceneTeamUpdateNotify)packet.ProtoBuf);
                break;
            case Opcode.SceneEntityAppearNotify:
                ProcessSceneEntityAppearNotify((SceneEntityAppearNotify)packet.ProtoBuf);
                break;
            case Opcode.EvtCreateGadgetNotify:
                ProcessEvtCreateGadgetNotify((EvtCreateGadgetNotify)packet.ProtoBuf);
                break;
            case Opcode.AbilityChangeNotify:
                ProcessAbilityChangeNotify((AbilityChangeNotify)packet.ProtoBuf);
                break;
        }
    }
}