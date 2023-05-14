namespace DamageLogger.Data.Excel;

[ResourcePath("Excel.WeaponExcelConfigData.json")]
public class WeaponData : BaseExcel<WeaponData>
{
    public override uint Id { get; init; }
    public uint NameTextMapHash { get; init; }
    public string? Icon { get; init; }
    public uint GadgetId { get; init; }
    public override string Name => GameData.ResolveName(NameTextMapHash, Id, Icon);
}