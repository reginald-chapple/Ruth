using Ruth.Modules.Engagement.Contracts.Achievements;
using Ruth.Modules.Engagement.Contracts.Journeys;
using Ruth.Modules.Engagement.Contracts.Streaks;
using Ruth.Modules.Engagement.Domain.Achievements;
using Ruth.Modules.Engagement.Domain.Journeys;
using Ruth.Modules.Engagement.Domain.Streaks;
using Xunit;

namespace Ruth.Modules.Engagement.Tests.Domain;

public sealed class W5S2JourneysStreaksAchievementsDomainTests
{
    [Fact]
    public void JourneyPrerequisiteValidator_DetectsCycles_AndEnforcesPrerequisites()
    {
        Assert.False(JourneyPrerequisiteValidator.ValidateAcyclicPrerequisites([]));
        Assert.False(
            JourneyPrerequisiteValidator.ValidateAcyclicPrerequisites([
                new MilestoneTypeSpec("", "A", 0),
            ])
        );
        Assert.False(
            JourneyPrerequisiteValidator.ValidateAcyclicPrerequisites([
                new MilestoneTypeSpec("S1", "DUP", 0),
                new MilestoneTypeSpec("S2", "DUP", 1),
            ])
        );

        var cyclicSpec = new JourneyProgramSpec(
            "Cyclic Track",
            "cyclic-track",
            [new MilestoneStatusSpec("Done", "#107C10", IsCompleteStatus: true, Order: 0)],
            [
                new MilestoneTypeSpec("Step A", "A", 0, PrerequisiteCodes: ["B"]),
                new MilestoneTypeSpec("Step B", "B", 1, PrerequisiteCodes: ["A"]),
            ]
        );
        Assert.True(JourneyProgram.Create(cyclicSpec).IsFailure);

        var validSpec = new JourneyProgramSpec(
            "Onboarding Journey",
            "onboarding-journey",
            [
                new MilestoneStatusSpec("Started", "#0F6CBD", IsCompleteStatus: false, Order: 0),
                new MilestoneStatusSpec("Completed", "#107C10", IsCompleteStatus: true, Order: 1),
            ],
            [
                new MilestoneTypeSpec("Step 1", "S1", 0),
                new MilestoneTypeSpec("Step 2", "S2", 1, PrerequisiteCodes: ["S1"]),
            ]
        );
        var progRes = JourneyProgram.Create(validSpec);
        Assert.True(progRes.IsSuccess);
        var program = progRes.Value;

        var personId = Guid.CreateVersion7();
        var doneStatus = program.Statuses.First(static s => s.IsCompleteStatus);
        var step1 = program.MilestoneTypes.First(static m =>
            string.Equals(m.Code, "S1", StringComparison.Ordinal)
        );
        var step2 = program.MilestoneTypes.First(static m =>
            string.Equals(m.Code, "S2", StringComparison.Ordinal)
        );

        var premature = PersonMilestone.Record(
            personId,
            program,
            step2,
            doneStatus,
            new DateOnly(2026, 11, 1),
            completedDate: null,
            endDate: null,
            branchId: null,
            note: null,
            new HashSet<Guid>()
        );
        Assert.True(premature.IsFailure);

        var m1 = PersonMilestone.Record(
            personId,
            program,
            step1,
            doneStatus,
            new DateOnly(2026, 11, 1),
            completedDate: null,
            endDate: null,
            branchId: null,
            "Completed S1",
            new HashSet<Guid>()
        );
        Assert.True(m1.IsSuccess);

        var completedSet = new HashSet<Guid> { step1.Id.Value };
        Assert.True(
            PersonMilestone
                .Record(
                    personId,
                    program,
                    step1,
                    doneStatus,
                    new DateOnly(2026, 11, 2),
                    completedDate: null,
                    endDate: null,
                    branchId: null,
                    note: null,
                    completedSet
                )
                .IsFailure
        );

        var m2 = PersonMilestone.Record(
            personId,
            program,
            step2,
            doneStatus,
            new DateOnly(2026, 11, 5),
            completedDate: null,
            endDate: null,
            branchId: null,
            "Completed S2",
            completedSet
        );
        Assert.True(m2.IsSuccess);
        _ = completedSet.Add(step2.Id.Value);
        Assert.True(JourneyPrerequisiteValidator.IsProgramComplete(program, completedSet));
    }

