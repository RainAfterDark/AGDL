using DamageLogger.Configuration;
using DamageLogger.Core.Combat;
using DamageLogger.Core.Entity;
using Serilog;

namespace DamageLogger.Core.Logging;

public class FileLogger
{
    private readonly DamageLoggerConfig _config;
    private string? _teamName;
    private string? _logFileName;

    private string? DirPath => _teamName is null ? null : Path.Join(_config.LogDirectory, _teamName);
    private string? FilePath => _logFileName is null || DirPath is null ? null : Path.Join(DirPath, _logFileName);
    public string? AbsFilePath => FilePath is null ? null : Path.Join(Directory.GetCurrentDirectory(), FilePath);

    public FileLogger(DamageLoggerConfig config)
    {
        _config = config;
        if (!Directory.Exists(_config.LogDirectory))
            Directory.CreateDirectory(_config.LogDirectory);
    }

    public void UpdateTeam(IEnumerable<AvatarEntity> team)
    {
        _teamName = string.Join('-', team.Select(avatar => avatar.Name));
        if (!Directory.Exists(DirPath)) Directory.CreateDirectory(DirPath!);
        var files = Directory.GetFiles(DirPath!).Select(Path.GetFileName).ToArray();
        var identifier = files.Length;
        foreach (var file in files)
        {
            if (file is null) continue;
            var sep = file.IndexOf('-');
            if (sep == -1) continue;
            if (int.TryParse(file[..sep], out var index))
                identifier = int.Max(identifier, index);
        }
        do _logFileName = $"{identifier++}-{_teamName}.log";
        while (File.Exists(FilePath));
        Log.Information("Logging to {LogFilePath}", AbsFilePath);
    }

    public void LogHitInfo(HitInfo hitInfo)
    {
        if (FilePath is null) return;
        File.AppendAllText(FilePath, hitInfo.ToTsvRowString() + "\n");
    }
}