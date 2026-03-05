using Microsoft.Extensions.Logging;

namespace PreflightApi.Tests.SmokeTests;

public class WarningCapturingLogger : ILogger
{
    private readonly List<string> _warnings = new();

    public IReadOnlyList<string> Warnings => _warnings;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == LogLevel.Warning)
        {
            _warnings.Add(formatter(state, exception));
        }
    }
}
