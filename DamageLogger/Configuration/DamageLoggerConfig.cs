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

    public string LogDirectory { get; set; } = "DamageLogs";
    public ConsoleLogMode ConsoleLoggingMode { get; set; } = ConsoleLogMode.Table;
    public bool LogCharacterSwap { get; set; } = false;
    public DamageToEntityFilter DamageToEntityFilters { get; set; } = new();
    
    public void Update(DamageLoggerConfig newConfig)
    {
        foreach (var property in typeof(DamageLoggerConfig).GetProperties().Where(p => p.CanWrite))
            property.SetValue(this, property.GetValue(newConfig, null), null);
    }
}