    [Fact]
    public void StreakBitmaskEngine_PopCount_Exclusions_Grace_AndBitwiseMerge_AreExact()
    {
        var anchor = new DateOnly(2026, 1, 4);
        var st = StreakType
            .Create(
                new StreakTypeSpec(
                    "Weekly Cohort",
                    "WEEKLY_COHORT",
                    StreakCadence.Weekly,
                    anchor,
                    WeekEndDay: DayOfWeek.Sunday
                )
            )
            .Value;

        Assert.True(st.AddExclusion(new DateOnly(2026, 1, 18), branchId: null).IsSuccess);

        var personA = Guid.CreateVersion7();
        var personB = Guid.CreateVersion7();
        var streakA = Streak.Enroll(st, personA, branchId: null, anchor).Value;
        var streakB = Streak.Enroll(st, personB, branchId: null, anchor).Value;

        _ = streakA.RecordEngagement(st, new DateOnly(2026, 1, 4), new DateOnly(2026, 1, 25));
        _ = streakA.RecordEngagement(st, new DateOnly(2026, 1, 11), new DateOnly(2026, 1, 25));
        _ = streakA.RecordEngagement(st, new DateOnly(2026, 1, 25), new DateOnly(2026, 1, 25));

        Assert.Equal(3, streakA.EngagementCount);
        Assert.Equal(3, streakA.CurrentStreakCount);
        Assert.Equal(3, streakA.LongestStreakCount);
        Assert.Equal(3, StreakBitmaskEngine.CountSetBits(streakA.EngagementMap));

        _ = streakB.RecordEngagement(st, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 1));
        streakA.MergeFrom(streakB, st, new DateOnly(2026, 2, 1));
        Assert.Equal(4, streakA.EngagementCount);
        Assert.Equal(4, streakA.CurrentStreakCount);
        Assert.Equal(4, streakA.LongestStreakCount);

        var exclMap = StreakBitmaskEngine.BuildExclusionMap(
            st.AnchorStartDate,
            st.Cadence,
            st.WeekEndDay,
            st.Exclusions,
            branchId: null,
            maxBucketIndex: 4
        );
        var sparkline = StreakBitmaskEngine.FormatRecentBuckets(
            streakA.EngagementMap,
            exclMap,
            asOfBucketIndex: 4,
            windowBuckets: 5
        );
        Assert.Equal("11E11", sparkline);

        var branch1 = Guid.CreateVersion7();
        var branch2 = Guid.CreateVersion7();
        Assert.True(st.AddExclusion(new DateOnly(2026, 2, 8), branch1).IsSuccess);
        var branchExcl = StreakBitmaskEngine.BuildExclusionMap(
            st.AnchorStartDate,
            st.Cadence,
            st.WeekEndDay,
            st.Exclusions,
            branch2,
            maxBucketIndex: 6
        );
        Assert.False(StreakBitmaskEngine.IsBitSet(branchExcl, 5));

        var gId = Guid.CreateVersion7();
        var gtId = Guid.CreateVersion7();
        Assert.True(st.MatchesAttendance(gId, gtId));
        var groupSt = StreakType
            .Create(
                new StreakTypeSpec(
                    "Group Specific",
                    "GRP_SPEC",
                    StreakCadence.Daily,
                    anchor,
                    SourceKind: StreakSourceKind.GroupAttendance,
                    TargetEntityId: gId
                )
            )
            .Value;
        Assert.True(groupSt.MatchesAttendance(gId, groupTypeId: null));
        Assert.False(groupSt.MatchesAttendance(Guid.CreateVersion7(), groupTypeId: null));
    }

    [Fact]
    public void AchievementEvaluator_EnforcesPrerequisites_OverAchievement_AndMaxSuccessCaps()
    {
        var prereq = AchievementType
            .Create(
                new AchievementTypeSpec(
                    "Bronze",
                    "BRONZE",
                    AchievementSourceKind.ManualOrExternal,
                    2
                )
            )
            .Value;

        var gold = AchievementType
            .Create(
                new AchievementTypeSpec(
                    "Gold",
                    "GOLD",
                    AchievementSourceKind.ManualOrExternal,
                    5,
                    AllowOverAchievement: false,
                    MaxSuccessesAllowed: 1,
                    PrerequisiteAchievementIds: [prereq.Id.Value]
                )
            )
            .Value;

        var personId = Guid.CreateVersion7();
        var today = new DateOnly(2026, 11, 25);

        var blocked = AchievementEvaluator.EvaluateProgress(
            gold,
            personId,
            5,
            today,
            [],
            new HashSet<Guid>()
        );
        Assert.True(blocked.IsFailure);

        var earnedSet = new HashSet<Guid> { prereq.Id.Value };
        var first = AchievementEvaluator.EvaluateProgress(gold, personId, 3, today, [], earnedSet);
        Assert.True(first.IsSuccess);
        Assert.False(first.Value.NewlyEarned);
        Assert.Equal(0.6m, first.Value.Attempt.Progress);

        var second = AchievementEvaluator.EvaluateProgress(
            gold,
            personId,
            6,
            today,
            [first.Value.Attempt],
            earnedSet
        );
        Assert.True(second.IsSuccess);
        Assert.True(second.Value.NewlyEarned);
        Assert.True(second.Value.Attempt.IsSuccessful);
        Assert.True(second.Value.Attempt.IsClosed);
        Assert.Equal(1.0m, second.Value.Attempt.Progress);

        var capped = AchievementEvaluator.EvaluateProgress(
            gold,
            personId,
            1,
            today,
            [second.Value.Attempt],
            earnedSet
        );
        Assert.True(capped.IsFailure);
    }
}
