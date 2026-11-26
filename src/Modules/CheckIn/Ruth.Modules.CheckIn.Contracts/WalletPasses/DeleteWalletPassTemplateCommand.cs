using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.CheckIn.Contracts.WalletPasses;

/// <summary>Deletes a non-system smart wallet pass template that has no issued passes.</summary>
/// <param name="TemplateId">Target template identifier.</param>
public sealed record DeleteWalletPassTemplateCommand(Guid TemplateId) : ICommand;
