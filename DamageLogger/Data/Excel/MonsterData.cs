using DamageLogger.Data.Enums;

namespace DamageLogger.Data.Excel;

[ResourcePath("Excel.MonsterExcelConfigData.json")]
public class MonsterData : BaseExcel<MonsterData>
{
    public string? MonsterName { get; init; }
    public MonsterType Type { get; init; }
    public string? Skin { get; init; }
    public uint DescribeId { get; init; }
    public override uint Id { get; init; }
    public uint NameTextMapHash { get; init; }

    public override string Name
    {
        get
        {
            MonsterDescribeData.DataDict.TryGetValue(DescribeId, out var describeData);
            return GameData.ResolveName(NameTextMapHash, Id, describeData?.Name, MonsterName, Skin);
        }
    }
}