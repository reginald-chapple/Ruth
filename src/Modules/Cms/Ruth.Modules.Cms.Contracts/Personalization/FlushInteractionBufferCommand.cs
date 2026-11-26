using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to flush all currently buffered interactions into PostgreSQL.</summary>
public sealed record FlushInteractionBufferCommand : ICommand<InteractionBufferStatusResponse>;
