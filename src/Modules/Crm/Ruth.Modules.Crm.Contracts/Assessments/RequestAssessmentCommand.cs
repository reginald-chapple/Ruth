using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Assessments;

/// <summary>Command to request or self-initiate an assessment for a person.</summary>
public sealed record RequestAssessmentCommand(
    Guid PersonId,
    Guid AssessmentTypeId,
    Guid? RequesterPersonId = null,
    DateOnly? RequestedDueDate = null
) : ICommand<AssessmentResponse>;
