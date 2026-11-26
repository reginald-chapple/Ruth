using Microsoft.EntityFrameworkCore;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.People;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.Crm.Infrastructure;

internal sealed class CrmTenantSeedContributor(CrmDbContext dbContext) : ITenantSeedContributor
{
    public int Order => 30;

    public Task SeedTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return SeedReferenceCatalogsAsync(dbContext, tenantId, cancellationToken);
    }

    internal static async Task SeedReferenceCatalogsAsync(
        CrmDbContext dbContext,
        TenantId tenantId,
        CancellationToken cancellationToken
    )
    {
        await SeedMetaLookupsAsync(dbContext, tenantId, cancellationToken);
        await SeedDefaultBadgeDefinitionsAsync(dbContext, cancellationToken);
        await SeedDefaultAssessmentTypesAsync(dbContext, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMetaLookupsAsync(
        CrmDbContext dbContext,
        TenantId tenantId,
        CancellationToken cancellationToken
    )
    {
        if (!await dbContext.MetaFirstNameGenderLookups.AnyAsync(cancellationToken))
        {
            dbContext.MetaFirstNameGenderLookups.AddRange(
                MetaLookupReferenceData.BuildGenderRows(tenantId)
            );
        }

        if (!await dbContext.MetaNickNameLookups.AnyAsync(cancellationToken))
        {
            dbContext.MetaNickNameLookups.AddRange(
                MetaLookupReferenceData.BuildNickNameRows(tenantId)
            );
        }
    }

    private static async Task SeedDefaultBadgeDefinitionsAsync(
        CrmDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.BadgeDefinitions.AnyAsync(cancellationToken))
        {
            return;
        }

        (string Name, string Key, int Order)[] defaults =
        [
            ("Top Signal", TopSignalBadgeEvaluator.Key, 0),
            ("Connection Status", ConnectionStatusBadgeEvaluator.Key, 1),
            ("Age & Grade", AgeAndBirthdayBadgeEvaluator.Key, 2),
            ("Assessments", AssessmentSummaryBadgeEvaluator.Key, 3),
            ("Background Check", BackgroundCheckStatusBadgeEvaluator.Key, 4),
            ("Account Protection", ProtectionProfileBadgeEvaluator.Key, 5),
        ];

        foreach (var (name, key, order) in defaults)
        {
            var badge = BadgeDefinition.Create(name, key, order: order);
            if (badge.IsSuccess)
            {
                _ = dbContext.BadgeDefinitions.Add(badge.Value);
            }
        }
    }

    private static async Task SeedDefaultAssessmentTypesAsync(
        CrmDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (await dbContext.AssessmentTypes.AnyAsync(cancellationToken))
        {
            return;
        }

        (string Key, string Title, string Path, string Color)[] defaults =
        [
            ("team-strengths", "Team Strengths", "/crm/assessments/team-strengths", "#4F6BED"),
            ("disc", "DISC Personality", "/crm/assessments/disc", "#0078D4"),
            ("conflict-theme", "Conflict Theme", "/crm/assessments/conflict", "#CA5010"),
            ("motivators", "Motivators", "/crm/assessments/motivators", "#107C10"),
            ("eq-inventory", "Emotional Intelligence", "/crm/assessments/eq", "#8764B8"),
        ];

        foreach (var (key, title, path, color) in defaults)
        {
            var type = AssessmentType.Create(
                key,
                title,
                path,
                minimumDaysToRetake: 90,
                isSystem: true,
                badgeColor: color
            );
            if (type.IsSuccess)
            {
                _ = dbContext.AssessmentTypes.Add(type.Value);
            }
        }
    }
}
