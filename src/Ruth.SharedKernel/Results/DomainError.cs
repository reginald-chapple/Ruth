namespace Ruth.SharedKernel.Results;

/// <summary>Represents a structured domain, validation, or application failure.</summary>
/// <param name="Code">A stable, machine-readable error code (for example, Person.NotFound).</param>
/// <param name="Description">A human-readable description of the failure.</param>
/// <param name="Type">The error category used to map outcomes at application boundaries.</param>
public sealed record DomainError(string Code, string Description, ErrorType Type)
{
    /// <summary>Sentinel representing the absence of an error.</summary>
    public static readonly DomainError None = new(string.Empty, string.Empty, ErrorType.Failure);

    /// <summary>Sentinel used when a required non-null value is missing.</summary>
    public static readonly DomainError NullValue = new(
        "General.NullValue",
        "A required value was null.",
        ErrorType.Validation
    );

    /// <summary>Creates a general domain failure error.</summary>
    public static DomainError Failure(string code, string description)
    {
        return Create(code, description, ErrorType.Failure);
    }

    /// <summary>Creates a validation error.</summary>
    public static DomainError Validation(string code, string description)
    {
        return Create(code, description, ErrorType.Validation);
    }

    /// <summary>Creates a not-found error.</summary>
    public static DomainError NotFound(string code, string description)
    {
        return Create(code, description, ErrorType.NotFound);
    }

    /// <summary>Creates a conflict error.</summary>
    public static DomainError Conflict(string code, string description)
    {
        return Create(code, description, ErrorType.Conflict);
    }

    /// <summary>Creates an unauthorized error.</summary>
    public static DomainError Unauthorized(string code, string description)
    {
        return Create(code, description, ErrorType.Unauthorized);
    }

    /// <summary>Creates a forbidden error.</summary>
    public static DomainError Forbidden(string code, string description)
    {
        return Create(code, description, ErrorType.Forbidden);
    }

    /// <summary>Creates an unexpected error.</summary>
    public static DomainError Unexpected(string code, string description)
    {
        return Create(code, description, ErrorType.Unexpected);
    }

    private static DomainError Create(string code, string description, ErrorType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return new DomainError(code, description, type);
    }
}
