using Serilog.Core;
using Serilog.Events;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DamageLogger.Util;

public sealed class CustomConsole
{
    public class ConsoleFooterSink : ILogEventSink
    {
        public event EventHandler? Logged;
        public void Emit(LogEvent _) => Logged?.Invoke(this, EventArgs.Empty);
    }
    
    public readonly ConsoleFooterSink FooterPre = new();
    public readonly ConsoleFooterSink FooterPost = new();

    private int _previousLineCount;
    private string? _footerText;
    public string? FooterText
    {
        get => _footerText;
        set
        {
            _footerText = value;
            ClearPreviousLines();
            WriteFooter();
        }
    }

    private static readonly Lazy<CustomConsole> LazyInstance = new(() => new CustomConsole());
    public static CustomConsole Instance => LazyInstance.Value;
    
    private CustomConsole()
    {
        FooterPre.Logged += (_, _) => ClearPreviousLines();
        FooterPost.Logged += (_, _) => WriteFooter();
    }

    private void ClearPreviousLines()
    {
        lock (Console.Out)
        {
            if (_previousLineCount == 0) return;
            foreach (var _ in Enumerable.Range(0, _previousLineCount))
            {
                Console.SetCursorPosition(0, int.Max(Console.CursorTop - 1, 0));
                Console.Write(new string(' ', Console.BufferWidth));
            }
            Console.SetCursorPosition(0, Console.CursorTop);
        }
    }

    private void WriteFooter()
    {
        lock (Console.Out)
        {
            var previousTop = Console.CursorTop;
            if (FooterText is not null)
            {
                AnsiConsole.Write(new Rule());
                AnsiConsole.MarkupLine(FooterText);
            }
            _previousLineCount = Console.CursorTop - previousTop;
        }
    }

    public void WriteLine(string text)
    {
        lock (Console.Out)
        {
            ClearPreviousLines();
            AnsiConsole.MarkupLine(text);
            WriteFooter();
        }
    }

    public void Render(IRenderable item)
    {
        lock (Console.Out)
        {
            ClearPreviousLines();
            AnsiConsole.Write(item);
            WriteFooter();
        }
    }
}