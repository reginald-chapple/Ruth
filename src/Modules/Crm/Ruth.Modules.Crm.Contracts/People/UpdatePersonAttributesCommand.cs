using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.People;

/// <summary>Command to update extensible JSONB attributes on a person.</summary>
public sealed record UpdatePersonAttributesCommand(
    Guid PersonId,
    IReadOnlyDictionary<string, string?> Attributes
) : ICommand<PersonDetailResponse>;
