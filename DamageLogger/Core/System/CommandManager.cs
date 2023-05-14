namespace DamageLogger.Core.System;

public class CommandManager
{
    private readonly DamageLogger _damageLogger;
    private bool _shouldCancel;

    private readonly Dictionary<char, (string name, Action<CommandManager> action)> _commands = new()
    {
        { '1', ("Toggle Logging", (cm) => cm._damageLogger.IsLogging = !cm._damageLogger.IsLogging ) },
        { '2', ("Print Damage Breakdown", (cm) => cm._damageLogger.RenderDamageBreakdown() ) },
        { '3', ("Reset Current Log", (cm) => cm._damageLogger.ResetCurrentLog() ) },
        { '4', ("Reload Config", (cm) => cm._damageLogger.ReloadConfig() ) },
    };
    
    public string CommandsText { get; }

    public CommandManager(DamageLogger damageLogger)
    {
        _damageLogger = damageLogger;
        CommandsText = "Commands: " + string.Join(", ", _commands.Select(
            pair => $"[invert] {pair.Key} [/] {pair.Value.name}"));
    }

    public void RunLoop()
    {
        while (!_shouldCancel)
        {
            var info = Console.ReadKey(true);
            if (!_damageLogger.PlayerLoggedIn) continue;
            if (_commands.TryGetValue(info.KeyChar, out var cmdInfo))
                cmdInfo.action(this);
        }
    }

    public void Close()
    {
        _shouldCancel = true;
    }
}