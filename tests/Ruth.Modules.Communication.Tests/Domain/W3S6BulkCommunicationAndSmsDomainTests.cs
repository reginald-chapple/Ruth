using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.Modules.Communication.Domain.BulkCommunications;
using Ruth.Modules.Communication.Domain.TwoWaySms;
using Xunit;

namespace Ruth.Modules.Communication.Tests.Domain;

public sealed class W3S6BulkCommunicationAndSmsDomainTests
{
    [Fact]
    public void EmailSectionComposer_ValidatesAllSectionKinds_BlocksUnsafeUrls_AndProducesValidHtml()
    {
        var unsafeSection = EmailSection.ValidateAndNormalize([
            new EmailSectionDto(
                0,
                EmailSectionKind.CallToActionButton,
                ButtonLabel: "Click",
                ButtonUrl: "javascript:alert(1)"
            ),
        ]);
        Assert.True(unsafeSection.IsFailure);

        var missingAlt = EmailSection.ValidateAndNormalize([
            new EmailSectionDto(
                0,
                EmailSectionKind.ImageBlock,
                ImageUrl: "https://example.org/banner.png"
            ),
        ]);
        Assert.True(missingAlt.IsFailure);

        var badHex = EmailSection.ValidateAndNormalize([
            new EmailSectionDto(0, EmailSectionKind.Divider, AccentColorHex: "not-a-color"),
        ]);
        Assert.True(badHex.IsFailure);

        var validSections = EmailSection.ValidateAndNormalize([
            new EmailSectionDto(
                0,
                EmailSectionKind.HeroBanner,
                Heading: "Hello {{ RecipientName }}",
                BodyMarkup: "Welcome aboard.",
                AccentColorHex: "#1E40AF"
            ),
            new EmailSectionDto(
                1,
                EmailSectionKind.RichText,
                Heading: "Details & Updates",
                BodyMarkup: "<p>Program update.</p>"
            ),
            new EmailSectionDto(
                2,
                EmailSectionKind.ImageBlock,
                ImageUrl: "https://example.org/hero.jpg",
                ImageAltText: "Community workshop banner"
            ),
            new EmailSectionDto(
                3,
                EmailSectionKind.TwoColumnText,
                BodyMarkup: "Left column",
                SecondaryColumnMarkup: "Right column"
            ),
            new EmailSectionDto(4, EmailSectionKind.Divider),
            new EmailSectionDto(
                5,
                EmailSectionKind.CallToActionButton,
                ButtonLabel: "Register Now",
                ButtonUrl: "{{ ActionUrl }}"
            ),
            new EmailSectionDto(6, EmailSectionKind.Footer, BodyMarkup: "123 Civic Way"),
        ]);
        Assert.True(validSections.IsSuccess);
        Assert.Equal(7, validSections.Value.Count);

        var html = EmailSectionHtmlComposer.ComposeBodyMarkup(
            validSections.Value,
            includeUnsubscribeFooter: true
        );
        Assert.Contains("#1e40af", html, StringComparison.Ordinal);
        Assert.Contains("alt=\"Community workshop banner\"", html, StringComparison.Ordinal);
        Assert.Contains("href=\"{{ ActionUrl }}\"", html, StringComparison.Ordinal);
        Assert.Contains("UnsubscribeUrl", html, StringComparison.Ordinal);
    }

