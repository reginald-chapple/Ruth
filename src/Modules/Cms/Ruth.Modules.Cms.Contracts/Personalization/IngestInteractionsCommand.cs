using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Cms.Contracts.Personalization;

/// <summary>Command to enqueue one or more interaction events into the high-throughput ingestion buffer.</summary>
public sealed record IngestInteractionsCommand(
    IReadOnlyList<TrackInteractionItemSpec> Items,
    bool FlushImmediately = false
) : ICommand<InteractionBufferStatusResponse>;
