namespace Ruth.SharedKernel.Tenancy;

/// <summary>Thrown when an entity, token, or principal attempts to read or mutate data across a tenant boundary.</summary>
public sealed class CrossTenantAccessException : InvalidOperationException
{
    /// <summary>Initializes a new <see cref="CrossTenantAccessException" />.</summary>
    public CrossTenantAccessException()
        : base("Cross-tenant access or entity reassignment is forbidden.") { }

    /// <summary>Initializes a new <see cref="CrossTenantAccessException" /> with a custom message.</summary>
    public CrossTenantAccessException(string message)
        : base(message) { }

    /// <summary>Initializes a new <see cref="CrossTenantAccessException" /> with a message and inner exception.</summary>
    public CrossTenantAccessException(string message, Exception innerException)
        : base(message, innerException) { }
}
