using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.Labels;

/// <summary>Deletes a non-system check-in label definition.</summary>
/// <param name="LabelId">Label identifier to delete.</param>
public sealed record DeleteCheckInLabelCommand(Guid LabelId) : ICommand<bool>;
