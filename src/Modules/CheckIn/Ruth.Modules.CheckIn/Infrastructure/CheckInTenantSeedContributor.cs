using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.SharedKernel.Tenancy;

namespace Ruth.Modules.CheckIn.Infrastructure;

internal sealed class CheckInTenantSeedContributor(CheckInDbContext dbContext)
    : ITenantSeedContributor
{
    public int Order => 40;

    public Task SeedTenantAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return SeedSystemLabelsAsync(dbContext, cancellationToken);
    }

    internal static async Task SeedSystemLabelsAsync(
        CheckInDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var existingKeys = await dbContext
            .CheckInLabels.AsNoTracking()
            .Select(static l => l.Key)
            .ToListAsync(cancellationToken);
        var keySet = existingKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

        CheckInLabelSpec[] defaults =
        [
            new(
                "Standard Participant Safety Badge",
                "participant-safety-badge",
                CheckInLabelKind.ParticipantBadge,
                CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto(),
                "Default 4x2 thermal participant name, room, security code, and medical alert badge.",
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 0,
                Copies: 1,
                IsActive: true,
                IsSystem: true
            ),
            new(
                "Standard Guardian Pickup Receipt",
                "guardian-pickup-receipt",
                CheckInLabelKind.GuardianPickupReceipt,
                CheckInLabelLayoutSchema.CreateDefaultGuardianReceiptDto(),
                "Default 4x2 thermal guardian security pickup claim receipt with Code 128 barcode.",
                ConfigurationId: null,
                AreaId: null,
                CustomZplOverride: null,
                PrintOrder: 10,
                Copies: 1,
                IsActive: true,
                IsSystem: true
            ),
        ];

        var added = false;
        foreach (var spec in defaults)
        {
            if (keySet.Contains(spec.Key))
            {
                continue;
            }

            var created = CheckInLabel.Create(spec);
            if (created.IsSuccess)
            {
                _ = dbContext.CheckInLabels.Add(created.Value);
                added = true;
            }
        }

        if (added)
        {
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
