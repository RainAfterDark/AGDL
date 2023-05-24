using DamageLogger.Util;

namespace DamageLogger.Core.System;

public class CommandManager
{
    private readonly DamageLogger _damageLogger;
    private readonly CustomConsole _console;
    private bool _shouldCancel;

    private readonly Dictionary<char, (string name, Action<CommandManager> action)> _commands = new()
    {
        { '1', ("Toggle Console Logging", cm => cm._damageLogger.ToggleConsoleLogging() ) },
        { '2', ("Print Damage Breakdown", cm => cm._damageLogger.RenderDamageBreakdown() ) },
        { '3', ("Reset Current Log", cm => cm._damageLogger.ResetCurrentLog() ) },
        { '4', ("Reload Config", cm => cm._damageLogger.ReloadConfig() ) },
        { '5', ("Clear Console", cm => cm._console.Clear() ) },
    };
    
    public string CommandsText { get; }

    public CommandManager(DamageLogger damageLogger)
    {
        _damageLogger = damageLogger;
        _console = CustomConsole.Instance;
        CommandsText = "Command Hotkeys: " + string.Join(", ", _commands.Select(
            pair => $"[invert] {pair.Key} [/] {pair.Value.name}"));
    }

    public void RunLoop()
    {
        while (!_shouldCancel)
        {
            if (!_damageLogger.PlayerLoggedIn) continue;
            var info = Console.ReadKey(true);
            if (_commands.TryGetValue(info.KeyChar, out var cmdInfo))
                cmdInfo.action(this);
        }
    }

    public void Close()
    {
        _shouldCancel = true;
    }
}