using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Gets a single check-in label definition by identifier.</summary>
/// <param name="LabelId">Label identifier.</param>
public sealed record GetCheckInLabelByIdQuery(Guid LabelId) : IQuery<CheckInLabelResponse>;
