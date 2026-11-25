using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Core.Contracts.Files;

/// <summary>Deletes a binary file from storage and metadata tables.</summary>
public sealed record DeleteBinaryFileCommand(Guid BinaryFileId) : ICommand<bool>;
