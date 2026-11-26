using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Domain;
using Ruth.Modules.Commerce.Domain.Bookings;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Core.Contracts.Schedules;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Slices.Bookings;

internal sealed class BookingScheduleAndConflictLoader(
    CommerceDbContext dbContext,
    IQueryDispatcher queryDispatcher
)
{
    public async Task<BookableService?> LoadServiceAsync(
        string idOrSlug,
        bool asNoTracking,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return null;
        }

        var trimmed = idOrSlug.Trim();
        var lowerSlug = trimmed.ToLowerInvariant();
        IQueryable<BookableService> q = dbContext.BookableServices;
        if (asNoTracking)
        {
            q = q.AsNoTracking();
        }

        return Guid.TryParse(trimmed, out var guid)
            ? await q.FirstOrDefaultAsync(
                s => s.Id == BookableServiceId.From(guid),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(s => s.Slug == lowerSlug, cancellationToken);
    }

    public async Task<RentableResource?> LoadResourceAsync(
        string idOrSlug,
        bool asNoTracking,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(idOrSlug))
        {
            return null;
        }

        var trimmed = idOrSlug.Trim();
        var lowerSlug = trimmed.ToLowerInvariant();
        IQueryable<RentableResource> q = dbContext.RentableResources;
        if (asNoTracking)
        {
            q = q.AsNoTracking();
        }

        return Guid.TryParse(trimmed, out var guid)
            ? await q.FirstOrDefaultAsync(
                r => r.Id == RentableResourceId.From(guid),
                cancellationToken
            )
            : await q.FirstOrDefaultAsync(r => r.Slug == lowerSlug, cancellationToken);
    }

    public async Task<Result<ResolvedTargetScheduleContext>> BuildSlotContextAsync(
        BookingTargetKind targetKind,
        string targetIdOrSlug,
        int? requestedDurationMinutes,
        int requestedQuantity,
        Guid? requestedProviderPersonId,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        string? excludeHoldToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var parametersRes =
            targetKind == BookingTargetKind.Service
                ? await BuildServiceParametersAsync(
                    targetIdOrSlug,
                    requestedQuantity,
                    requestedProviderPersonId,
                    cancellationToken
                )
                : await BuildResourceParametersAsync(
                    targetIdOrSlug,
                    requestedDurationMinutes,
                    requestedQuantity,
                    cancellationToken
                );
        if (parametersRes.IsFailure)
        {
            return Result.Failure<ResolvedTargetScheduleContext>(parametersRes.Errors);
        }

        var (service, resource, parameters) = parametersRes.Value;
        var expandedStart = rangeStartUtc.ToUniversalTime().AddDays(-1);
        var expandedEnd = rangeEndUtc.ToUniversalTime().AddDays(1);

        var (openWindows, blackouts) = await LoadAvailabilityAndBlackoutsAsync(
            parameters,
            expandedStart,
            expandedEnd,
            cancellationToken
        );

        var occupied = await LoadOccupiedBlocksAsync(
            expandedStart,
            expandedEnd,
            excludeHoldToken,
            nowUtc,
            cancellationToken
        );

        return new ResolvedTargetScheduleContext(
            service,
            resource,
            parameters,
            openWindows,
            blackouts,
            occupied
        );
    }

    private async Task<
        Result<(BookableService? Service, RentableResource? Resource, SlotEngineParameters Params)>
    > BuildServiceParametersAsync(
        string targetIdOrSlug,
        int requestedQuantity,
        Guid? requestedProviderPersonId,
        CancellationToken cancellationToken
    )
    {
        var service = await LoadServiceAsync(targetIdOrSlug, asNoTracking: true, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return CommerceErrors.BookableServiceNotFound;
        }

        if (
            requestedProviderPersonId is { } reqProv
            && service.AssignedProviderPersonIds.Count > 0
            && !service.AssignedProviderPersonIds.Contains(reqProv)
        )
        {
            return CommerceErrors.SlotUnavailable;
        }

        var p = new SlotEngineParameters(
            BookingTargetKind.Service,
            service.Id,
            RentableResourceId: null,
            service.DefaultLocationId,
            service.DurationMinutes,
            service.SlotIncrementMinutes,
            service.SetupBufferMinutes,
            service.TeardownBufferMinutes,
            service.MaxParticipantsPerSlot,
            service.MinimumLeadTimeMinutes,
            service.MaximumAdvanceDays,
            requestedQuantity,
            requestedProviderPersonId,
            service.AssignedProviderPersonIds
        );
        return (service, null, p);
    }

    private async Task<
        Result<(BookableService? Service, RentableResource? Resource, SlotEngineParameters Params)>
    > BuildResourceParametersAsync(
        string targetIdOrSlug,
        int? requestedDurationMinutes,
        int requestedQuantity,
        CancellationToken cancellationToken
    )
    {
        var resource = await LoadResourceAsync(
            targetIdOrSlug,
            asNoTracking: true,
            cancellationToken
        );
        if (resource is null || !resource.IsActive)
        {
            return CommerceErrors.RentableResourceNotFound;
        }

        var duration = requestedDurationMinutes ?? resource.MinDurationMinutes;
        if (duration < resource.MinDurationMinutes || duration > resource.MaxDurationMinutes)
        {
            return CommerceErrors.SlotUnavailable;
        }

        var rp = new SlotEngineParameters(
            BookingTargetKind.Resource,
            BookableServiceId: null,
            resource.Id,
            resource.CoreLocationId,
            duration,
            resource.SlotIncrementMinutes,
            resource.SetupBufferMinutes,
            resource.TeardownBufferMinutes,
            resource.QuantityAvailable,
            resource.MinimumLeadTimeMinutes,
            resource.MaximumAdvanceDays,
            requestedQuantity,
            RequestedProviderPersonId: null,
            CandidateProviderIds: []
        );
        return (null, resource, rp);
    }

    private async Task<(
        List<NormalizedAvailabilityInterval> Open,
        List<NormalizedAvailabilityInterval> Blackouts
    )> LoadAvailabilityAndBlackoutsAsync(
        SlotEngineParameters parameters,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken
    )
    {
        var rules = await dbContext
            .AvailabilityRules.AsNoTracking()
            .Where(r =>
                r.IsActive
                && (
                    (
                        parameters.BookableServiceId.HasValue
                        && r.BookableServiceId == parameters.BookableServiceId
                    )
                    || (
                        parameters.RentableResourceId.HasValue
                        && r.RentableResourceId == parameters.RentableResourceId
                    )
                )
            )
            .ToListAsync(cancellationToken);

        var open = new List<NormalizedAvailabilityInterval>();
        var blackouts = new List<NormalizedAvailabilityInterval>();

        foreach (var rule in rules)
        {
            var windows = await ExpandRuleAsync(
                rule,
                rangeStartUtc,
                rangeEndUtc,
                cancellationToken
            );
            var targetList =
                rule.RuleType == AvailabilityRuleType.BlackoutWindow ? blackouts : open;
            foreach (var (s, e) in windows)
            {
                targetList.Add(
                    new NormalizedAvailabilityInterval(s, e, rule.ProviderPersonId, rule.LocationId)
                );
            }
        }

        return (open, blackouts);
    }

    private async Task<List<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>> ExpandRuleAsync(
        AvailabilityRule rule,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        CancellationToken cancellationToken
    )
    {
        var list = new List<(DateTimeOffset StartUtc, DateTimeOffset EndUtc)>();
        list.AddRange(rule.ExpandDirectWindows(rangeStartUtc, rangeEndUtc));

        if (rule.ScheduleId is { } scheduleId)
        {
            var occRes = await queryDispatcher.QueryAsync<
                GetScheduleOccurrencesQuery,
                IReadOnlyList<ScheduleOccurrenceDto>
            >(
                new GetScheduleOccurrencesQuery(
                    scheduleId,
                    rangeStartUtc,
                    rangeEndUtc,
                    rule.TimeZoneId
                ),
                cancellationToken
            );
            if (occRes.IsSuccess)
            {
                foreach (var occ in occRes.Value)
                {
                    if (occ.EndOnUtc > occ.StartOnUtc)
                    {
                        list.Add(
                            (occ.StartOnUtc.ToUniversalTime(), occ.EndOnUtc.ToUniversalTime())
                        );
                    }
                }
            }
        }

        return list;
    }

    private async Task<List<OccupiedBookingBlock>> LoadOccupiedBlocksAsync(
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc,
        string? excludeHoldToken,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        var bookings = await dbContext
            .ServiceBookings.AsNoTracking()
            .Where(b =>
                b.Status == ServiceBookingStatus.Confirmed
                && b.EffectiveBlockStartUtc < rangeEndUtc
                && b.EffectiveBlockEndUtc > rangeStartUtc
            )
            .Select(static b => new OccupiedBookingBlock(
                b.TargetKind,
                b.BookableServiceId,
                b.RentableResourceId,
                b.ProviderPersonId,
                b.LocationId,
                b.StartUtc,
                b.EndUtc,
                b.EffectiveBlockStartUtc,
                b.EffectiveBlockEndUtc,
                b.ParticipantOrUnitCount
            ))
            .ToListAsync(cancellationToken);

        var trimmedToken = string.IsNullOrWhiteSpace(excludeHoldToken)
            ? null
            : excludeHoldToken.Trim();
        var holds = await dbContext
            .BookingSlotHolds.AsNoTracking()
            .Where(h =>
                !h.IsCompleted
                && h.ExpiresAtUtc > nowUtc
                && h.EffectiveBlockStartUtc < rangeEndUtc
                && h.EffectiveBlockEndUtc > rangeStartUtc
                && (trimmedToken == null || h.HoldToken != trimmedToken)
            )
            .Select(static h => new OccupiedBookingBlock(
                h.TargetKind,
                h.BookableServiceId,
                h.RentableResourceId,
                h.ProviderPersonId,
                h.LocationId,
                h.SlotStartUtc,
                h.SlotEndUtc,
                h.EffectiveBlockStartUtc,
                h.EffectiveBlockEndUtc,
                h.Quantity
            ))
            .ToListAsync(cancellationToken);

        return [.. bookings, .. holds];
    }
}
