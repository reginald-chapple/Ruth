using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Ruth.Modules.CheckIn.Contracts.WalletPasses;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.SharedKernel.Messaging;

namespace Ruth.Modules.Commerce.Slices;

internal sealed class WalletPassCommerceSyncCollaborator(
    CommerceDbContext dbContext,
    IQueryDispatcher queryDispatcher,
    ICommandDispatcher commandDispatcher
)
{
    public async Task<int> ComputeActivePunchCreditsAsync(
        Guid personId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .IssuedPunchPasses.AsNoTracking()
            .Where(p =>
                p.OwnerPersonId == personId
                && p.Status == IssuedPunchPassStatus.Active
                && (!p.ExpiresAtUtc.HasValue || p.ExpiresAtUtc > nowUtc)
            )
            .SumAsync(static p => p.RemainingCredits, cancellationToken);
    }

    public async Task SyncPunchPassBalanceAsync(
        Guid personId,
        int totalRemainingCredits,
        CancellationToken cancellationToken
    )
    {
        if (personId == Guid.Empty)
        {
            return;
        }

        var passes = await LoadActiveWalletPassesAsync(personId, cancellationToken);
        foreach (var pass in passes)
        {
            var updatedFields = pass.DynamicFields with
            {
                PunchOrCreditBalance = totalRemainingCredits,
            };
            _ = await commandDispatcher.SendAsync<
                UpdateIssuedWalletPassDynamicFieldsCommand,
                IssuedWalletPassResponse
            >(
                new UpdateIssuedWalletPassDynamicFieldsCommand(
                    pass.Id,
                    updatedFields,
                    pass.ExpiresAtUtc
                ),
                cancellationToken
            );
        }
    }

    public async Task SyncUpcomingBookingForPersonAsync(
        Guid personId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken
    )
    {
        if (personId == Guid.Empty)
        {
            return;
        }

        var passes = await LoadActiveWalletPassesAsync(personId, cancellationToken);
        if (passes.Count == 0)
        {
            return;
        }

        var nextBooking = await dbContext
            .ServiceBookings.AsNoTracking()
            .Where(b =>
                b.CustomerPersonId == personId
                && b.Status == ServiceBookingStatus.Confirmed
                && b.EndUtc > nowUtc
            )
            .OrderBy(static b => b.StartUtc)
            .Select(static b => new
            {
                b.TargetName,
                b.StartUtc,
                b.ConfirmationCode,
            })
            .FirstOrDefaultAsync(cancellationToken);

        var summary = nextBooking is null
            ? null
            : string.Create(
                CultureInfo.InvariantCulture,
                $"{nextBooking.TargetName} · {nextBooking.StartUtc:MMM d HH:mm} UTC ({nextBooking.ConfirmationCode})"
            );

        foreach (var pass in passes)
        {
            var updatedFields = pass.DynamicFields with { NextShiftOrBookingSummary = summary };
            _ = await commandDispatcher.SendAsync<
                UpdateIssuedWalletPassDynamicFieldsCommand,
                IssuedWalletPassResponse
            >(
                new UpdateIssuedWalletPassDynamicFieldsCommand(
                    pass.Id,
                    updatedFields,
                    pass.ExpiresAtUtc
                ),
                cancellationToken
            );
        }
    }

    private async Task<IReadOnlyList<IssuedWalletPassResponse>> LoadActiveWalletPassesAsync(
        Guid personId,
        CancellationToken cancellationToken
    )
    {
        var res = await queryDispatcher.QueryAsync<
            GetIssuedWalletPassesQuery,
            IReadOnlyList<IssuedWalletPassResponse>
        >(new GetIssuedWalletPassesQuery(PersonId: personId, ActiveOnly: true), cancellationToken);

        return res.IsSuccess ? res.Value : [];
    }
}
