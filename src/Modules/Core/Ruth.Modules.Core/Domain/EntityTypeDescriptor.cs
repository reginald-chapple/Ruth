using Ruth.SharedKernel.Metadata;
using Ruth.SharedKernel.Primitives;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Core.Domain;

/// <summary>Persisted metadata record for a compile-time registered modular entity type.</summary>
public sealed class EntityTypeDescriptor
    : AggregateRoot<EntityTypeDescriptorId>,
        IAuditableEntity,
        IHasConcurrencyToken
{
    private EntityTypeDescriptor() { }

    private EntityTypeDescriptor(
        EntityTypeDescriptorId id,
        Guid systemGuid,
        string key,
        string name,
        string friendlyName,
        string moduleName,
        bool isEntity,
        bool isSecured,
        bool supportsAttributes
    )
        : base(id)
    {
        SystemGuid = systemGuid;
        Key = key;
        Name = name;
        FriendlyName = friendlyName;
        ModuleName = moduleName;
        IsEntity = isEntity;
        IsSecured = isSecured;
        SupportsAttributes = supportsAttributes;
    }

    /// <summary>Gets the deterministic well-known SystemGuid.</summary>
    public Guid SystemGuid { get; private set; }

    /// <summary>Gets the canonical entity type key (for example, Ruth.Core.DefinedType).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Gets the CLR type name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the human-readable display name.</summary>
    public string FriendlyName { get; private set; } = string.Empty;

    /// <summary>Gets the owning bounded-context module name.</summary>
    public string ModuleName { get; private set; } = string.Empty;

    /// <summary>Gets whether this descriptor represents a persisted domain entity.</summary>
    public bool IsEntity { get; private set; }

    /// <summary>Gets whether this entity participates in ISecured ACL authorization.</summary>
    public bool IsSecured { get; private set; }

    /// <summary>Gets whether this entity supports extensible JSONB attributes.</summary>
    public bool SupportsAttributes { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedOnUtc { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedOnUtc { get; private set; }

    /// <inheritdoc />
    public Guid ConcurrencyStamp { get; private set; } = Guid.CreateVersion7();

    /// <summary>Creates a descriptor from a compile-time <see cref="EntityTypeRegistration" />.</summary>
    public static Result<EntityTypeDescriptor> FromRegistration(EntityTypeRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        return
            registration.SystemGuid == Guid.Empty
            || string.IsNullOrWhiteSpace(registration.Key)
            || string.IsNullOrWhiteSpace(registration.Name)
            || string.IsNullOrWhiteSpace(registration.ModuleName)
            ? (Result<EntityTypeDescriptor>)CoreErrors.EntityTypeInvalid
            : (Result<EntityTypeDescriptor>)
                new EntityTypeDescriptor(
                    EntityTypeDescriptorId.From(registration.SystemGuid),
                    registration.SystemGuid,
                    registration.Key.Trim(),
                    registration.Name.Trim(),
                    string.IsNullOrWhiteSpace(registration.FriendlyName)
                        ? registration.Name.Trim()
                        : registration.FriendlyName.Trim(),
                    registration.ModuleName.Trim(),
                    registration.IsEntity,
                    registration.IsSecured,
                    registration.SupportsAttributes
                );
    }

    /// <summary>Synchronizes mutable metadata fields from a compile-time registration.</summary>
    public void SyncFrom(EntityTypeRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);

        Name = registration.Name.Trim();
        FriendlyName = string.IsNullOrWhiteSpace(registration.FriendlyName)
            ? registration.Name.Trim()
            : registration.FriendlyName.Trim();
        ModuleName = registration.ModuleName.Trim();
        IsEntity = registration.IsEntity;
        IsSecured = registration.IsSecured;
        SupportsAttributes = registration.SupportsAttributes;
    }

    /// <inheritdoc />
    public void SetCreated(DateTimeOffset createdOnUtc)
    {
        CreatedOnUtc = createdOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetModified(DateTimeOffset modifiedOnUtc)
    {
        ModifiedOnUtc = modifiedOnUtc.ToUniversalTime();
    }

    /// <inheritdoc />
    public void SetConcurrencyStamp(Guid newStamp)
    {
        ConcurrencyStamp = newStamp;
    }
}
