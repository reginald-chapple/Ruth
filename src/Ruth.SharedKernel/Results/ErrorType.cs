namespace Ruth.SharedKernel.Results;

/// <summary>Classifies a domain or application <see cref="DomainError" />.</summary>
public enum ErrorType
{
    /// <summary>General business rule or domain invariant failure.</summary>
    Failure = 0,

    /// <summary>Input or invariant validation failure.</summary>
    Validation = 1,

    /// <summary>Requested resource or aggregate was not found.</summary>
    NotFound = 2,

    /// <summary>State conflict or uniqueness violation.</summary>
    Conflict = 3,

    /// <summary>Caller is not authenticated.</summary>
    Unauthorized = 4,

    /// <summary>Caller lacks permission for the requested operation.</summary>
    Forbidden = 5,

    /// <summary>Unexpected non-domain failure.</summary>
    Unexpected = 6,
}
