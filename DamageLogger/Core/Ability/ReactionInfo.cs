namespace DamageLogger.Core.Ability;

public class ReactionInfo : AbilityInfo
{
    public uint ApplyEntityId { get; }

    public ReactionInfo(uint applyEntityId, string abilityName, string abilityOverride) 
        : base(abilityName, abilityOverride)
    {
        ApplyEntityId = applyEntityId;
    }
}