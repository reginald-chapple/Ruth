namespace Ruth.SharedKernel.Tenancy;

/// <summary>Thrown when a tenant-scoped persistence or application operation executes without an active <see cref="TenantId" />.</summary>
public sealed class TenantContextRequiredException : InvalidOperationException
{
    /// <summary>Initializes a new <see cref="TenantContextRequiredException" />.</summary>
    public TenantContextRequiredException()
        : base("An active tenant context is required for this operation.") { }

    /// <summary>Initializes a new <see cref="TenantContextRequiredException" /> with a custom message.</summary>
    public TenantContextRequiredException(string message)
        : base(message) { }

    /// <summary>Initializes a new <see cref="TenantContextRequiredException" /> with a message and inner exception.</summary>
    public TenantContextRequiredException(string message, Exception innerException)
        : base(message, innerException) { }
}
