using Ruth.Modules.Crm.Contracts.BackgroundChecks;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.BackgroundChecks;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Domain.Verification;
using Ruth.Providers.Checkr;
using Xunit;

namespace Ruth.Modules.Crm.Tests.Domain;

public sealed class W2S4AssessmentsCheckrVerificationDomainTests
{
    private static readonly DateTimeOffset _nowUtc = new(2026, 11, 24, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Assessment_EnforcesRequiresRequest_Cooldown_AndExpiration()
    {
        var personId = PersonId.Create();
        var requesterId = PersonId.Create();
        var reqType = AssessmentType
            .Create(
                "leadership-360",
                "Leadership 360",
                "/assessments/l360",
                requiresRequest: true,
                minimumDaysToRetake: 90,
                validDurationDays: 365
            )
            .Value;

        var selfBlocked = Assessment.Request(
            personId,
            reqType,
            requesterPersonId: null,
            requestedDueDate: null,
            lastCompletedOnUtc: null,
            _nowUtc
        );
        Assert.Equal(CrmErrors.AssessmentRequiresRequest, selfBlocked.PrimaryError);

        var selfType = AssessmentType
            .Create(
                "disc",
                "DISC",
                "/assessments/disc",
                requiresRequest: false,
                minimumDaysToRetake: 90,
                validDurationDays: 180
            )
            .Value;

        var cooldownBlocked = Assessment.Request(
            personId,
            selfType,
            requesterPersonId: null,
            requestedDueDate: null,
            lastCompletedOnUtc: _nowUtc.AddDays(-10),
            _nowUtc
        );
        Assert.Equal(CrmErrors.AssessmentCooldownActive, cooldownBlocked.PrimaryError);

        var ok = Assessment
            .Request(
                personId,
                selfType,
                requesterId,
                requestedDueDate: null,
                lastCompletedOnUtc: null,
                _nowUtc
            )
            .Value;
        Assert.True(
            ok.Complete( /*lang=json,strict*/
                "{\"primary\":\"D\"}",
                "DISC: D",
                _nowUtc.AddDays(-200)
            ).IsSuccess
        );
        Assert.True(ok.IsExpiredAt(selfType.ValidDurationDays, _nowUtc));
    }

    [Fact]
    public void CheckrWebhookVerifierAndParser_AndOutOfOrderGuard_WorkCorrectly()
    {
        const string secret = "test-checkr-webhook-secret";
        const string payload =
            /*lang=json,strict*/
            """{"id":"evt_1","type":"report.completed","data":{"object":{"id":"rep_99","status":"clear","package":"tasker_standard","candidate_id":"cand_123"}}}""";

        var validSig = CheckrWebhookSignatureVerifier.ComputeSignature(payload, secret);
        Assert.True(CheckrWebhookSignatureVerifier.Verify(payload, validSig, secret));
        Assert.False(CheckrWebhookSignatureVerifier.Verify(payload, "deadbeef", secret));
        Assert.False(CheckrWebhookSignatureVerifier.Verify(payload + " ", validSig, secret));

        var parsed = CheckrWebhookParser.Parse(payload);
        Assert.True(parsed.IsSuccess);
        Assert.Equal("cand_123", parsed.Value.ProviderRequestId);
        Assert.Equal(BackgroundCheckStatus.ReportClear, parsed.Value.MappedStatus);
        Assert.False(parsed.Value.RecordFound);
        Assert.Equal("Candidate Pass", parsed.Value.Recommendation);

        var check = BackgroundCheck.Create(
            BackgroundCheckId.Create(),
            PersonId.Create(),
            "checkr",
            "tasker_standard",
            "cand_123",
            BackgroundCheckStatus.InvitationSent,
            "invitation.created",
            _nowUtc
        );

        check.ApplyWebhookEvent(parsed.Value, _nowUtc.AddMinutes(5));
        Assert.Equal(BackgroundCheckStatus.ReportClear, check.Status);
        Assert.Equal("rep_99", check.ResponseId);

        var lateEvent = CheckrWebhookParser
            .Parse(
                /*lang=json,strict*/
                """{"id":"evt_old","type":"invitation.completed","data":{"object":{"id":"inv_1","status":"completed","candidate_id":"cand_123"}}}"""
            )
            .Value;
        check.ApplyWebhookEvent(lateEvent, _nowUtc.AddMinutes(6));
        Assert.Equal(BackgroundCheckStatus.ReportClear, check.Status);
    }

    [Fact]
    public void IdentityVerification_EnforcesAttemptsLockout_AndTtl()
    {
        var code = IdentityVerificationCode.Create("482910", _nowUtc);
        var verification = IdentityVerification
            .Create("+16025550199", "10.0.0.1", code.Id, _nowUtc)
            .Value;

        Assert.Equal(
            CrmErrors.IdentityVerificationCodeMismatch,
            verification.VerifyCode("000000", code.Code, 15, 2, _nowUtc.AddMinutes(1)).PrimaryError
        );
        Assert.Equal(1, verification.FailedMatchAttemptCount);

        Assert.Equal(
            CrmErrors.IdentityVerificationCodeMismatch,
            verification.VerifyCode("111111", code.Code, 15, 2, _nowUtc.AddMinutes(2)).PrimaryError
        );
        Assert.Equal(2, verification.FailedMatchAttemptCount);

        Assert.Equal(
            CrmErrors.IdentityVerificationExpiredOrLocked,
            verification.VerifyCode("482910", code.Code, 15, 2, _nowUtc.AddMinutes(3)).PrimaryError
        );

        var fresh = IdentityVerification.Create("+16025550199", "10.0.0.1", code.Id, _nowUtc).Value;
        Assert.Equal(
            CrmErrors.IdentityVerificationExpiredOrLocked,
            fresh.VerifyCode("482910", code.Code, 15, 10, _nowUtc.AddMinutes(16)).PrimaryError
        );
        Assert.True(fresh.VerifyCode("482910", code.Code, 15, 10, _nowUtc.AddMinutes(5)).IsSuccess);
        Assert.True(fresh.IsVerified);
    }
}
