using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Engine;

/// <summary>Command to register a CMS block type definition.</summary>
public sealed record RegisterCmsBlockTypeCommand(
    string Key,
    string Name,
    string Category,
    string? Description = null,
    bool RequiresInteractivity = false,
    IReadOnlyDictionary<string, string?>? DefaultSettings = null
) : ICommand<CmsBlockTypeResponse>;
