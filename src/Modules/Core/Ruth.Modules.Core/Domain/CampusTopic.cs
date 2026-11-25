using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Core.Domain;

/// <summary>Represents a contact topic and email address configured on a <see cref="Campus" />.</summary>
public sealed class CampusTopic : Entity<Guid>
{
    private CampusTopic() { }

    internal CampusTopic(
        Guid id,
        CampusId campusId,
        Guid topicTypeValueId,
        string email,
        bool isPublic
    )
        : base(id)
    {
        CampusId = campusId;
        TopicTypeValueId = topicTypeValueId;
        Email = email;
        IsPublic = isPublic;
    }

    /// <summary>Gets the owning campus identifier.</summary>
    public CampusId CampusId { get; private set; }

    /// <summary>Gets the topic type defined value identifier.</summary>
    public Guid TopicTypeValueId { get; private set; }

    /// <summary>Gets the destination email address for this topic.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Gets whether this topic is displayed publicly.</summary>
    public bool IsPublic { get; private set; }
}
