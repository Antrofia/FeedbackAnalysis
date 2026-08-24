using Microsoft.Extensions.Logging;

namespace FeedbackAnalysis.Tests.TestSupport;

public sealed record LogEntry(LogLevel Level, string Category, Exception? Exception, string Message);

/// <summary>
/// Логгер-коллектор: сохраняет все записи для проверок в тестах.
/// </summary>
public sealed class CollectingLogger : ILogger
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new LogEntry(logLevel, typeof(TState).Name, exception, formatter(state, exception)));
    }
}

public sealed class CollectingLogger<T> : ILogger<T>
{
    private readonly CollectingLogger _inner = new();

    public IReadOnlyList<LogEntry> Entries => _inner.Entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        ((ILogger)_inner).Log(logLevel, eventId, state, exception, formatter);
    }
}
