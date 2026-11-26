using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain;
using Ruth.Modules.CheckIn.Domain.Configurations;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;
using Ruth.SharedKernel.Time;

namespace Ruth.Modules.CheckIn.Slices.Sessions;

internal sealed class KioskSearchCandidatesQueryHandler(
    KioskSearchReadModelLoader loader,
    IClock clock
) : IQueryHandler<KioskSearchCandidatesQuery, KioskSearchResponse>
{
    public async Task<Result<KioskSearchResponse>> HandleAsync(
        KioskSearchCandidatesQuery query,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(query);

        var config = await loader.LoadConfigurationAsync(query.ConfigurationId, cancellationToken);
        if (config is null)
        {
            return Result.Failure<KioskSearchResponse>(CheckInErrors.ConfigurationNotFound);
        }

        if (!config.IsActive)
        {
            return Result.Failure<KioskSearchResponse>(CheckInErrors.ConfigurationInactive);
        }

        var flagsResult = ResolveSearchPermissions(
            config.SearchMode,
            query.InputKind,
            query.SearchTerm
        );
        if (flagsResult.IsFailure)
        {
            return Result.Failure<KioskSearchResponse>(flagsResult.Errors);
        }

        var serviceDate = query.ServiceDate ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var lookupResult = await loader.LookupHouseholdsAsync(
            query.SearchTerm,
            flagsResult.Value,
            query.MaxFamilies,
            cancellationToken
        );
        if (lookupResult.IsFailure)
        {
            return Result.Failure<KioskSearchResponse>(lookupResult.Errors);
        }

        var families = await EvaluateFamiliesAsync(
            config,
            query.ScheduleId,
            serviceDate,
            lookupResult.Value,
            cancellationToken
        );
        return Result.Success(
            new KioskSearchResponse(
                config.Id.Value,
                config.Name,
                query.ScheduleId,
                serviceDate,
                families
            )
        );
    }

    private static Result<(
        bool AllowPhone,
        bool AllowName,
        bool AllowBarcode
    )> ResolveSearchPermissions(
        CheckInSearchMode searchMode,
        KioskSearchInputKind inputKind,
        string? rawTerm
    )
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
        {
            return Result.Success((AllowPhone: false, AllowName: false, AllowBarcode: false));
        }

        return (searchMode, inputKind) switch
        {
            (CheckInSearchMode.PhoneOnly, KioskSearchInputKind.Name) => Result.Failure<(
                bool,
                bool,
                bool
            )>(CheckInErrors.SearchModeNotPermitted),
            (CheckInSearchMode.NameOnly, KioskSearchInputKind.Phone) => Result.Failure<(
                bool,
                bool,
                bool
            )>(CheckInErrors.SearchModeNotPermitted),
            (
                CheckInSearchMode.BarcodeAndPassOnly,
                KioskSearchInputKind.Phone
                    or KioskSearchInputKind.Name
            ) => Result.Failure<(bool, bool, bool)>(CheckInErrors.SearchModeNotPermitted),
            (_, KioskSearchInputKind.BarcodeOrCard) => Result.Success((false, false, true)),
            (_, KioskSearchInputKind.Phone) => Result.Success((true, false, false)),
            (_, KioskSearchInputKind.Name) => Result.Success((false, true, false)),
            (CheckInSearchMode.PhoneOnly, _) => Result.Success((true, false, true)),
            (CheckInSearchMode.NameOnly, _) => Result.Success((false, true, true)),
            (CheckInSearchMode.BarcodeAndPassOnly, _) => Result.Success((false, false, true)),
            _ => Result.Success((true, true, true)),
        };
    }

    private async Task<IReadOnlyList<KioskFamilyCandidateResponse>> EvaluateFamiliesAsync(
        CheckInConfiguration config,
        Guid scheduleId,
        DateOnly serviceDate,
        IReadOnlyList<KioskHouseholdLookupResponse> crmFamilies,
        CancellationToken cancellationToken
    )
    {
        if (crmFamilies.Count == 0)
        {
            return [];
        }

        var (occupancyMap, activePeople, recentAreaByPerson, requiresGroupContext) =
            await LoadEvaluationSnapshotAsync(
                config,
                scheduleId,
                serviceDate,
                crmFamilies,
                cancellationToken
            );
        var output = new List<KioskFamilyCandidateResponse>(crmFamilies.Count);
        foreach (var family in crmFamilies)
        {
            var members = new List<KioskMemberCandidateResponse>(family.Members.Count);
            foreach (var member in family.Members)
            {
                members.Add(
                    await EvaluateSingleMemberAsync(
                        config,
                        scheduleId,
                        serviceDate,
                        member,
                        occupancyMap,
                        activePeople.Contains(member.PersonId),
                        recentAreaByPerson.GetValueOrDefault(member.PersonId),
                        requiresGroupContext,
                        cancellationToken
                    )
                );
            }

            output.Add(
                new KioskFamilyCandidateResponse(
                    family.HouseholdId,
                    family.HouseholdName,
                    family.MatchKind,
                    members
                )
            );
        }

        return output;
    }

    private async Task<(
        Dictionary<Guid, int> OccupancyMap,
        HashSet<Guid> ActivePeople,
        Dictionary<Guid, Guid> RecentAreaByPerson,
        bool RequiresGroupContext
    )> LoadEvaluationSnapshotAsync(
        CheckInConfiguration config,
        Guid scheduleId,
        DateOnly serviceDate,
        IReadOnlyList<KioskHouseholdLookupResponse> crmFamilies,
        CancellationToken cancellationToken
    )
    {
        var occupancyMap = await loader.LoadLiveRoomOccupancyAsync(
            config.Id,
            serviceDate,
            scheduleId,
            cancellationToken
        );
        var allPersonIds = crmFamilies
            .SelectMany(static f => f.Members.Select(static m => m.PersonId))
            .Distinct()
            .ToArray();
        var activePeople = await loader.LoadAlreadyCheckedInPeopleAsync(
            config.Id,
            serviceDate,
            scheduleId,
            allPersonIds,
            cancellationToken
        );
        var recentAreas = await loader.LoadRecentAreaSelectionsAsync(
            config,
            serviceDate,
            allPersonIds,
            cancellationToken
        );
        var requiresGroupContext = config.Areas.Any(static a =>
            a.IsActive && a.Eligibility.EnrollmentMode != EligibilityEnrollmentMode.OpenToEligible
        );
        return (occupancyMap, activePeople, recentAreas, requiresGroupContext);
    }

    private async Task<KioskMemberCandidateResponse> EvaluateSingleMemberAsync(
        CheckInConfiguration config,
        Guid scheduleId,
        DateOnly serviceDate,
        KioskPersonLookupDto member,
        Dictionary<Guid, int> occupancyMap,
        bool alreadyCheckedIn,
        Guid? recentAreaId,
        bool requiresGroupContext,
        CancellationToken cancellationToken
    )
    {
        var (enrolledGroups, confirmedShifts) = requiresGroupContext
            ? await loader.ResolveGroupContextAsync(member.PersonId, serviceDate, cancellationToken)
            : ([], []);

        var participantContext = new CheckInParticipantContextDto(
            member.PersonId,
            member.AgeYears,
            member.BirthDate,
            member.Grade,
            member.MembershipTier,
            member.SkillAndBadgeCodes,
            member.NeedsAccommodations,
            enrolledGroups,
            confirmedShifts
        );

        var areaOptions = config
            .Areas.OrderBy(static a => a.Order)
            .ThenBy(static a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(area =>
                EvaluateAreaForMember(area, scheduleId, participantContext, occupancyMap)
            )
            .ToList();

        var preselected = areaOptions.FirstOrDefault(o =>
            o.IsEligible
            && o.RecommendedLocationId.HasValue
            && recentAreaId.HasValue
            && o.AreaId == recentAreaId.Value
        );

        return new KioskMemberCandidateResponse(
            member.PersonId,
            member.FullName,
            member.NickName,
            member.LastName,
            member.AgeYears,
            member.GradeFormatted,
            alreadyCheckedIn,
            preselected?.AreaId,
            preselected?.RecommendedLocationId,
            member.MedicalOrAllergyNote,
            member.ActiveSignalNotes,
            areaOptions
        );
    }

    private static EvaluatedCheckInAreaOptionResponse EvaluateAreaForMember(
        CheckInArea area,
        Guid scheduleId,
        CheckInParticipantContextDto participant,
        IReadOnlyDictionary<Guid, int> occupancyMap
    )
    {
        var (isEligible, reasons) = CheckInEligibilityEvaluator.Evaluate(area, participant);
        var (recommendedId, recommendedName, rooms) = RoomBalancingDomainService.EvaluateAreaRooms(
            area,
            scheduleId,
            occupancyMap
        );

        return new EvaluatedCheckInAreaOptionResponse(
            area.Id.Value,
            area.Name,
            area.LinkedGroupId,
            isEligible,
            reasons,
            area.PrintBadge,
            area.IssueSecurityPickupCode,
            area.Eligibility.SupportsAccommodations,
            isEligible ? recommendedId : null,
            isEligible ? recommendedName : null,
            rooms
        );
    }
}
