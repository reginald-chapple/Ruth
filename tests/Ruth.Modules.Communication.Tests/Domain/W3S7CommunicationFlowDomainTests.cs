using Ruth.Modules.Communication.Contracts.Flows;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Domain.Flows;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Domain;

public sealed class W3S7CommunicationFlowDomainTests
{
    [Fact]
    public void CommunicationFlow_ValidatesSteps_UniqueKeys_AndConversionGoalConstraints()
    {
        var duplicateKeys = CommunicationFlow.Create(
            new CommunicationFlowSpec(
                "Invalid Duplicate Keys",
                "invalid-dup-flow",
                CommunicationFlowCategory.Onboarding,
                [
                    new CommunicationFlowStepDto(
                        0,
                        "step-one",
                        "Welcome",
                        CommunicationChannel.Email,
                        0,
                        "Welcome",
                        "<p>Hi</p>"
                    ),
                    new CommunicationFlowStepDto(
                        1,
                        "step-one",
                        "Duplicate",
                        CommunicationChannel.Email,
                        60,
                        "Follow",
                        "<p>Hi</p>"
                    ),
                ]
            )
        );
        Assert.True(duplicateKeys.IsFailure);

        var missingGroup = CommunicationFlow.Create(
            new CommunicationFlowSpec(
                "Missing Target Group",
                "missing-group-flow",
                CommunicationFlowCategory.Onboarding,
                [
                    new CommunicationFlowStepDto(
                        0,
                        "step-zero",
                        "Welcome",
                        CommunicationChannel.Sms,
                        0,
                        BodyTemplate: "Welcome!"
                    ),
                ],
                ConversionGoalKind: FlowConversionGoalKind.JoinedTargetGroup,
                ConversionTargetGroupId: null
            )
        );
        Assert.True(missingGroup.IsFailure);

        var validSystemFlow = CommunicationFlow.Create(
            new CommunicationFlowSpec(
                "Member Renewal Sequence",
                "member-renewal-sequence",
                CommunicationFlowCategory.MembershipAndRenewal,
                [
                    new CommunicationFlowStepDto(
                        0,
                        "day-0-open",
                        "Open Window",
                        CommunicationChannel.Email,
                        0,
                        "Renew Today",
                        "<p>Open</p>"
                    ),
                    new CommunicationFlowStepDto(
                        1,
                        "day-5-remind",
                        "Day 5 Reminder",
                        CommunicationChannel.Sms,
                        7_200,
                        BodyTemplate: "Friendly renewal reminder",
                        SendOnlyIfPreviousUnopened: true
                    ),
                ],
                ExitCondition: FlowExitCondition.ExitOnConversionOrClick,
                ConversionGoalKind: FlowConversionGoalKind.ClickedGoalUrl,
                ConversionGoalUrlPrefix: "https://example.org/renew",
                IsSystem: true
            )
        );
        Assert.True(validSystemFlow.IsSuccess);
        Assert.True(validSystemFlow.Value.EnsureCanDelete().IsFailure);
    }

    [Fact]
    public void FlowRecipientProgression_EvaluatesDelays_ExitConditions_AndPersonMerge()
    {
        var flow = CommunicationFlow
            .Create(
                new CommunicationFlowSpec(
                    "Engagement Drip",
                    "engagement-drip",
                    CommunicationFlowCategory.EngagementAndRetention,
                    [
                        new CommunicationFlowStepDto(
                            0,
                            "step-0",
                            "Immediate",
                            CommunicationChannel.Email,
                            DelayMinutes: 15,
                            SubjectTemplate: "Hello",
                            BodyTemplate: "<p>Step 0</p>"
                        ),
                        new CommunicationFlowStepDto(
                            1,
                            "step-1",
                            "Follow Up",
                            CommunicationChannel.Email,
                            DelayMinutes: 1_440,
                            SubjectTemplate: "Follow Up",
                            BodyTemplate: "<p>Step 1</p>"
                        ),
                    ],
                    ExitCondition: FlowExitCondition.ExitOnAnyEngagement
                )
            )
            .Value;

        var startUtc = new DateTimeOffset(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);
        var instance = CommunicationFlowInstance.Create(flow.Id, "Wave 1", startUtc);
        var oldPersonId = Guid.CreateVersion7();
        var newPersonId = Guid.CreateVersion7();

        Assert.True(CommunicationFlowStep.ValidateAndNormalize([]).IsFailure);
        Assert.True(
            CommunicationFlowStep
                .ValidateAndNormalize([
                    new CommunicationFlowStepDto(
                        0,
                        "BAD KEY!",
                        "Invalid",
                        CommunicationChannel.Sms,
                        0,
                        BodyTemplate: "Hi"
                    ),
                ])
                .IsFailure
        );

        var recipient = CommunicationFlowInstanceRecipient
            .Enroll(
                flow,
                instance.Id,
                oldPersonId,
                "Member@Example.org",
                "+15553332211",
                "Jordan",
                new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                {
                    ["BranchName"] = "Downtown",
                },
                startUtc
            )
            .Value;

        Assert.Equal(startUtc.AddMinutes(15), recipient.NextStepDueAtUtc);

        var completedAfter0 = recipient.AdvanceToNextStep(
            flow.Steps,
            startUtc.AddMinutes(15),
            stepWasSent: true
        );
        Assert.False(completedAfter0);
        Assert.Equal(1, recipient.CurrentStepIndex);
        Assert.Equal(startUtc.AddMinutes(15 + 1_440), recipient.NextStepDueAtUtc);

        recipient.RecordSignalAndEvaluateExit(
            flow.ExitCondition,
            opened: true,
            clicked: false,
            converted: false,
            startUtc.AddHours(1),
            "OpenedEmail"
        );
        Assert.Equal(FlowRecipientStatus.ConvertedExited, recipient.Status);
        Assert.Null(recipient.NextStepDueAtUtc);

        recipient.RepointPerson(oldPersonId, newPersonId);
        Assert.Equal(newPersonId, recipient.PersonId);
    }
}
