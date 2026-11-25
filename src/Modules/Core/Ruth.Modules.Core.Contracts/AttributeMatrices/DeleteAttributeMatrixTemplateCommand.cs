using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.AttributeMatrices;

/// <summary>Deletes an attribute matrix template.</summary>
public sealed record DeleteAttributeMatrixTemplateCommand(Guid TemplateId) : ICommand<bool>;
