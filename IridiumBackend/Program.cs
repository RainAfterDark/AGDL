using DNToolKit.Configuration;
using IridiumBackend.Configuration;
using IridiumBackend.Frontend;
using Serilog;

const ushort GameMajorVersion = 3;
const ushort GameMinorVersion = 6;
const string ConfigPath = "config.json";

Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console().CreateLogger();
Log.Information("DNToolKit for v{Major}.{Minor}", GameMajorVersion, GameMinorVersion);

var config = ConfigurationProvider.LoadConfig<FrontendConfig>(ConfigPath);
var toolKit = new DNToolKit.DNToolKit(config);

var frontendManager = new FrontendManager(config.FrontendUrl);
toolKit.PacketReceived += (_, e) => frontendManager.SendPacket(e);

Console.CancelKeyPress += Close;

await toolKit.RunAsync();

void Close(object? sender, ConsoleCancelEventArgs e)
{
    toolKit.Close();
    frontendManager.Close();
}