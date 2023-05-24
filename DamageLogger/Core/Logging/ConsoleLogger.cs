using System.Collections.ObjectModel;
using DamageLogger.Configuration;
using DamageLogger.Core.Combat;
using DamageLogger.Core.Entity;
using DamageLogger.Extensions;
using DamageLogger.Util;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DamageLogger.Core.Logging;

public class ConsoleLogger
{
    private readonly DamageLoggerConfig _config;
    private readonly CustomConsole _console;
    private string _currentTeamDamageText = "";
    private bool _isLogging = true;

    private string CommandsText { get; }
    private string CurrentTeamDamageText
    {
        get => _currentTeamDamageText;
        set
        {
            _currentTeamDamageText = value;
            UpdateConsoleFooter();
        }
    }

    public bool IsLogging
    {
        get => _isLogging;
        set
        {
            _isLogging = value;
            UpdateConsoleFooter();
        }
    }

    public ConsoleLogger(DamageLoggerConfig config, string commandsText)
    {
        _config = config;
        _console = CustomConsole.Instance;
        CommandsText = commandsText;
    }
    
    private void UpdateConsoleFooter()
    {
        var loggingColor = IsLogging ? Theme.Colors.On : Theme.Colors.Off;
        var loggingText = $"[bold {loggingColor}]{(IsLogging ? "ON " : "OFF")}[/]";
        _console.FooterText = $"Console Logging: {loggingText} | {CurrentTeamDamageText}\n{CommandsText}";
    }

    private float GetTotalTeamDamage(IEnumerable<AvatarEntity> team)
    {
        return team.Sum(avatar => avatar.CombatManager.TotalDamageDealt);
    }
    
    private string GetAvatarDamageText(AvatarEntity avatar, float teamDamage, float loggingSeconds)
    {
        var damage = avatar.CombatManager.TotalDamageDealt;
        var ratio = damage.SafeDivision(teamDamage);
        var dps = damage.SafeDivision(loggingSeconds);
        return $"[{Theme.Colors.Avatar}]{avatar.Name}[/] = [{Theme.Colors.Damage}]{damage:N}[/] " +
               $"([{Theme.Colors.Dps}]{dps:N}/s[/], [{Theme.Colors.Ratio}]{ratio:P}[/])";
    }

    public void RenderHitInfoHeader()
    {
        if (!IsLogging) return;
        _console.Render(new Rule());
        _console.WriteLine(HitInfo.GetHeaderRow());
    }
    
    public void RenderHitInfo(HitInfo hitInfo)
    {
        if (!IsLogging) return;
        switch (_config.ConsoleLoggingMode)
        {
            case DamageLoggerConfig.ConsoleLogMode.Friendly:
                hitInfo.LogFriendly();
                break;
            case DamageLoggerConfig.ConsoleLogMode.Tsv:
                _console.WriteLine(hitInfo.ToTsvRowString());
                break;
            case DamageLoggerConfig.ConsoleLogMode.Table:
            default:
                _console.WriteLine(hitInfo.ToTableRowString());
                break;
        }
    }
    
    public void UpdateCurrentTeamDamageText(ReadOnlyCollection<AvatarEntity> currentTeam, float loggingSeconds)
    {
        var teamDamage = GetTotalTeamDamage(currentTeam);
        var totalDps =  teamDamage.SafeDivision(loggingSeconds);
        var totalDamageText = $"[yellow]{teamDamage:N}[/] ([red]{totalDps:N}/s[/])";
        var teamDamageText = string.Join(", ", currentTeam.Select(
            avatar => GetAvatarDamageText(avatar, teamDamage, loggingSeconds)));
        CurrentTeamDamageText = $"Current Team Damage (over [{Theme.Colors.Dps}]{loggingSeconds}s[/]): " +
                                $"{totalDamageText}\n{teamDamageText}";
    }
    
    public void RenderDamageBreakdown(ReadOnlyCollection<AvatarEntity> team, float loggingSeconds)
    {
        _console.Render(new Rule());
        var teamDamage = GetTotalTeamDamage(team);
        var totalDps = teamDamage.SafeDivision(loggingSeconds);
        var totalDamageText = $"[{Theme.Colors.Damage}]{teamDamage:N}[/] ([{Theme.Colors.Dps}]{totalDps:N}/s[/])";
        _console.WriteLine($"Damage Breakdown (over [{Theme.Colors.Dps}]{loggingSeconds}s[/]): {totalDamageText}");
        
        const int gridCount = 2;
        var grid = new Grid();
        foreach (var _ in Enumerable.Range(0, gridCount))
            grid.AddColumn();
        var tables = new Queue<Table>();
        var chart = new BreakdownChart().Compact().Width(180);
        
        foreach (var avatar in team)
        {
            var totalDamage = avatar.CombatManager.TotalDamageDealt;
            chart.AddItem(avatar.Name, totalDamage, Theme.GetNextColor());
                
            var table = new Table()
                .Title(GetAvatarDamageText(avatar, teamDamage, loggingSeconds))
                .AddColumn("Damage Source")
                .AddColumn("Count")
                .AddColumn("Damage")
                .AddColumn("DPS")
                .AddColumn("Ratio");
            
            foreach (var (source, data) in 
                     avatar.CombatManager.DamageDealtMap
                           .OrderByDescending(pair => pair.Value.damage))
            {
                var dps = data.damage.SafeDivision(loggingSeconds);
                var ratio = data.damage.SafeDivision(totalDamage);
                table.AddRow(
                    $"[{Theme.Colors.Source}]{source}[/]", 
                    $"[{Theme.Colors.Count}]x{data.count}[/]",
                    $"[{Theme.Colors.Damage}]{data.damage:N}[/]", 
                    $"[{Theme.Colors.Dps}]{dps:N}/s[/]", 
                    $"[{Theme.Colors.Ratio}]{ratio:P}[/]");
            }
            tables.Enqueue(table);
        }
        
        while (true)
        {
            var toAddTables = new List<IRenderable>();
            foreach (var _ in Enumerable.Range(0, gridCount))
            {
                if (tables.TryDequeue(out var table))
                    toAddTables.Add(table);
            }
            if (toAddTables.Count == 0) break;
            grid.AddRow(toAddTables.ToArray());
        }
        
        _console.Render(chart);
        _console.Render(grid);
    }
}