namespace Ruth.Modules.Reporting.Contracts.MergeTemplates;

/// <summary>Persisted MergeTemplate definition for HTML/DOCX document generation.</summary>
/// <param name="Id">MergeTemplate identifier.</param>
/// <param name="Name">Template name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Format">Document format (Html or Docx).</param>
/// <param name="TemplateBody">Safe merge-token template body evaluated per row.</param>
/// <param name="HeaderMarkup">Optional header markup rendered once before rows.</param>
/// <param name="FooterMarkup">Optional footer markup rendered once after rows.</param>
/// <param name="CategoryId">Optional category identifier.</param>
/// <param name="IsGlobal">Whether available organization-wide.</param>
/// <param name="OwnerPersonId">Optional personal owner PersonId.</param>
/// <param name="IsActive">Whether the template is active.</param>
public sealed record MergeTemplateResponse(
    Guid Id,
    string Name,
    string? Description,
    MergeTemplateFormat Format,
    string TemplateBody,
    string? HeaderMarkup,
    string? FooterMarkup,
    Guid? CategoryId,
    bool IsGlobal,
    Guid? OwnerPersonId,
    bool IsActive
);
