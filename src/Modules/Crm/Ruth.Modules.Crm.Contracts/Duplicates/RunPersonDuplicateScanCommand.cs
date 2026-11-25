using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Crm.Contracts.Duplicates;

/// <summary>Command to trigger an on-demand duplicate scan across active person records.</summary>
public sealed record RunPersonDuplicateScanCommand(bool IncludeInactive = false)
    : ICommand<DuplicateScanSummaryResponse>;
