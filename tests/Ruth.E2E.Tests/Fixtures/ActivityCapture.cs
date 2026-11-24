using System.Collections.Concurrent;
using System.Diagnostics;

namespace Ruth.E2E.Tests.Fixtures;

public sealed class ActivityCapture : IDisposable
{
    private readonly ConcurrentQueue<Activity> _activities = new();
    private readonly ActivityListener _listener;

    public ActivityCapture(params string[] sourcePrefixes)
    {
        var prefixes =
            sourcePrefixes.Length > 0 ? sourcePrefixes : ["Ruth", "Microsoft.AspNetCore"];

        _listener = new ActivityListener
        {
            ShouldListenTo = source => MatchesPrefix(source.Name, prefixes),
            Sample = static (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Enqueue(activity),
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public IReadOnlyList<Activity> RecordedActivities => [.. _activities];

    public void Dispose()
    {
        _listener.Dispose();
    }

    private static bool MatchesPrefix(string sourceName, string[] prefixes)
    {
        foreach (var prefix in prefixes)
        {
            if (sourceName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