    [Fact]
    public void CommunicationLifecycle_EnforcesTransitions_ContentAndRecipientGuards()
    {
        var commResult = Communication.Domain.BulkCommunications.Communication.CreateDraft(
            new CommunicationDraftSpec(
                "Annual Renewal Notice",
                CommunicationChannel.Email,
                TopicKey: "billing-and-dues",
                IsBulk: true,
                Subject: "Renew your membership",
                Sections:
                [
                    new EmailSectionDto(
                        0,
                        EmailSectionKind.RichText,
                        Heading: "Renewal",
                        BodyMarkup: "<p>Thank you!</p>"
                    ),
                ]
            )
        );
        Assert.True(commResult.IsSuccess);
        var comm = commResult.Value;

        // Zero recipients blocked
        Assert.True(comm.SubmitForApproval(0).IsFailure);
        Assert.True(comm.Approve(Guid.CreateVersion7(), "ok", 0).IsFailure);

        // Submit -> Reject -> Edit -> Submit -> Approve -> Sending -> Sent
        Assert.True(comm.SubmitForApproval(3).IsSuccess);
        Assert.Equal(CommunicationStatus.PendingApproval, comm.Status);

        Assert.True(comm.Reject(Guid.CreateVersion7(), string.Empty).IsFailure);
        Assert.True(comm.Reject(Guid.CreateVersion7(), "Add deadline date").IsSuccess);
        Assert.Equal(CommunicationStatus.Rejected, comm.Status);

        Assert.True(
            comm.UpdateDraft(
                new CommunicationDraftSpec(
                    "Annual Renewal Notice v2",
                    CommunicationChannel.Email,
                    TopicKey: "billing-and-dues",
                    Subject: "Renew by Friday",
                    HtmlBodyOverride: "<p>Updated</p>"
                )
            ).IsSuccess
        );
        Assert.Equal(CommunicationStatus.Draft, comm.Status);

        Assert.True(comm.Approve(Guid.CreateVersion7(), "Looks good", 3).IsSuccess);
        Assert.Equal(CommunicationStatus.Approved, comm.Status);
        Assert.True(
            comm.UpdateDraft(
                new CommunicationDraftSpec("Should fail", CommunicationChannel.Email)
            ).IsFailure
        );

        Assert.True(comm.BeginSending().IsSuccess);
        Assert.Equal(CommunicationStatus.Sending, comm.Status);
        Assert.True(comm.Cancel("Too late").IsFailure);

        var sentAt = DateTimeOffset.UtcNow;
        Assert.True(comm.CompleteSending(sentAt).IsSuccess);
        Assert.Equal(CommunicationStatus.Sent, comm.Status);
        _ = Assert.NotNull(comm.SentAtUtc);
    }

    [Fact]
    public void Preferences_TokenSigner_AndSmsKeywordMatcher_EnforceInvariants()
    {
        const string Secret = "unit-test-hmac-secret-key-0123456789";
        var personId = Guid.CreateVersion7();
        var token = PreferenceTokenSigner.Sign(
            CommunicationChannel.Email,
            "member@example.org",
            personId,
            Secret
        );
        var verified = PreferenceTokenSigner.Verify(token, Secret);
        Assert.True(verified.IsSuccess);
        Assert.Equal(CommunicationChannel.Email, verified.Value.Channel);
        Assert.Equal("member@example.org", verified.Value.NormalizedDestination);
        Assert.Equal(personId, verified.Value.PersonId);
        Assert.True(PreferenceTokenSigner.Verify(token + "tamper", Secret).IsFailure);

        var pref = CommunicationSubscriptionPreference
            .Create(CommunicationChannel.Email, "Member@Example.org", personId)
            .Value;
        pref.ApplyOneClickOptOut("events-and-programs", "OneClick");
        Assert.True(
            CommunicationSubscriptionPreference
                .EvaluateSuppression(isBulk: true, "events-and-programs", pref)
                .IsSuppressed
        );
        Assert.False(
            CommunicationSubscriptionPreference
                .EvaluateSuppression(isBulk: true, "billing-and-dues", pref)
                .IsSuppressed
        );

        // Hard bounce blocks even non-bulk transactional sends
        pref.ApplyOneClickOptOut("*", "HardBounce");
        Assert.True(
            CommunicationSubscriptionPreference
                .EvaluateSuppression(isBulk: false, "billing-and-dues", pref)
                .IsSuppressed
        );

        // SMS keywords & pipelines
        Assert.True(SmsKeywordMatcher.IsMandatoryOptOutKeyword(" stop "));
        Assert.True(SmsKeywordMatcher.IsMandatoryOptInKeyword("START"));
        Assert.False(SmsKeywordMatcher.IsMandatoryOptOutKeyword("STOP BY LATER"));

        var pipeline = SmsPipeline
            .Create(
                "Event Responder",
                [
                    new SmsActionInputDto(
                        "RSVP StartsWith",
                        0,
                        SmsKeywordMatchMode.StartsWithKeyword,
                        "RSVP",
                        SmsActionKind.AutoReply,
                        ReplyTemplate: "Confirmed!",
                        ContinueProcessing: true
                    ),
                    new SmsActionInputDto(
                        "Fallback Audit",
                        1,
                        SmsKeywordMatchMode.FallbackAny,
                        Keyword: null,
                        SmsActionKind.AutoReply,
                        ReplyTemplate: "Logged."
                    ),
                ],
                isSystem: true
            )
            .Value;

        Assert.True(pipeline.EnsureCanDelete().IsFailure);
        var matched = SmsKeywordMatcher.MatchActions(Guid.Empty, "rsvp yes 2 guests", [pipeline]);
        Assert.Equal(2, matched.Count);
    }

