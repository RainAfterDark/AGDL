using System.Collections.ObjectModel;

namespace DamageLogger.Core.Combat;

public class CombatManager
{
    private readonly Dictionary<string, (float damage, int count)> _damageDealtMap = new();

    public ReadOnlyDictionary<string, (float damage, int count)> DamageDealtMap => _damageDealtMap.AsReadOnly();
    public float TotalDamageDealt { get; private set; }
    public float TotalDamageTaken { get; private set; }

    public void DealDamage(float damage, string damageSource)
    {
        _damageDealtMap.TryAdd(damageSource, (0, 0));
        var data = _damageDealtMap[damageSource];
        data.damage += damage;
        data.count += 1;
        _damageDealtMap[damageSource] = data;
        TotalDamageDealt += damage;
    }
    
    public void TakeDamage(float damage) => TotalDamageTaken += damage;

    public void Reset()
    {
        _damageDealtMap.Clear();
        TotalDamageDealt = 0;
        TotalDamageTaken = 0;
    }
}