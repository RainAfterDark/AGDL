using DamageLogger.Extensions;
using Serilog;

const string gameVersion = "3.6.0";
const string configPath = "config.json";

Console.CursorVisible = false;
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.ConsoleWithFooter()
    .CreateLogger();

Log.Information("DamageLogger for AnimeGame version {GameVersion}", gameVersion);
var damageLogger = new DamageLogger.DamageLogger(configPath);
damageLogger.Run();