    [Fact]
    public void DomainEdgeCases_CoverTemplates_Preferences_Tokens_Recipients_AndSmsActionModes()
    {
        Assert.True(PreferenceTokenSigner.Verify(token: null, "secret").IsFailure);
        Assert.True(PreferenceTokenSigner.Verify("nodot", "secret").IsFailure);
        Assert.True(PreferenceTokenSigner.Verify("%%%invalid%%%.%%%", "secret").IsFailure);

        Assert.True(
            CommunicationRecipient.NormalizeDestination(CommunicationChannel.Email, "bad").IsFailure
        );
        Assert.True(
            CommunicationRecipient.NormalizeDestination(CommunicationChannel.Sms, "abc").IsFailure
        );
        Assert.True(
            CommunicationRecipient
                .NormalizeDestination(CommunicationChannel.Push, "short")
                .IsFailure
        );
        Assert.True(
            CommunicationRecipient
                .NormalizeDestination(CommunicationChannel.Push, "valid-push-token-12345")
                .IsSuccess
        );

        var pref = CommunicationSubscriptionPreference
            .Create(CommunicationChannel.Email, "user@example.org")
            .Value;
        var pid = Guid.CreateVersion7();
        pref.ApplyPreferences(globalOptOut: false, ["events-and-programs", " "], "UserChoice", pid);
        Assert.Equal(pid, pref.PersonId);
        Assert.Contains("events-and-programs", pref.OptedOutTopics, StringComparer.Ordinal);

        Assert.True(
            CommunicationTemplate
                .Create(
                    new CommunicationTemplateSpec(
                        "",
                        "valid-key",
                        CommunicationChannel.Sms,
                        SmsBodyTemplate: "Hi"
                    )
                )
                .IsFailure
        );
        Assert.True(
            CommunicationTemplate
                .Create(
                    new CommunicationTemplateSpec(
                        "SMS Alert",
                        "sms-alert",
                        CommunicationChannel.Sms,
                        TopicKey: "BAD TOPIC!",
                        SmsBodyTemplate: "Hi"
                    )
                )
                .IsFailure
        );
        Assert.True(
            CommunicationTemplate
                .Create(
                    new CommunicationTemplateSpec(
                        "Push Alert",
                        "push-alert",
                        CommunicationChannel.Push,
                        PushTitleTemplate: "Alert",
                        PushBodyTemplate: "Body"
                    )
                )
                .IsSuccess
        );

        var containsPipeline = SmsPipeline
            .Create(
                "Contains Pipeline",
                [
                    new SmsActionInputDto(
                        "Contains Help",
                        0,
                        SmsKeywordMatchMode.ContainsKeyword,
                        "ASSIST",
                        SmsActionKind.AutoReply,
                        ReplyTemplate: "How can we help?"
                    ),
                    new SmsActionInputDto(
                        "Exact Exact",
                        1,
                        SmsKeywordMatchMode.ExactKeyword,
                        "PING",
                        SmsActionKind.AutoReply,
                        ReplyTemplate: "PONG",
                        IsActive: false
                    ),
                ]
            )
            .Value;
        _ = Assert.Single(
            SmsKeywordMatcher.MatchActions(Guid.Empty, "Need assist please", [containsPipeline])
        );
        Assert.Empty(SmsKeywordMatcher.MatchActions(Guid.Empty, "PING", [containsPipeline]));

        Assert.True(SmsPipeline.Create("Empty Actions", actions: []).IsFailure);
        Assert.True(
            SmsPipeline
                .Create(
                    "Invalid Action",
                    [
                        new SmsActionInputDto(
                            "",
                            0,
                            SmsKeywordMatchMode.ExactKeyword,
                            "HI",
                            SmsActionKind.AutoReply
                        ),
                    ]
                )
                .IsFailure
        );

        var smsComm = Communication
            .Domain.BulkCommunications.Communication.CreateDraft(
                new CommunicationDraftSpec(
                    "SMS Blast",
                    CommunicationChannel.Sms,
                    SmsBody: "Snow closure update"
                )
            )
            .Value;
        Assert.True(smsComm.SubmitForApproval(2).IsSuccess);

        var pushComm = Communication
            .Domain.BulkCommunications.Communication.CreateDraft(
                new CommunicationDraftSpec(
                    "Push Blast",
                    CommunicationChannel.Push,
                    PushTitle: "Urgent",
                    PushBody: "Check app"
                )
            )
            .Value;
        Assert.True(pushComm.SubmitForApproval(2).IsSuccess);
    }
}
