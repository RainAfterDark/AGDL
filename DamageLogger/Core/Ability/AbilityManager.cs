using Common.Protobuf;
using DamageLogger.Data;
using DamageLogger.Data.Enums;
using Serilog;

namespace DamageLogger.Core.Ability;

public class AbilityManager
{
    private readonly Dictionary<uint, AbilityInfo> _abilityMap = new();
    private readonly Dictionary<string, uint> _reactionSourceMap = new();
    private readonly Dictionary<(uint instancedAbilityId, uint instancedModifierId), ReactionInfo> _reactionModifierMap = new();

    private void AddAbility(uint index, string abilityName, string overrideName)
    {
        _abilityMap[index] = new AbilityInfo(abilityName, overrideName);
        if (overrideName != "Default")
            Log.Debug("Non-default override {Override} for ability {Ability}", 
                overrideName, abilityName);
    }
    
    public void AddAbility(AbilityMetaAddAbility data)
    {
        var abilityName = GameData.GetStringFromHash(data.Ability.AbilityName);
        var overrideName = GameData.GetStringFromHash(data.Ability.AbilityOverride);
        AddAbility(data.Ability.InstancedAbilityId, abilityName, overrideName);
    }

    public void UpdateAbilities(AbilityControlBlock controlBlock)
    {
        foreach (var embryo in controlBlock.AbilityEmbryoList)
        {
            var abilityName = GameData.GetStringFromHash(embryo.AbilityNameHash);
            var overrideName = GameData.GetStringFromHash(embryo.AbilityOverrideNameHash);
            AddAbility(embryo.AbilityId, abilityName, overrideName);
        }
    }

    public AbilityInfo? GetAbility(uint index)
    {
        _abilityMap.TryGetValue(index, out var ability);
        return ability;
    }

    public void UpdateReactionSource(AbilityMetaUpdateBaseReactionDamage data)
    {
        if (data.AbilityName is null) return;
        var abilityName = GameData.GetStringFromHash(data.AbilityName);
        _reactionSourceMap[abilityName] = data.SourceCasterId;
    }

    public void UpdateReactionSource(AbilityMetaTriggerElementReaction data)
    {
        // This is such a hack lol
        var abilityName = $"ElementAbility_{(ElementReactionType)data.ElementReactionType}";
        _reactionSourceMap[abilityName] = data.TriggerEntityId;
    }

    public uint? GetReactionSourceId(ReactionInfo reactionInfo)
    {
        return _reactionSourceMap.TryGetValue(reactionInfo.AbilityName, out var sourceEntityId)
            ? sourceEntityId : null;
    }

    public void UpdateReactionModifier(AbilityInvokeEntryHead head, AbilityMetaModifierChange data)
    {
        var abilityName = GameData.GetStringFromHash(data.ParentAbilityName);
        var overrideName = data.ParentAbilityOverride is not null
            ? GameData.GetStringFromHash(data.ParentAbilityOverride)
            : "Default";
        _reactionModifierMap[(head.InstancedAbilityId, head.InstancedModifierId)] =
            new ReactionInfo(data.ApplyEntityId, abilityName, overrideName);
    }

    public ReactionInfo? GetReactionInfo(uint instancedAbilityId, uint instancedModifierId)
    {
        _reactionModifierMap.TryGetValue((instancedAbilityId, instancedModifierId), out var reactionInfo);
        return reactionInfo;
    }
}