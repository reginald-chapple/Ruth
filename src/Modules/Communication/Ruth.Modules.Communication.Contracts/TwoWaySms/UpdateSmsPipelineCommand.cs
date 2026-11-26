using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Updates an existing SmsPipeline and replaces its ordered <see cref="SmsActionInputDto" /> rules.</summary>
/// <param name="PipelineId">Target pipeline identifier.</param>
/// <param name="Name">Updated pipeline name.</param>
/// <param name="Actions">Updated ordered keyword action rules.</param>
/// <param name="Description">Optional description.</param>
/// <param name="SystemPhoneNumberId">Optional E.164 system phone number filter.</param>
/// <param name="PriorityOrder">Zero-based evaluation priority.</param>
/// <param name="IsActive">Whether the pipeline is active.</param>
public sealed record UpdateSmsPipelineCommand(
    Guid PipelineId,
    string Name,
    IReadOnlyList<SmsActionInputDto> Actions,
    string? Description = null,
    Guid? SystemPhoneNumberId = null,
    int PriorityOrder = 10,
    bool IsActive = true
) : ICommand<SmsPipelineResponse>;
