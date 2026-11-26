using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Communication.Contracts.TwoWaySms;

/// <summary>Creates a new SmsPipeline keyword responder with ordered <see cref="SmsActionInputDto" /> rules.</summary>
/// <param name="Name">Pipeline display name.</param>
/// <param name="Actions">Ordered keyword action rules.</param>
/// <param name="Description">Optional description.</param>
/// <param name="SystemPhoneNumberId">Optional E.164 system phone number filter.</param>
/// <param name="PriorityOrder">Zero-based evaluation priority.</param>
/// <param name="IsActive">Whether the pipeline is active.</param>
public sealed record CreateSmsPipelineCommand(
    string Name,
    IReadOnlyList<SmsActionInputDto> Actions,
    string? Description = null,
    Guid? SystemPhoneNumberId = null,
    int PriorityOrder = 10,
    bool IsActive = true
) : ICommand<SmsPipelineResponse>;
