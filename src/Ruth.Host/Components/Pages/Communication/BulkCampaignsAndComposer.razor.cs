using System.Globalization;
using Microsoft.AspNetCore.Components;
using Ruth.Modules.Communication.Contracts.BulkCommunications;
using Ruth.Modules.Communication.Contracts.Transports;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Communication;

/// <summary>Staff workspace for structured email composition, bulk campaign approval, and batch dispatch.</summary>
public sealed partial class BulkCampaignsAndComposer : ComponentBase
{
    private static readonly string[] _topics =
    [
        "general-announcements",
        "events-and-programs",
        "volunteer-updates",
        "billing-and-dues",
        "community-digest",
    ];

    private List<CommunicationTemplateResponse> _templates = [];
    private List<CommunicationSummaryResponse> _campaigns = [];

    private Guid _selectedTemplateId;
    private string _title = "Monthly Member & Community Digest";
    private string _topicKey = "community-digest";
    private string _subject = "{{ OrganizationName | default:'Ruth' }} — Community Highlights";
    private string _heroHeading = "Hello {{ RecipientName | default:'Member' }}";
    private string _richBodyMarkup =
        "<p>Explore upcoming workshops, volunteer shifts, and community milestones this month.</p>";
    private string _ctaLabel = "Open Member Portal";
    private string _ctaUrl = "https://example.org/portal";
    private string _recipientsCsv = "alex@example.org, sam@example.org";

    private EmailComposerPreviewResponse? _preview;
    private string? _statusMessage;
    private bool _isError;

    [Inject]
    private IQueryDispatcher QueryDispatcher { get; set; } = default!;

    [Inject]
    private ICommandDispatcher CommandDispatcher { get; set; } = default!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        var templates = await QueryDispatcher.QueryAsync<
            GetCommunicationTemplatesQuery,
            IReadOnlyList<CommunicationTemplateResponse>
        >(new GetCommunicationTemplatesQuery(ActiveOnly: true));
        if (templates.IsSuccess)
        {
            _templates = [.. templates.Value];
        }

        var campaigns = await QueryDispatcher.QueryAsync<
            GetCommunicationsQuery,
            IReadOnlyList<CommunicationSummaryResponse>
        >(new GetCommunicationsQuery(Limit: 30));
        if (campaigns.IsSuccess)
        {
            _campaigns = [.. campaigns.Value];
        }
    }

    private Task OnTemplateSelectedAsync(Guid templateId)
    {
        _selectedTemplateId = templateId;
        var tpl = _templates.FirstOrDefault(t => t.Id == templateId);
        if (tpl is not null)
        {
            _topicKey = tpl.TopicKey;
            _subject = tpl.SubjectTemplate ?? _subject;
            var hero = tpl.Sections.FirstOrDefault(static s =>
                s.Kind == EmailSectionKind.HeroBanner
            );
            if (hero?.Heading is not null)
            {
                _heroHeading = hero.Heading;
            }
        }

        return Task.CompletedTask;
    }

    private List<EmailSectionDto> BuildSections()
    {
        var list = new List<EmailSectionDto>
        {
            new(0, EmailSectionKind.HeroBanner, Heading: _heroHeading),
            new(1, EmailSectionKind.RichText, BodyMarkup: _richBodyMarkup),
        };
        if (!string.IsNullOrWhiteSpace(_ctaLabel) && !string.IsNullOrWhiteSpace(_ctaUrl))
        {
            list.Add(
                new EmailSectionDto(
                    2,
                    EmailSectionKind.CallToActionButton,
                    ButtonLabel: _ctaLabel,
                    ButtonUrl: _ctaUrl
                )
            );
        }

        return list;
    }

    private async Task PreviewComposerAsync()
    {
        var result = await CommandDispatcher.SendAsync<
            PreviewEmailComposerCommand,
            EmailComposerPreviewResponse
        >(new PreviewEmailComposerCommand(_subject, BuildSections()));

        if (result.IsSuccess)
        {
            _preview = result.Value;
            _isError = false;
            _statusMessage = "Compiled structured email preview.";
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task CreateDraftCampaignAsync()
    {
        var recipients = _recipientsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static addr => new CommunicationRecipientInputDto(DestinationAddress: addr))
            .ToArray();

        var result = await CommandDispatcher.SendAsync<
            CreateCommunicationCommand,
            CommunicationDetailResponse
        >(
            new CreateCommunicationCommand(
                _title,
                CommunicationChannel.Email,
                _topicKey,
                IsBulk: true,
                TemplateId: _selectedTemplateId == Guid.Empty ? null : _selectedTemplateId,
                Subject: _subject,
                Sections: BuildSections(),
                Recipients: recipients
            )
        );

        if (result.IsSuccess)
        {
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Created draft '{result.Value.Title}' with {result.Value.Recipients.Count} recipient(s)."
            );
            await RefreshAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }

    private async Task SubmitCampaignAsync(Guid id)
    {
        var res = await CommandDispatcher.SendAsync<
            SubmitCommunicationForApprovalCommand,
            CommunicationDetailResponse
        >(new SubmitCommunicationForApprovalCommand(id));
        await HandleLifecycleResultAsync(res, "Campaign submitted for approval.");
    }

    private async Task ApproveCampaignAsync(Guid id)
    {
        var res = await CommandDispatcher.SendAsync<
            ApproveCommunicationCommand,
            CommunicationDetailResponse
        >(new ApproveCommunicationCommand(id, ApprovalNote: "Approved from Staff Workspace"));
        await HandleLifecycleResultAsync(res, "Campaign approved for batch sending.");
    }

    private async Task RejectCampaignAsync(Guid id)
    {
        var res = await CommandDispatcher.SendAsync<
            RejectCommunicationCommand,
            CommunicationDetailResponse
        >(new RejectCommunicationCommand(id, "Returned for copy edits."));
        await HandleLifecycleResultAsync(res, "Campaign returned to Rejected state.");
    }

    private async Task DispatchCampaignAsync(Guid id)
    {
        var res = await CommandDispatcher.SendAsync<
            ProcessDueBulkCommunicationsCommand,
            BulkDispatchBatchResultResponse
        >(new ProcessDueBulkCommunicationsCommand(SpecificCommunicationId: id));
        if (res.IsSuccess)
        {
            _isError = false;
            _statusMessage = string.Create(
                CultureInfo.InvariantCulture,
                $"Batch dispatched: Delivered={res.Value.RecipientsDelivered}, Skipped={res.Value.RecipientsSkipped}."
            );
            await RefreshAsync();
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private async Task HandleLifecycleResultAsync(
        SharedKernel.Results.Result<CommunicationDetailResponse> result,
        string successMessage
    )
    {
        if (result.IsSuccess)
        {
            _isError = false;
            _statusMessage = successMessage;
            await RefreshAsync();
            return;
        }

        _isError = true;
        _statusMessage = result.PrimaryError.Description;
    }
}
