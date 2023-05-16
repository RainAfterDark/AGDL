using Common.Protobuf;
using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class AvatarEntity : BaseEntity
{
    public override AvatarData? Data { get; }

    public override string Name
    {
        get
        {
            var name = Data?.Name ?? ConfigId.ToString();
            if (!name.Contains(' ')) return name;
            var split = name.Split(' ');
            return split.First().Length <= 3 ? string.Join(' ', split) : split.Last();
        }
    }

    public AvatarEntity(SceneTeamAvatar avatar)
        : base(avatar.EntityId, avatar.SceneEntityInfo.Avatar.AvatarId)
    {
        AvatarData.DataDict.TryGetValue(ConfigId, out var avatarData);
        Data = avatarData;
        AbilityManager.UpdateAbilities(avatar.AbilityControlBlock);
    }
}