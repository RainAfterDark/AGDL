using DamageLogger.Core.Ability;
using DamageLogger.Core.Combat;
using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public abstract class BaseEntity
{
    public uint EntityId { get; }
    public uint ConfigId { get; }
    public uint? OwnerId { get; protected init; }
    public abstract IExcel? Data { get; }
    public virtual string Name => Data?.Name ?? ConfigId.ToString();
    public AbilityManager AbilityManager { get; } = new();
    public CombatManager CombatManager { get; } = new();

    protected BaseEntity(uint entityId, uint configId)
    {
        EntityId = entityId;
        ConfigId = configId;
    }
}