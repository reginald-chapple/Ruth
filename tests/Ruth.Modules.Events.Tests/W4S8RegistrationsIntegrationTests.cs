using Microsoft.Extensions.DependencyInjection;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Tests.Fixtures;
using Ruth.SharedKernel.Messaging;
using Xunit;

namespace Ruth.Modules.Events.Tests;

[Collection(EventsPostgresFixtures.Name)]
public sealed class W4S8RegistrationsIntegrationTests(EventsPostgresContainerFixture postgres)
{
    [Fact]
    public async Task Registrations_SeatHolds_Deposits_Invoices_WaitlistPromotion_GroupPlacement_AndCleanupJob_WorkEndToEnd()
    {
        var conn = await postgres.CreateIsolatedDatabaseAsync();
        await using var provider = await EventsTestHostFactory.BuildAndMigrateAsync(conn);
        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var commands = sp.GetRequiredService<ICommandDispatcher>();
        var queries = sp.GetRequiredService<IQueryDispatcher>();

        var (accountId, gatewayId, groupTypeId, groupId, person1Id, person2Id) =
            await W4S8RegistrationSeedHelper.SeedDependenciesAsync(commands, queries);

        var (_, instance) = await CreateTemplateAndInstanceAsync(
            commands,
            accountId,
            gatewayId,
            groupTypeId,
            groupId
        );

        var reg1 = await SubmitConfirmedDepositAndInstallmentAsync(
            commands,
            queries,
            instance.Slug,
            person1Id
        );

        var reg2 = await SubmitWaitlistedAndVerifyInvoiceModeAsync(
            commands,
            accountId,
            instance.Slug,
            person2Id
        );

        await W4S8RegistrationVerificationHelper.VerifyCancelWaitlistMergeAndJobAsync(
            sp,
            commands,
            queries,
            reg1,
            reg2,
            person1Id,
            person2Id
        );
    }

    private static async Task<(
        RegistrationTemplateResponse Template,
        RegistrationInstanceResponse Instance
    )> CreateTemplateAndInstanceAsync(
        ICommandDispatcher commands,
        Guid accountId,
        Guid gatewayId,
        Guid groupTypeId,
        Guid groupId
    )
    {
        var tmplRes = await commands.SendAsync<
            CreateRegistrationTemplateCommand,
            RegistrationTemplateResponse
        >(
            new CreateRegistrationTemplateCommand(
                new RegistrationTemplateSpec(
                    "Annual Leadership Retreat",
                    accountId,
                    DefaultFinancialGatewayId: gatewayId,
                    BaseCostPerRegistrant: 100m,
                    MinimumInitialDepositPerRegistrant: 40m,
                    PaymentModel: RegistrationPaymentModel.DepositAndInstallments,
                    WaitlistEnabled: true,
                    SeatHoldTtlMinutes: 15,
                    Forms:
                    [
                        new RegistrationFormSpec(
                            "Participant Form",
                            Fields:
                            [
                                new RegistrationFormFieldSpec(
                                    "tshirt",
                                    "T-Shirt Size",
                                    RegistrationFieldKind.SingleSelect,
                                    IsRequired: true,
                                    SelectOptions: ["S", "M", "L"]
                                ),
                            ]
                        ),
                    ],
                    Discounts:
                    [
                        new RegistrationDiscountSpec(
                            "SAVE20",
                            RegistrationDiscountKind.FixedAmountPerRegistrant,
                            20m
                        ),
                    ],
                    GroupPlacements:
                    [
                        new RegistrationGroupPlacementSpec(
                            "Retreat Cohort",
                            groupTypeId,
                            DefaultTargetGroupId: groupId,
                            AutoPlaceOnConfirmation: true
                        ),
                    ]
                )
            )
        );
        Assert.True(tmplRes.IsSuccess);

        var instRes = await commands.SendAsync<
            CreateRegistrationInstanceCommand,
            RegistrationInstanceResponse
        >(
            new CreateRegistrationInstanceCommand(
                new RegistrationInstanceSpec(
                    tmplRes.Value.Id,
                    "Leadership Retreat 2026",
                    "leadership-retreat-2026",
                    MaxAttendees: 1,
                    LinkedTargetGroupId: groupId
                )
            )
        );
        Assert.True(instRes.IsSuccess);
        return (tmplRes.Value, instRes.Value);
    }

