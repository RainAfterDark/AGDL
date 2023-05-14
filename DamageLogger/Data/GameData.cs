using Common.Protobuf;
using Serilog;

namespace DamageLogger.Data;

public static class GameData
{
    public const uint LevelEntityId = 327155713;
    public static Dictionary<uint, string> StringHashes { get; set; } = new();
    public static Dictionary<uint, string> TextMap { get; set; } = new();
    
    public static string ResolveName(uint nameTextMapHash, uint id, params string?[] fallbackNames)
    {
        if (TextMap.TryGetValue(nameTextMapHash, out var textMapName))
            return textMapName;
        foreach (var name in fallbackNames)
        {
            if (name is not null && name != "")
                return name;
        }
        return id.ToString();
    }

    public static string GetStringFromHash(uint hash)
    {
        StringHashes.TryGetValue(hash, out var abilityName);
        if (abilityName is null)
            Log.Warning("No string for hash {Hash}", hash);
        return abilityName ?? hash.ToString();
    }

    public static string GetStringFromHash(AbilityString abilityString)
    {
        return abilityString.TypeCase == AbilityString.TypeOneofCase.Str
            ? abilityString.Str
            : GetStringFromHash(abilityString.Hash);
    }
}