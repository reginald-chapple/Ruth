using Ruth.SharedKernel.Primitives;

namespace Ruth.Modules.CheckIn.Domain.Sessions;

/// <summary>
/// Persisted daily ledger row enforcing database-level uniqueness of issued security pickup codes
/// within a local service date and branch scope.
/// </summary>
public sealed class DailySecurityPickupCode : Entity<DailySecurityPickupCodeId>
{
    /// <summary>Scope key used when a configuration applies across all branches.</summary>
    public const string GlobalBranchScopeKey = "GLOBAL";

    private DailySecurityPickupCode() { }

    private DailySecurityPickupCode(
        DailySecurityPickupCodeId id,
        DateOnly serviceDate,
        string branchScopeKey,
        string code,
        AttendanceCheckInSessionId sessionId,
        DateTimeOffset issuedOnUtc
    )
        : base(id)
    {
        ServiceDate = serviceDate;
        BranchScopeKey = branchScopeKey;
        Code = code;
        SessionId = sessionId;
        IssuedOnUtc = issuedOnUtc.ToUniversalTime();
    }

    /// <summary>Gets the local service date for which this pickup code is reserved.</summary>
    public DateOnly ServiceDate { get; private set; }

    /// <summary>Gets the branch scope key ("GLOBAL" or normalized BranchId).</summary>
    public string BranchScopeKey { get; private set; } = GlobalBranchScopeKey;

    /// <summary>Gets the uppercase security pickup code.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Gets the session that claimed this code.</summary>
    public AttendanceCheckInSessionId SessionId { get; private set; }

    /// <summary>Gets the UTC timestamp when this code was issued.</summary>
    public DateTimeOffset IssuedOnUtc { get; private set; }

    /// <summary>Resolves the canonical branch scope key from an optional <paramref name="branchId" />.</summary>
    public static string FormatBranchScopeKey(Guid? branchId)
    {
        return branchId is { } id && id != Guid.Empty
            ? id.ToString("D").ToUpperInvariant()
            : GlobalBranchScopeKey;
    }

    internal static DailySecurityPickupCode Issue(
        DateOnly serviceDate,
        Guid? branchId,
        string code,
        AttendanceCheckInSessionId sessionId,
        DateTimeOffset issuedOnUtc
    )
    {
        return new DailySecurityPickupCode(
            DailySecurityPickupCodeId.Create(),
            serviceDate,
            FormatBranchScopeKey(branchId),
            code.Trim().ToUpperInvariant(),
            sessionId,
            issuedOnUtc
        );
    }
}
