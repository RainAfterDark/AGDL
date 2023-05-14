namespace DamageLogger.Data.Excel;

[ResourcePath("Excel.MonsterDescribeExcelConfigData.json")]
public class MonsterDescribeData : BaseExcel<MonsterDescribeData>
{
    public override uint Id { get; init; }
    public uint NameTextMapHash { get; init; }

    public override string Name => GameData.ResolveName(NameTextMapHash, Id);
}