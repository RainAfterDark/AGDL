using Google.Protobuf;
using System.Reflection;
using Common.Protobuf;

namespace Common;

public class ProtobufFactory
{
    private static readonly Dictionary<Opcode, Type> Opcode2TypeDict = new();
    private static readonly Dictionary<Opcode, MessageParser> Opcode2ParserDict = new();
    private static readonly Dictionary<string, MessageParser> Name2ParserDict = new();
    static ProtobufFactory()
    {
        Assembly assembly = Assembly.GetAssembly(typeof(GetPlayerTokenRsp))!;
        var protoClasses = assembly.GetTypes().Where(x => typeof(IMessage).IsAssignableFrom(x));

        // _ = enumerable.Select(type =>
        // {
        //     return type.GetMembers().Where(x => x.Name == "Parser").First();
        // });
        
        
        foreach (var type in protoClasses)
        {
            var classInstance = type.GetConstructors().First().Invoke(new object[]{});
            var getParserMethod = type.GetMethod("get_Parser"); 
            if (getParserMethod is not null)
            {
                var parser = getParserMethod?.Invoke(classInstance, new object[]{});
                // Console.WriteLine("found parser for type:" + type.Name);
                if (parser is null) continue;
                Name2ParserDict.TryAdd(type.Name, (parser as MessageParser)!);
                if (!Enum.TryParse(type.Name, out Opcode opcode)) continue;
                Opcode2ParserDict.TryAdd(opcode, (parser as MessageParser)!);
                Opcode2TypeDict.TryAdd(opcode, type);

            }
        }
    }
    
    public static MessageParser? GetPacketTypeParser(Opcode opcode)
    {
        return Opcode2ParserDict.TryGetValue(opcode, out var parser) ? parser : null;
    }

    public static MessageParser? GetPacketTypeParser(string name)
    {
        return Name2ParserDict.TryGetValue(name, out var parser) ? parser : null;
    }
}