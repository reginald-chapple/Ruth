using Microsoft.AspNetCore.Components;
using Ruth.Modules.Communication.Contracts.TwoWaySms;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Host.Components.Pages.Communication;

/// <summary>Staff two-way SMS Conversations inbox and keyword responder pipeline configurator.</summary>
public sealed partial class TwoWaySmsInbox : ComponentBase
{
    private List<SmsConversationSummaryResponse> _conversations = [];
    private List<SmsConversationMessageResponse> _threadMessages = [];
    private List<SmsPipelineResponse> _pipelines = [];

    private SmsConversationSummaryResponse? _selectedConversation;
    private string _replyBody = string.Empty;
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
        var convs = await QueryDispatcher.QueryAsync<
            GetSmsConversationsQuery,
            IReadOnlyList<SmsConversationSummaryResponse>
        >(new GetSmsConversationsQuery(Limit: 50));
        if (convs.IsSuccess)
        {
            _conversations = [.. convs.Value];
        }

        var pipes = await QueryDispatcher.QueryAsync<
            GetSmsPipelinesQuery,
            IReadOnlyList<SmsPipelineResponse>
        >(new GetSmsPipelinesQuery());
        if (pipes.IsSuccess)
        {
            _pipelines = [.. pipes.Value];
        }
    }

    private async Task SelectConversationAsync(SmsConversationSummaryResponse summary)
    {
        _selectedConversation = summary;
        _ = await CommandDispatcher.SendAsync<MarkSmsConversationReadCommand, int>(
            new MarkSmsConversationReadCommand(
                summary.SystemPhoneNumberId,
                summary.ParticipantPhoneNumber
            )
        );
        await LoadThreadAsync(summary.SystemPhoneNumberId, summary.ParticipantPhoneNumber);
        await RefreshAsync();
    }

    private async Task LoadThreadAsync(Guid phoneId, string participantPhone)
    {
        var thread = await QueryDispatcher.QueryAsync<
            GetSmsConversationThreadQuery,
            IReadOnlyList<SmsConversationMessageResponse>
        >(new GetSmsConversationThreadQuery(phoneId, participantPhone));
        if (thread.IsSuccess)
        {
            _threadMessages = [.. thread.Value];
        }
    }

    private async Task SendReplyAsync()
    {
        if (_selectedConversation is null)
        {
            return;
        }

        var res = await CommandDispatcher.SendAsync<
            SendSmsConversationReplyCommand,
            SmsConversationMessageResponse
        >(
            new SendSmsConversationReplyCommand(
                _selectedConversation.SystemPhoneNumberId,
                _selectedConversation.ParticipantPhoneNumber,
                _replyBody
            )
        );

        if (res.IsSuccess)
        {
            _replyBody = string.Empty;
            _isError = false;
            _statusMessage = "SMS reply sent.";
            await LoadThreadAsync(
                _selectedConversation.SystemPhoneNumberId,
                _selectedConversation.ParticipantPhoneNumber
            );
            await RefreshAsync();
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }

    private async Task CreatePipelineAsync(
        (string Name, string Keyword, string ReplyTemplate) input
    )
    {
        var res = await CommandDispatcher.SendAsync<CreateSmsPipelineCommand, SmsPipelineResponse>(
            new CreateSmsPipelineCommand(
                input.Name,
                [
                    new SmsActionInputDto(
                        $"{input.Keyword} Auto-Reply",
                        0,
                        SmsKeywordMatchMode.ExactKeyword,
                        input.Keyword,
                        SmsActionKind.AutoReply,
                        ReplyTemplate: input.ReplyTemplate
                    ),
                ]
            )
        );

        if (res.IsSuccess)
        {
            _isError = false;
            _statusMessage = $"Created SMS pipeline '{res.Value.Name}'.";
            await RefreshAsync();
            return;
        }

        _isError = true;
        _statusMessage = res.PrimaryError.Description;
    }
}
