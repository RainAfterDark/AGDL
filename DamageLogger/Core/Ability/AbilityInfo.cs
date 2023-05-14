namespace DamageLogger.Core.Ability;

public class AbilityInfo
{
    public string OriginalAbilityName { get; }
    public string AbilityOverride { get; }
    public bool IsOverriden { get; }
    public string AbilityName => IsOverriden ? AbilityOverride : OriginalAbilityName;

    public AbilityInfo(string abilityName, string abilityOverride)
    {
        OriginalAbilityName = abilityName;
        AbilityOverride = abilityOverride;
        IsOverriden = AbilityOverride != "Default";
    }
}