using Common.Protobuf;
using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class MonsterEntity : BaseEntity
{
    public override MonsterData? Data { get; }

    public override string Name => $"{EntityId.ToString()[5..]} {Data?.Name}";

    public MonsterEntity(uint entityId, SceneMonsterInfo monster)
    {
        EntityId = entityId;
        MonsterData.DataDict.TryGetValue(monster.MonsterId, out var monsterData);
        Data = monsterData;
    }
}