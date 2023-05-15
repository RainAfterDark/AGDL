using DamageLogger.Data.Excel;

namespace DamageLogger.Core.Entity;

public class BasicEntity : BaseEntity
{
    public override IExcel? Data => null;
    public override string Name { get; }

    public BasicEntity(string name)
    {
        Name = name;
    }
}