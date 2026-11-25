using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Contracts.People;
using Ruth.Modules.Crm.Domain;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure;
using Ruth.SharedKernel.Messaging;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Crm.Slices.People;

internal sealed class GetMetaNameInsightsQueryHandler(CrmDbContext dbContext)
    : IQueryHandler<GetMetaNameInsightsQuery, MetaNameInsightsResponse>
{
    public async Task<Result<MetaNameInsightsResponse>> HandleAsync(
        GetMetaNameInsightsQuery query,
        CancellationToken cancellationToken = default
    )
    {
        var sanitized = CrmTextPolicy.SanitizePersonName(query.FirstName);
        var upper = sanitized.ToUpperInvariant();

        var genderRow = await dbContext
            .MetaFirstNameGenderLookups.AsNoTracking()
            .FirstOrDefaultAsync(g => g.NormalizedFirstName == upper, cancellationToken);

        var nickRows = await dbContext
            .MetaNickNameLookups.AsNoTracking()
            .Where(n => n.NormalizedFirstName == upper || n.NormalizedNickName == upper)
            .ToListAsync(cancellationToken);

        var related = nickRows
            .SelectMany(static n => new[] { n.FirstName, n.NickName })
            .Where(n => !string.Equals(n, sanitized, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var metaphone = DoubleMetaphoneEncoder.Encode(sanitized);

        return Result.Success(
            new MetaNameInsightsResponse(
                sanitized,
                genderRow?.PredictGender() ?? Gender.Unknown,
                genderRow?.MalePercent,
                genderRow?.FemalePercent,
                related,
                metaphone.PrimaryKey,
                metaphone.AlternateKey
            )
        );
    }
}