    private static async Task<RegistrationResponse> SubmitConfirmedDepositAndInstallmentAsync(
        ICommandDispatcher commands,
        IQueryDispatcher queries,
        string slug,
        Guid person1Id
    )
    {
        var holdRes = await commands.SendAsync<
            CreateRegistrationSessionHoldCommand,
            RegistrationSessionResponse
        >(new CreateRegistrationSessionHoldCommand(slug, 1));
        Assert.True(holdRes.IsSuccess);
        Assert.Equal(1, holdRes.Value.HeldConfirmedSeats);

        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["tshirt"] = "M",
        };
        var quoteRes = await queries.QueryAsync<
            PreviewRegistrationPricingQuery,
            RegistrationPricingQuoteResponse
        >(
            new PreviewRegistrationPricingQuery(
                slug,
                [
                    new RegistrantSubmissionDto(
                        "Elena",
                        "Vance",
                        "elena@example.org",
                        PersonId: person1Id,
                        FieldValues: fields
                    ),
                ],
                DiscountCode: "SAVE20",
                SessionToken: holdRes.Value.SessionToken
            )
        );
        Assert.True(quoteRes.IsSuccess);
        Assert.Equal(80m, quoteRes.Value.TotalAmount);
        Assert.Equal(40m, quoteRes.Value.MinimumInitialAmountDue);

        var submitRes = await commands.SendAsync<SubmitRegistrationCommand, RegistrationResponse>(
            new SubmitRegistrationCommand(
                slug,
                "Elena",
                "Vance",
                "elena@example.org",
                [
                    new RegistrantSubmissionDto(
                        "Elena",
                        "Vance",
                        "elena@example.org",
                        PersonId: person1Id,
                        FieldValues: fields
                    ),
                ],
                RegistrarPersonId: person1Id,
                SessionToken: holdRes.Value.SessionToken,
                DiscountCode: "SAVE20",
                InitialPaymentAmount: 40m,
                OneTimeHostedPaymentToken: "tok_retreat_deposit_1"
            )
        );
        Assert.True(submitRes.IsSuccess);
        Assert.Equal(40m, submitRes.Value.BalanceDue);
        var confirmedAtt = Assert.Single(submitRes.Value.Registrants);
        Assert.True(confirmedAtt.PlacedGroupMemberId.HasValue);

        var payBalance = await commands.SendAsync<
            RecordRegistrationPaymentCommand,
            RegistrationResponse
        >(
            new RecordRegistrationPaymentCommand(
                submitRes.Value.Id,
                40m,
                OneTimeHostedPaymentToken: "tok_retreat_installment_2"
            )
        );
        Assert.True(payBalance.IsSuccess);
        Assert.Equal(0m, payBalance.Value.BalanceDue);
        return payBalance.Value;
    }

    private static async Task<RegistrationResponse> SubmitWaitlistedAndVerifyInvoiceModeAsync(
        ICommandDispatcher commands,
        Guid accountId,
        string retreatSlug,
        Guid person2Id
    )
    {
        var waitRes = await commands.SendAsync<SubmitRegistrationCommand, RegistrationResponse>(
            new SubmitRegistrationCommand(
                retreatSlug,
                "Marcus",
                "Cole",
                "marcus@example.org",
                [
                    new RegistrantSubmissionDto(
                        "Marcus",
                        "Cole",
                        "marcus@example.org",
                        PersonId: person2Id,
                        FieldValues: new Dictionary<string, string?>(
                            StringComparer.OrdinalIgnoreCase
                        )
                        {
                            ["tshirt"] = "L",
                        }
                    ),
                ],
                RegistrarPersonId: person2Id
            )
        );
        Assert.True(waitRes.IsSuccess);
        var waitAtt = Assert.Single(waitRes.Value.Registrants);
        Assert.Equal(RegistrantStatus.Waitlisted, waitAtt.Status);
        Assert.Equal(1, waitAtt.WaitlistPosition);

        var invTmpl = await commands.SendAsync<
            CreateRegistrationTemplateCommand,
            RegistrationTemplateResponse
        >(
            new CreateRegistrationTemplateCommand(
                new RegistrationTemplateSpec(
                    "Corporate Sponsor Table",
                    accountId,
                    BaseCostPerRegistrant: 250m,
                    PaymentModel: RegistrationPaymentModel.InvoiceBilling
                )
            )
        );
        Assert.True(invTmpl.IsSuccess);

        var invInst = await commands.SendAsync<
            CreateRegistrationInstanceCommand,
            RegistrationInstanceResponse
        >(
            new CreateRegistrationInstanceCommand(
                new RegistrationInstanceSpec(invTmpl.Value.Id, "Gala 2026", "gala-2026")
            )
        );
        Assert.True(invInst.IsSuccess);

        var invReg = await commands.SendAsync<SubmitRegistrationCommand, RegistrationResponse>(
            new SubmitRegistrationCommand(
                "gala-2026",
                "Marcus",
                "Cole",
                "marcus@example.org",
                [new RegistrantSubmissionDto("Marcus", "Cole", "marcus@example.org")],
                RegistrarPersonId: person2Id
            )
        );
        Assert.True(invReg.IsSuccess);
        Assert.True(invReg.Value.LinkedInvoiceId.HasValue);
        return waitRes.Value;
    }
}
