using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.Identity.Domain;

/// <summary>Strongly-typed UUIDv7 identifier for <see cref="AuthAuditLog" />.</summary>
public readonly record struct AuthAuditLogId(Guid Value) : IStronglyTypedId<AuthAuditLogId>
{
    /// <summary>Represents an uninitialized identifier.</summary>
    public static AuthAuditLogId Empty => new(Guid.Empty);

    /// <summary>Creates a new UUIDv7 audit log identifier.</summary>
    public static AuthAuditLogId Create()
    {
        return new(Guid.CreateVersion7());
    }

    /// <inheritdoc />
    public static AuthAuditLogId From(Guid value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Value.ToString();
    }
}
