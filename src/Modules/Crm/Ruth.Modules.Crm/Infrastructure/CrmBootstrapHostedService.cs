using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ruth.Modules.Crm.Domain.Assessments;
using Ruth.Modules.Crm.Domain.Badges;
using Ruth.Modules.Crm.Domain.People;
using Ruth.Modules.Crm.Infrastructure.Graph;

namespace Ruth.Modules.Crm.Infrastructure;

internal sealed partial class CrmBootstrapHostedService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ICrmGraphService graphService,
    ILogger<CrmBootstrapHostedService> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue<bool>("Bootstrap:AutoMigrateAndSeed"))
        {
            return;
        }

        LogApplyingMigrations(logger);
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CrmDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);
        await graphService.EnsureGraphInitializedAsync(cancellationToken);
        await SeedReferenceCatalogsAsync(dbContext, cancellationToken);
        LogMigrationsCompleted(logger);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    internal static async Task SeedReferenceCatalogsAsync(
        CrmDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        await SeedMetaLookupsAsync(dbContext, cancellationToken);
        await SeedDefaultBadgeDefinitionsAsync(dbContext, cancellationToken);
        await SeedDefaultAssessmentTypesAsync(dbContext, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMetaLookupsAsync(
        CrmDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (!await dbContext.MetaFirstNameGenderLookups.AnyAsync(cancellationToken))
        {
            dbContext.MetaFirstNameGenderLookups.AddRange(
                MetaLookupReferenceData.BuildGenderRows()
            );
        }

        if (!await dbContext.MetaNickNameLookups.AnyAsync(cancellationToken))
        {
            dbContext.MetaNickNameLookups.AddRange(MetaLookupReferenceData.BuildNickNameRows());
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
            ("spiritual-gifts", "Spiritual Gifts", "/crm/assessments/spiritual-gifts", "#4F6BED"),
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

    [LoggerMessage(
        EventId = 5103,
        Level = LogLevel.Information,
        Message = "Applying CRM schema EF Core migrations, Apache AGE graph init, and reference catalogs."
    )]
    private static partial void LogApplyingMigrations(ILogger logger);

    [LoggerMessage(
        EventId = 5104,
        Level = LogLevel.Information,
        Message = "CRM schema migrations and reference catalog seeding completed."
    )]
    private static partial void LogMigrationsCompleted(ILogger logger);
}
