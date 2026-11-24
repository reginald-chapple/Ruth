namespace Ruth.SharedKernel.Primitives;

/// <summary>Marks an entity that uses an application-managed optimistic concurrency stamp.</summary>
public interface IHasConcurrencyToken
{
    /// <summary>Gets the current optimistic concurrency stamp rotated on each persistence mutation.</summary>
    Guid ConcurrencyStamp { get; }

    /// <summary>Rotates the optimistic concurrency stamp to a new value.</summary>
    /// <param name="newStamp">The new concurrency stamp value.</param>
    void SetConcurrencyStamp(Guid newStamp);
}
