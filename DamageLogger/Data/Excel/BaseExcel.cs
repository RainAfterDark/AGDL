namespace DamageLogger.Data.Excel;

public abstract class BaseExcel<T> : IExcel
{
    public virtual uint Id { get; init; }
    public virtual string Name => Id.ToString();
    public static List<T> DataList { get; set; } = new();
    public static Dictionary<uint, T> DataDict { get; } = new();
}

public interface IExcel
{
    public uint Id { get; init; }
    public string Name { get; }
}