using Common.Protobuf;
using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class WeaponEntity : BaseEntity
{
    public override WeaponData? Data { get; }
    public GadgetData? GadgetData { get; }
    public uint GadgetId { get; }

    public override string Name => Data?.Name ?? GadgetData?.Name ??
        (ConfigId != 0
            ? ConfigId.ToString()
            : GadgetId.ToString());

    public WeaponEntity(SceneWeaponInfo weapon, uint ownerId)
        : base(weapon.EntityId, weapon.ItemId)
    {
        OwnerId = ownerId;
        WeaponData.DataDict.TryGetValue(ConfigId, out var weaponData);
        Data = weaponData;
        GadgetId = weapon.GadgetId;
        GadgetData.DataDict.TryGetValue(GadgetId, out var gadgetData);
        GadgetData = gadgetData;
    }
}