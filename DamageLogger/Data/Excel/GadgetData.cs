using DamageLogger.Data.Enums;

namespace DamageLogger.Data.Excel;

[ResourcePath("Excel.GadgetExcelConfigData.json")]
public class GadgetData : BaseExcel<GadgetData>
{
    public EntityType? Type { get; init; }
    public string? JsonName { get; init; }
    public string? ItemJsonName { get; init; }
    public override uint Id { get; init; }
    public uint NameTextMapHash { get; init; }
    public string? LodPatternName { get; init; }
    public override string Name => GameData.ResolveName(NameTextMapHash, Id, JsonName, LodPatternName, ItemJsonName);
}