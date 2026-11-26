using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Reporting.Contracts.MergeTemplates;

/// <summary>Creates a DOCX or HTML MergeTemplate.</summary>
/// <param name="Name">Template name.</param>
/// <param name="Format">Target format (Html or Docx).</param>
/// <param name="TemplateBody">Safe merge-token template body.</param>
/// <param name="Description">Optional description.</param>
/// <param name="HeaderMarkup">Optional header markup.</param>
/// <param name="FooterMarkup">Optional footer markup.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="IsGlobal">Whether available globally.</param>
/// <param name="OwnerPersonId">Optional owner PersonId.</param>
public sealed record CreateMergeTemplateCommand(
    string Name,
    MergeTemplateFormat Format,
    string TemplateBody,
    string? Description = null,
    string? HeaderMarkup = null,
    string? FooterMarkup = null,
    Guid? CategoryId = null,
    bool IsGlobal = true,
    Guid? OwnerPersonId = null
) : ICommand<MergeTemplateResponse>;
