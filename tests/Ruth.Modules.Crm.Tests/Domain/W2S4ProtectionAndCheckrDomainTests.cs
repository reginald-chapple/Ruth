using Ruth.Modules.Crm.Contracts.Badges;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Signals;
using Ruth.Modules.Crm.Slices.People;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S4ProtectionAndCheckrDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TopSignalCalculator_PicksLowestOrderActiveSignal_AndIgnoresExpired()
    {
        var person = Person.Create(new PersonInput("Alex", "Rivera"), _nowUtc).Value;
        var urgentType = SignalType.Create("Safety Alert", "#D13438", "warning", order: 1).Value;
        var vipType = SignalType.Create("VIP Guest", "#4F6BED", "star", order: 5).Value;

        var expiredUrgent = PersonSignal
            .Create(person.Id, urgentType.Id, person.Id, expirationDateUtc: _nowUtc.AddDays(-1))
            .Value;
        var activeVip = PersonSignal
            .Create(person.Id, vipType.Id, person.Id, expirationDateUtc: _nowUtc.AddDays(30))
            .Value;

        var typeMap = new Dictionary<SignalTypeId, SignalType>
        {
            [urgentType.Id] = urgentType,
            [vipType.Id] = vipType,
        };

        TopSignalCalculator.Recalculate(person, [expiredUrgent, activeVip], typeMap, _nowUtc);
        Assert.Equal(activeVip.Id.Value, person.TopSignalId);
        Assert.Equal("#4F6BED", person.TopSignalColor);

        TopSignalCalculator.Recalculate(person, [expiredUrgent], typeMap, _nowUtc);
        Assert.Null(person.TopSignalId);
    }

    [Fact]
    public void BuiltInPersonBadgeEvaluators_RenderExpectedBadges()
    {
        var person = Person
            .Create(
                new PersonInput(
                    "Taylor",
                    "Kim",
                    ConnectionStatus: PersonConnectionStatus.Member,
                    BirthYear: 1995,
                    BirthMonth: 5,
                    BirthDay: 10,
                    AccountProtectionProfile: AccountProtectionProfile.High
                ),
                _nowUtc
            )
            .Value;
        person.SetTopSignal(Guid.CreateVersion7(), "#D13438", "warning");

        var detail = PersonMapper.ToDetail(person, new DateOnly(2026, 11, 24));
        var ctx = new PersonBadgeEvaluationContext(
            detail,
            CompletedAssessmentsCount: 2,
            LatestAssessmentSummary: "DISC: D/I",
            LatestBackgroundCheckStatus: "Candidate Pass",
            LatestBackgroundCheckRecordFound: false,
            _nowUtc
        );

        IPersonBadgeEvaluator[] evaluators =
        [
            new TopSignalBadgeEvaluator(),
            new ConnectionStatusBadgeEvaluator(),
            new AgeAndBirthdayBadgeEvaluator(),
            new AssessmentSummaryBadgeEvaluator(),
            new BackgroundCheckStatusBadgeEvaluator(),
            new ProtectionProfileBadgeEvaluator(),
        ];

        var rendered = evaluators
            .Select(
                (ev, i) =>
                    ev.Evaluate(
                        new BadgeDefinitionResponse(
                            Guid.CreateVersion7(),
                            ev.DisplayName,
                            Description: null,
                            ev.ComponentKey,
                            Person.EntityTypeKeyName,
                            IsActive: true,
                            i,
                            new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                        ),
                        ctx
                    )
            )
            .OfType<EvaluatedPersonBadgeResponse>()
            .ToList();

        Assert.Equal(6, rendered.Count);
        Assert.Contains(rendered, static b =>
            string.Equals(b.ComponentKey, "background-check", StringComparison.Ordinal)
            && string.Equals(b.Value, "Pass", StringComparison.Ordinal)
        );
        Assert.Contains(rendered, static b =>
            string.Equals(b.ComponentKey, "assessment-summary", StringComparison.Ordinal)
            && string.Equals(b.Value, "DISC: D/I", StringComparison.Ordinal)
        );
    }
}
