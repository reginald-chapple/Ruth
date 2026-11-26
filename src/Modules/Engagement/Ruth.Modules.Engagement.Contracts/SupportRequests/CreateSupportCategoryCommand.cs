using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Engagement.Contracts.SupportRequests;

/// <summary>Command to create a SupportCategory.</summary>
/// <param name="Spec">SupportCategory specification.</param>
public sealed record CreateSupportCategoryCommand(SupportCategorySpec Spec)
    : ICommand<SupportCategoryResponse>;
