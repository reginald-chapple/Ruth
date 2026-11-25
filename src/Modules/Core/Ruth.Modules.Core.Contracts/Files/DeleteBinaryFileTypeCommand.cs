using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Deletes a non-system binary file type with no stored files.</summary>
public sealed record DeleteBinaryFileTypeCommand(Guid BinaryFileTypeId) : ICommand<bool>;
