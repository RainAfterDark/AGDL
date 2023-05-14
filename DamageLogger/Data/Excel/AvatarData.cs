namespace DamageLogger.Data.Excel;

[ResourcePath("Excel.AvatarExcelConfigData.json")]
public class AvatarData : BaseExcel<AvatarData>
{
    public string? IconName { get; init; }
    public override uint Id { get; init; }
    public uint NameTextMapHash { get; init; }

    public override string Name => GameData.ResolveName(NameTextMapHash, Id, InternalName);
    private string? InternalName => IconName?.Replace("UI_AvatarIcon_", "");
}