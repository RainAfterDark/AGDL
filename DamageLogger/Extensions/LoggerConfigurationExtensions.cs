using DamageLogger.Util;
using Serilog;
using Serilog.Configuration;

namespace DamageLogger.Extensions;

public static class LoggerConfigurationExtensions
{
    public static LoggerConfiguration ConsoleWithFooter(this LoggerSinkConfiguration loggerSinkConfiguration)
    {
        return loggerSinkConfiguration
            .Sink(CustomConsole.Instance.FooterPre)
            .WriteTo.Console()
            .WriteTo.Sink(CustomConsole.Instance.FooterPost);
    }
}