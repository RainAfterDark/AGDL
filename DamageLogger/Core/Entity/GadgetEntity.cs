using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class GadgetEntity : BaseEntity
{
    public override GadgetData? Data { get; }

    public GadgetEntity(uint entityId, uint gadgetId, uint? ownerId)
    {
        EntityId = entityId;
        ConfigId = gadgetId;
        GadgetData.DataDict.TryGetValue(ConfigId, out var gadgetData);
        Data = gadgetData;
        OwnerId = ownerId;
    }
}