namespace Ruth.SharedKernel.Security;

/// <summary>
/// Exception thrown before any payload is transmitted when an outbound HTTP target violates SSRF egress guards.
/// </summary>
public sealed class OutboundSsrfBlockedException : HttpRequestException
{
    /// <summary>Initializes a new instance of the <see cref="OutboundSsrfBlockedException" /> class.</summary>
    public OutboundSsrfBlockedException()
        : base("Outbound HTTP request was blocked by SSRF network egress policy.")
    {
        ReasonCode = "Outbound.Blocked";
    }

    /// <summary>Initializes a new instance of the <see cref="OutboundSsrfBlockedException" /> class.</summary>
    public OutboundSsrfBlockedException(string message)
        : base(message)
    {
        ReasonCode = "Outbound.Blocked";
    }

    /// <summary>Initializes a new instance of the <see cref="OutboundSsrfBlockedException" /> class.</summary>
    public OutboundSsrfBlockedException(string message, Exception? innerException)
        : base(message, innerException)
    {
        ReasonCode = "Outbound.Blocked";
    }

    /// <summary>Initializes a new instance of the <see cref="OutboundSsrfBlockedException" /> class with a structured reason code.</summary>
    public OutboundSsrfBlockedException(
        string reasonCode,
        string message,
        Exception? innerException = null
    )
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        ReasonCode = reasonCode;
    }

    /// <summary>Gets the machine-readable SSRF rejection reason code.</summary>
    public string ReasonCode { get; }
}
