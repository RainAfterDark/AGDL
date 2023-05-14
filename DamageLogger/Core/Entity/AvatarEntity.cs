using Common.Protobuf;
using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class AvatarEntity : BaseEntity
{
    private readonly Dictionary<uint, string> _abilities = new();
    
    public override AvatarData? Data { get; }

    public override string Name
    {
        get
        {
            var name = Data?.Name ?? ConfigId.ToString();
            if (!name.Contains(" ")) return name;
            var split = name.Split(" ");
            return split[0].Length <= 3 ? string.Join(" ", split) : split[1];
        }
    }

    public AvatarEntity(SceneTeamAvatar avatar)
    {
        EntityId = avatar.EntityId;
        ConfigId = avatar.SceneEntityInfo.Avatar.AvatarId;
        AvatarData.DataDict.TryGetValue(ConfigId, out var avatarData);
        Data = avatarData;
        AbilityManager.UpdateAbilities(avatar.AbilityControlBlock);
    }
}