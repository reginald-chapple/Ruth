using System.Text;
using Ruth.Modules.Core.Contracts.Files;
using Ruth.Modules.Core.Contracts.Locations;
using Ruth.Modules.Finance.Contracts.Accounts;
using Ruth.Modules.Finance.Contracts.Gateways;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Commerce.Tests.Infrastructure;

internal static class CommerceTestSeedHelper
{
    public static async Task<CommerceSeedBundle> SeedAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var nonDed = await queries.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: false));
        Assert.True(nonDed.IsSuccess);

        var ded = await queries.QueryAsync<
            GetFinancialAccountsQuery,
            IReadOnlyList<FinancialAccountResponse>
        >(new GetFinancialAccountsQuery(ActiveOnly: true, IsTaxDeductible: true));
        Assert.True(ded.IsSuccess);

        var gw = await queries.QueryAsync<
            GetFinancialGatewaysQuery,
            IReadOnlyList<FinancialGatewayResponse>
        >(new GetFinancialGatewaysQuery(ActiveOnly: true));
        Assert.True(gw.IsSuccess);

        var loc = await commands.SendAsync<CreateLocationCommand, LocationResponse>(
            new CreateLocationCommand(Name: "Courtroom & Studio B", IsActive: true)
        );
        Assert.True(loc.IsSuccess);

        var (groupTypeId, groupId, memberId, partnerId, walletPassId) =
            await CommerceConstituentSeedHelper.SeedPeopleHouseholdGroupAndWalletAsync(commands);
        var fileId = await SeedDigitalBinaryFileAsync(commands);

        return new CommerceSeedBundle(
            nonDed.Value[0].Id,
            ded.Value[0].Id,
            gw.Value[0].Id,
            loc.Value.Id,
            groupTypeId,
            groupId,
            fileId,
            memberId,
            partnerId,
            walletPassId
        );
    }

    private static async Task<Guid> SeedDigitalBinaryFileAsync(ICommandDispatcher commands)
    {
        var fileType = await commands.SendAsync<
            CreateBinaryFileTypeCommand,
            BinaryFileTypeResponse
        >(
            new CreateBinaryFileTypeCommand(
                "Commerce Digital Downloads",
                AllowAnonymous: true,
                AllowedExtensions: ".pdf"
            )
        );
        Assert.True(fileType.IsSuccess);

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Digital Guide PDF Data"));
        var file = await commands.SendAsync<UploadBinaryFileCommand, BinaryFileResponse>(
            new UploadBinaryFileCommand(
                fileType.Value.Id,
                "member-handbook-2026.pdf",
                "application/pdf",
                stream,
                IsTemporary: false
            )
        );
        Assert.True(file.IsSuccess);
        return file.Value.Id;
    }
}
