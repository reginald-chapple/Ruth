using Ruth.SharedKernel.Events;
using Ruth.SharedKernel.Primitives;

namespace Ruth.Persistence.EntityFramework;

internal sealed record StagedDomainEvents(
    IHasDomainEvents Aggregate,
    IReadOnlyList<IDomainEvent> Events
);
