namespace Ruth.SharedKernel.Primitives;

/// <summary>
/// Marks an entity that tracks UTC creation and modification timestamps via <see cref="Time.IClock" />.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets the UTC instant when this entity was first persisted.</summary>
    DateTimeOffset CreatedOnUtc { get; }

    /// <summary>Gets the UTC instant when this entity was last modified, or <see langword="null" />.</summary>
    DateTimeOffset? ModifiedOnUtc { get; }

    /// <summary>Sets the initial UTC creation timestamp.</summary>
    /// <param name="createdOnUtc">The UTC creation timestamp.</param>
    void SetCreated(DateTimeOffset createdOnUtc);

    /// <summary>Sets the UTC modification timestamp.</summary>
    /// <param name="modifiedOnUtc">The UTC modification timestamp.</param>
    void SetModified(DateTimeOffset modifiedOnUtc);
}
