using System.Collections;
using System.Reflection;
using DamageLogger.Data.Excel;
using Newtonsoft.Json;
using Serilog;

namespace DamageLogger.Data;

public static class ResourceLoader
{
    private static object? DeserializeResource(string path, Type? type)
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream($"DamageLogger.Resources.{path}");
        if (stream == null) return default;
        using var streamReader = new StreamReader(stream);
        using var jsonTextReader = new JsonTextReader(streamReader);
        var serializer = new JsonSerializer();
        return serializer.Deserialize(jsonTextReader, type);
    }
    
    private static T? DeserializeResource<T>(string path)
    {
        var obj = DeserializeResource(path, typeof(T));
        return obj is not null ? (T)obj : default;
    }

    private static void LoadExcels()
    {
        var excelTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(type => type.BaseType is not null && type.BaseType.IsGenericType &&
                           type.BaseType.GetGenericTypeDefinition() == typeof(BaseExcel<>));

        foreach (var excel in excelTypes)
        {
            var resourcePath = excel.GetCustomAttribute<ResourcePathAttribute>()!.Value;
            const BindingFlags dataFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy;
            var dataList = excel.GetProperty("DataList", dataFlags);
            var listType = typeof(List<>).MakeGenericType(excel);
            var parsedList = DeserializeResource(resourcePath, listType) as IList;
            dataList!.SetValue(null, parsedList);

            const BindingFlags idFlags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public;
            var idProp = excel.GetProperty("Id",  idFlags);
            if (idProp is null) continue; // Not all excels might have Id
            var dataDict = excel.GetProperty("DataDict", dataFlags)!.GetValue(null) as IDictionary;
            foreach (var entry in parsedList!)
            {
                var id = idProp.GetValue(entry)!;
                dataDict!.Add(id, entry);
            }
        }
    }

    private static void LoadGameData()
    {
        GameData.StringHashes = DeserializeResource<Dictionary<uint, string>>("StringHashes.json")!;
        GameData.TextMap = DeserializeResource<Dictionary<uint, string>>("TextMap.TextMapEN.json")!;
    }

    public static void LoadAll()
    {
        LoadExcels();
        LoadGameData();
        Log.Information("Finished loading resources");
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class ResourcePathAttribute : Attribute
{
    public readonly string Value;

    public ResourcePathAttribute(string name)
    {
        Value = name;
    }
}