using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.CheckIn.Domain.Sessions;
using Ruth.Modules.CheckIn.Infrastructure;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class CheckInSessionCollaborator(CheckInDbContext dbContext)
{
    public Task<CheckInConfiguration?> LoadConfigurationAsync(
        Guid configurationId,
        CancellationToken cancellationToken
    )
    {
        var id = CheckInConfigurationId.From(configurationId);
        return dbContext
            .CheckInConfigurations.AsNoTracking()
            .Include(static c => c.Areas)
                .ThenInclude(static a => a.Rooms)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> HasActiveCheckInAsync(
        CheckInConfigurationId configId,
        Guid personId,
        DateOnly serviceDate,
        Guid scheduleId,
        CancellationToken cancellationToken
    )
    {
        return dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .AnyAsync(
                e =>
                    e.CheckInConfigurationId == configId
                    && e.PersonId == personId
                    && e.ServiceDate == serviceDate
                    && e.ScheduleId == scheduleId
                    && e.Status == CheckInEntryStatus.CheckedIn,
                cancellationToken
            );
    }

    public async Task<Dictionary<Guid, int>> GetOrLoadScheduleOccupancyAsync(
        CheckInConfigurationId configId,
        DateOnly serviceDate,
        Guid scheduleId,
        Dictionary<Guid, Dictionary<Guid, int>> cache,
        CancellationToken cancellationToken
    )
    {
        if (cache.TryGetValue(scheduleId, out var existing))
        {
            return existing;
        }

        var loaded = await dbContext
            .CheckInAttendanceEntries.AsNoTracking()
            .Where(e =>
                e.CheckInConfigurationId == configId
                && e.ServiceDate == serviceDate
                && e.ScheduleId == scheduleId
                && e.Status == CheckInEntryStatus.CheckedIn
            )
            .GroupBy(static e => e.LocationId)
            .Select(static g => new { LocationId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(static x => x.LocationId, static x => x.Count, cancellationToken);

        cache[scheduleId] = loaded;
        return loaded;
    }

    public async Task<Result<AttendanceCheckInSession>> PersistWithDailyCodeAsync(
        CheckInConfiguration config,
        SubmitCheckInSessionCommand command,
        DateOnly serviceDate,
        DateTimeOffset nowUtc,
        List<CheckInEntrySpec> baseSpecs,
        bool requiresPickupCode,
        CancellationToken cancellationToken
    )
    {
        var issuedToday = await LoadIssuedCodesTodayAsync(
            config.BranchId,
            serviceDate,
            requiresPickupCode,
            cancellationToken
        );
        var expressToken =
            command.Channel == CheckInSessionChannel.Mobile
                ? SecurityPickupCodeGenerator.GenerateMobileExpressToken()
                : null;

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var codeResult = AllocateCodeIfRequired(config, requiresPickupCode, issuedToday);
            if (codeResult.IsFailure)
            {
                return Result.Failure<AttendanceCheckInSession>(codeResult.Errors);
            }

            var sessionResult = BuildCandidateSession(
                config,
                command,
                serviceDate,
                nowUtc,
                baseSpecs,
                codeResult.Value,
                expressToken
            );
            if (sessionResult.IsFailure)
            {
                return sessionResult;
            }

            if (
                await TryCommitSessionAsync(
                    sessionResult.Value,
                    config.BranchId,
                    serviceDate,
                    codeResult.Value,
                    nowUtc,
                    cancellationToken
                )
            )
            {
                return sessionResult;
            }
        }

        return Result.Failure<AttendanceCheckInSession>(CheckInErrors.SecurityCodePoolExhausted);
    }

    private async Task<HashSet<string>> LoadIssuedCodesTodayAsync(
        Guid? branchId,
        DateOnly serviceDate,
        bool requiresPickupCode,
        CancellationToken cancellationToken
    )
    {
        if (!requiresPickupCode)
        {
            return [];
        }

        var branchScope = DailySecurityPickupCode.FormatBranchScopeKey(branchId);
        var list = await dbContext
            .DailySecurityPickupCodes.AsNoTracking()
            .Where(c => c.ServiceDate == serviceDate && c.BranchScopeKey == branchScope)
            .Select(static c => c.Code)
            .ToListAsync(cancellationToken);
        return list.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static Result<string?> AllocateCodeIfRequired(
        CheckInConfiguration config,
        bool requiresPickupCode,
        HashSet<string> issuedToday
    )
    {
        if (!requiresPickupCode)
        {
            return Result.Success<string?>(value: null);
        }

        var generated = SecurityPickupCodeGenerator.Generate(
            config.SecurityCodeAlphaLength,
            config.SecurityCodeNumericLength,
            issuedToday
        );
        if (generated.IsFailure)
        {
            return Result.Failure<string?>(generated.Errors);
        }

        _ = issuedToday.Add(generated.Value);
        return Result.Success<string?>(generated.Value);
    }

    private static Result<AttendanceCheckInSession> BuildCandidateSession(
        CheckInConfiguration config,
        SubmitCheckInSessionCommand command,
        DateOnly serviceDate,
        DateTimeOffset nowUtc,
        List<CheckInEntrySpec> baseSpecs,
        string? pickupCode,
        string? expressToken
    )
    {
        var stamped = baseSpecs
            .Select(s =>
                s with
                {
                    SecurityPickupCode =
                        config.Areas.First(a => a.Id == s.AreaId).IssueSecurityPickupCode
                        || config.ProfileKind == CheckInProfileKind.YouthAndChildcare
                            ? pickupCode
                            : null,
                }
            )
            .ToList();

        return AttendanceCheckInSession.Create(
            new CheckInSessionCreationSpec(
                config.Id,
                config.Name,
                config.BranchId,
                command.KioskDeviceId,
                command.HouseholdId,
                command.CheckedInByPersonId,
                command.Channel,
                command.SearchModeUsed,
                command.SearchTermSummary,
                serviceDate,
                pickupCode,
                expressToken,
                command.ClientIpAddress,
                nowUtc
            ),
            stamped
        );
    }

    private async Task<bool> TryCommitSessionAsync(
        AttendanceCheckInSession session,
        Guid? branchId,
        DateOnly serviceDate,
        string? pickupCode,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        _ = dbContext.AttendanceCheckInSessions.Add(session);
        if (pickupCode is not null)
        {
            _ = dbContext.DailySecurityPickupCodes.Add(
                DailySecurityPickupCode.Issue(serviceDate, branchId, pickupCode, session.Id, nowUtc)
            );
        }

        try
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex)
            when (pickupCode is not null
                && ex.InnerException is PostgresException pgEx
                && string.Equals(
                    pgEx.SqlState,
                    PostgresErrorCodes.UniqueViolation,
                    StringComparison.Ordinal
                )
            )
        {
            dbContext.ChangeTracker.Clear();
            return false;
        }
    }
}
