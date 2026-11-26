using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.CheckIn.Contracts.Configurations;
using Ruth.Modules.CheckIn.Contracts.Labels;
using Ruth.Modules.CheckIn.Contracts.Sessions;
using Ruth.Modules.CheckIn.Domain.Labels;
using Ruth.Modules.CheckIn.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.CheckIn.Tests.Infrastructure;

[Collection(CheckInPostgresFixtures.Name)]
public sealed class W3S4LabelDesignerAndTcpPrintIntegrationTests(
    CheckInPostgresContainerFixture postgres
)
{
    [Fact]
    public async Task SeededSystemLabels_CustomLabelCrud_ZplPreview_AndRawTcpSocketPrinterDispatch_WorkEndToEnd()
    {
        var connectionString = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await CheckInTestHostFactory.BuildAndMigrateAsync(
            connectionString
        );
        await using var scope = provider.CreateAsyncScope();
        var commands = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queries = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();

        // 1. Verify bootstrapper seeded both system labels and blocks deleting system labels.
        var seeded = await queries.QueryAsync<
            GetCheckInLabelsQuery,
            IReadOnlyList<CheckInLabelResponse>
        >(new GetCheckInLabelsQuery(ActiveOnly: true));
        Assert.True(seeded.IsSuccess);
        Assert.True(seeded.Value.Count >= 2);
        var systemBadge = seeded.Value.First(static l => l.IsSystem);
        var deleteSystem = await commands.SendAsync<DeleteCheckInLabelCommand, bool>(
            new DeleteCheckInLabelCommand(systemBadge.Id)
        );
        Assert.True(deleteSystem.IsFailure);

        // 2. Create, update, preview, and delete a custom label.
        await VerifyCustomLabelCrudAndPreviewAsync(commands, queries);

        // 3. Submit a Check-In session and dispatch compiled ZPL over a real loopback TCP raw socket!
        await VerifySessionRawTcpSocketPrintingAsync(commands);
    }

    private static async Task VerifyCustomLabelCrudAndPreviewAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries
    )
    {
        var created = await commands.SendAsync<CreateCheckInLabelCommand, CheckInLabelResponse>(
            new CreateCheckInLabelCommand(
                "Makerspace Workshop Badge",
                "makerspace-workshop-badge",
                CheckInLabelKind.VisitorNameBadge,
                CheckInLabelLayoutSchema.CreateDefaultParticipantBadgeDto(),
                Description: "300 DPI custom badge"
            )
        );
        Assert.True(created.IsSuccess);

        var fetched = await queries.QueryAsync<GetCheckInLabelByIdQuery, CheckInLabelResponse>(
            new GetCheckInLabelByIdQuery(created.Value.Id)
        );
        Assert.True(fetched.IsSuccess);
        Assert.Equal("makerspace-workshop-badge", fetched.Value.Key);

        var updated = await commands.SendAsync<UpdateCheckInLabelCommand, CheckInLabelResponse>(
            new UpdateCheckInLabelCommand(
                created.Value.Id,
                "Makerspace Workshop Badge v2",
                "makerspace-workshop-badge",
                CheckInLabelKind.VisitorNameBadge,
                created.Value.Layout,
                Copies: 2
            )
        );
        Assert.True(updated.IsSuccess);
        Assert.Equal(2, updated.Value.Copies);

        var preview = await commands.SendAsync<
            PreviewCheckInLabelCommand,
            CheckInLabelPreviewResponse
        >(
            new PreviewCheckInLabelCommand(
                LabelId: created.Value.Id,
                Context: new CheckInLabelMergeContextDto(
                    ParticipantNickName: "Alex",
                    ParticipantLastName: "Kim",
                    SecurityPickupCode: "MK92",
                    MedicalAllergyAlert: "Latex allergy"
                )
            )
        );
        Assert.True(preview.IsSuccess);
        Assert.Contains("^PQ2,0,1,Y", preview.Value.CompiledZpl, StringComparison.Ordinal);
        Assert.Contains("Latex allergy", preview.Value.CompiledZpl, StringComparison.Ordinal);

        var deleted = await commands.SendAsync<DeleteCheckInLabelCommand, bool>(
            new DeleteCheckInLabelCommand(created.Value.Id)
        );
        Assert.True(deleted.IsSuccess);
    }

    private static async Task VerifySessionRawTcpSocketPrintingAsync(ICommandDispatcher commands)
    {
        var scheduleId = Guid.CreateVersion7();
        var roomId = Guid.CreateVersion7();
        var (configId, areaId) = await W3S3CheckInProfileSeedHelper.ConfigureCheckInProfileAsync(
            commands,
            linkedGroupId: Guid.Empty,
            scheduleId,
            roomId,
            Guid.CreateVersion7()
        );

        var session = await commands.SendAsync<
            SubmitCheckInSessionCommand,
            CheckInSessionReceiptResponse
        >(
            new SubmitCheckInSessionCommand(
                configId,
                scheduleId,
                [
                    new CheckInSelectionItemDto(
                        Guid.CreateVersion7(),
                        areaId,
                        roomId,
                        scheduleId,
                        PersonFullName: "Zoe Carter",
                        ParticipantContext: new CheckInParticipantContextDto(
                            AgeYears: 9m,
                            MembershipTier: "Family"
                        ),
                        AttendanceNote: "Peanut allergy"
                    ),
                ]
            )
        );
        Assert.True(session.IsSuccess);

        using var captureServer = new LoopbackZplTcpCaptureServer(expectedConnections: 2);
        var printed = await commands.SendAsync<
            PrintCheckInSessionLabelsCommand,
            PrintCheckInSessionLabelsResponse
        >(
            new PrintCheckInSessionLabelsCommand(
                session.Value.SessionId,
                DirectPrinterEndpointOverride: captureServer.Endpoint
            )
        );
        Assert.True(printed.IsSuccess);
        Assert.Equal(2, printed.Value.ServerDispatchedCount);
        Assert.Equal(0, printed.Value.ClientPendingCount);

        var receivedPayloads = await captureServer.WaitForPayloadsAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(2, receivedPayloads.Count);
        Assert.Contains(receivedPayloads, p =>
            p.Contains("Zoe", StringComparison.Ordinal)
            && p.Contains("^XA", StringComparison.Ordinal)
        );
        Assert.Contains(receivedPayloads, p =>
            p.Contains("GUARDIAN PICKUP RECEIPT", StringComparison.Ordinal)
            && p.Contains(session.Value.SecurityPickupCode!, StringComparison.Ordinal)
        );
    }
}
