using DamageLogger.Extensions;
using Serilog;

const string gameVersion = "3.8.0";
const string configPath = "config.json";

Console.CursorVisible = false;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.ConsoleWithFooter()
    .CreateLogger();

Log.Information("Damage Logger for Anime Game version {GameVersion}", gameVersion);
var damageLogger = new DamageLogger.DamageLogger(configPath);
damageLogger.Run();