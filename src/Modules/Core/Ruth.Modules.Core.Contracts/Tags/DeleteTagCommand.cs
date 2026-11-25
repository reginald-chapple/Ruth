using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Tags;

/// <summary>Deletes a non-system tag and its tagged item associations.</summary>
public sealed record DeleteTagCommand(Guid TagId, Guid? RequestingUserId = null) : ICommand<bool>;
