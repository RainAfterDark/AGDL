using System.Globalization;
using Common.Protobuf;
using DamageLogger.Core.Entity;
using DamageLogger.Core.Logging;
using DamageLogger.Data.Enums.Friendly;
using Serilog;
using Spectre.Console;

namespace DamageLogger.Core.Combat;

public class HitInfo
{
    public string ReceiveTime { get; init; }
    public string Attacker { get; init; }
    public AttackType AttackType { get; init; }
    public string DamageSource { get; init; }
    public string Defender { get; init; }
    public float Damage { get; init; }
    public float DamageShield { get; init; }
    public bool IsCriticalHit { get; init; }
    public bool IsApplyElement { get; init; }
    public FriendlyElementType ElementType { get; init; }
    public FriendlyElementReactionType AmplifyType { get; init; }
    public uint AttackTimestamp { get; init; }
    
    private static readonly List<(string column, int length)> HeaderRowList = new()
    {
        ("Time", 8),
        ("Attacker", 10),
        ("Type", 8),
        ("Damage Source", 40),
        ("Defender", 20),
        ("Damage", 10),
        ("To Shield", 10),
        ("Crit", 5),
        ("Apply", 5),
        ("Element", 8),
        ("Amplify", 10),
        ("Timestamp", 10),
    };

    public HitInfo(BaseEntity attacker, AttackType attackType, string damageSource, 
        BaseEntity defender, AttackResult attackResult, DateTime receiveTime)
    {
        ReceiveTime = receiveTime.ToString("T", DateTimeFormatInfo.InvariantInfo);
        Attacker = attacker.Name;
        AttackType = attackType;
        DamageSource = damageSource;
        Defender = defender.Name;
        Damage = attackResult.Damage;
        DamageShield = attackResult.DamageShield;
        IsCriticalHit = attackResult.IsCrit;
        IsApplyElement = attackResult.ElementDurabilityAttenuation != 0;
        ElementType = (FriendlyElementType)attackResult.ElementType;
        AmplifyType = (FriendlyElementReactionType)attackResult.AmplifyReactionType;
        AttackTimestamp = attackResult.AttackTimestampMs;
    }

    public void LogFriendly()
    {
        var isCritical = IsCriticalHit ? "critical " : "";
        var amplifyType = AmplifyType != FriendlyElementReactionType.None ? $" ({AmplifyType})" : "";
        Log.Information("{Attacker} used ({AttackType}) {DamageSource} to deal {Damage:0.##}" +
                        " {IsCritical}damage of {ElementType}{AmplifyType} type to {Defender}", 
            Attacker, AttackType, DamageSource, Damage, isCritical, amplifyType, ElementType, Defender);
    }

    private IEnumerable<string> ToRowList()
    {
        return new List<string>
        {
            ReceiveTime,
            Attacker,
            AttackType.ToString(),
            DamageSource,
            Defender,
            Damage.ToString("N"),
            DamageShield.ToString("N"),
            IsCriticalHit.ToString(),
            IsApplyElement.ToString(),
            ElementType.ToString(),
            AmplifyType.ToString(),
            AttackTimestamp.ToString(),
        };
    }

    public string ToTsvRowString()
    {
        return string.Join('\t', ToRowList());
    }

    private static string RowListToString(IEnumerable<string> rowList, Color color)
    {
        return "│ " + string.Join(" │ ", rowList.Select((column, i) =>
        {
            var length = HeaderRowList[i].length;
            if (column.Length > length)
                column = column[..(length - 1)] + "-";
            else if (column.Length < length)
                column = column.PadRight(length);
            return $"[{color.ToMarkup()}]{column}[/]";
        })) + " │";
    }

    public static string GetHeaderRow()
    {
        var headerRow = 
            RowListToString(HeaderRowList.Select(i => i.column), Color.Default);
        return $"[invert]{headerRow}[/]";
    }

    public string ToTableRowString()
    {
        return RowListToString(ToRowList(), Theme.GetColorFromElement(ElementType));
    }
}