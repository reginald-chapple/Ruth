using System.Collections.Concurrent;

namespace Ruth.Persistence.Tests.Fixtures;

internal sealed class OutboxTestConsumerState
{
    public ConcurrentBag<Guid> ConsumerAEvents { get; } = [];
    public ConcurrentBag<Guid> ConsumerBEvents { get; } = [];
    public int ConsumerBAttempts;
    public bool FailConsumerBOnce { get; set; }
    public bool AlwaysFailConsumerA { get; set; }
}
