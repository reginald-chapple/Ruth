using Microsoft.Extensions.Logging;

namespace Ruth.SharedKernel.Tests.TestDoubles;

internal sealed class RecordingLogger<TCategoryName> : ILogger<TCategoryName>
{
    private readonly List<LogEntry> _entries = [];

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        _entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception));
    }

    internal sealed record LogEntry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception
    );
}
