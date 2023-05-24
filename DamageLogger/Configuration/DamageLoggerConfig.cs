using DNToolKit.Configuration.Models;

namespace DamageLogger.Configuration;

public class DamageLoggerConfig : Config
{
    public enum ConsoleLogMode
    {
        Friendly,
        Tsv,
        Table
    }
    
    public class DamageToEntityFilter
    {
        public bool ToMonsters { get; set; } = true;
        public bool ToAvatars { get; set; } = false;
        public bool ToGadgets { get; set; } = false;
        public bool ToWeapons { get; set; } = false;
    }

    /// <summary>
    /// The directory path to write damage logs.
    /// </summary>
    public string LogDirectory { get; set; } = "DamageLogs";
    
    /// <summary>
    /// The <see cref="ConsoleLogMode"/> to be used by the console logger.
    /// </summary>
    public ConsoleLogMode ConsoleLoggingMode { get; set; } = ConsoleLogMode.Table;
    
    /// <summary>
    /// Declares, if character swaps should be logged.
    /// </summary>
    public bool LogCharacterSwap { get; set; } = false;
    
    /// <summary>
    /// Declares, if the logger should also use <see cref="Common.Protobuf.AbilityMetaTriggerElementReaction"/>
    /// invocations to determine the reaction source. This may or may not affect its accuracy.
    /// </summary>
    public bool UseTriggerReactionInvocations { get; set; } = false;
    
    /// <summary>
    /// The entity type filters used by the logger.
    /// </summary>
    public DamageToEntityFilter DamageToEntityFilters { get; set; } = new();
    
    public void Update(DamageLoggerConfig newConfig)
    {
        foreach (var property in typeof(DamageLoggerConfig).GetProperties().Where(p => p.CanWrite))
            property.SetValue(this, property.GetValue(newConfig, null), null);
    }
}