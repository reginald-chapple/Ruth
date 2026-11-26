using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Finance.Contracts.Pledges;

/// <summary>Command to revise an existing active financial pledge's amount, schedule, or notes.</summary>
/// <param name="PledgeId">Target pledge identifier.</param>
/// <param name="TotalAmount">Revised total pledged amount.</param>
/// <param name="Frequency">Revised installment cadence.</param>
/// <param name="StartDate">Revised start date.</param>
/// <param name="EndDate">Revised end date.</param>
/// <param name="Notes">Revised notes.</param>
public sealed record UpdateFinancialPledgeCommand(
    Guid PledgeId,
    decimal TotalAmount,
    PledgeFrequency Frequency,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Notes = null
) : ICommand<FinancialPledgeResponse>;
