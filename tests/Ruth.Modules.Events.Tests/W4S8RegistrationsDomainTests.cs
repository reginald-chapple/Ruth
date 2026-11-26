using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;
using Xunit;

namespace Ruth.Modules.Events.Tests;

public sealed class W4S8RegistrationsDomainTests
{
    [Fact]
    public void RegistrationCapacityAndPricingEngine_EvaluatesSeatSplit_ConditionalFields_FeeCaps_AndDiscounts()
    {
        var accountId = Guid.CreateVersion7();
        var groupTypeId = Guid.CreateVersion7();
        var tmplRes = RegistrationTemplate.Create(
            new RegistrationTemplateSpec(
                "Youth Leadership Camp",
                accountId,
                BaseCostPerRegistrant: 100m,
                MinimumInitialDepositPerRegistrant: 30m,
                PaymentModel: RegistrationPaymentModel.DepositAndInstallments,
                WaitlistEnabled: true,
                Forms:
                [
                    new RegistrationFormSpec(
                        "Medical & Dietary",
                        Fields:
                        [
                            new RegistrationFormFieldSpec(
                                "has_allergy",
                                "Has Food Allergy",
                                RegistrationFieldKind.YesNo,
                                IsRequired: true
                            ),
                            new RegistrationFormFieldSpec(
                                "allergy_detail",
                                "Allergy Details",
                                RegistrationFieldKind.Text,
                                IsRequired: true,
                                ShowWhenFieldKey: "has_allergy",
                                ShowWhenEqualsValue: "true"
                            ),
                            new RegistrationFormFieldSpec(
                                "shirt_size",
                                "Shirt Size",
                                RegistrationFieldKind.SingleSelect,
                                IsRequired: true,
                                SelectOptions: ["S", "M", "L"]
                            ),
                            new RegistrationFormFieldSpec(
                                "tracks",
                                "Tracks",
                                RegistrationFieldKind.MultiSelect,
                                SelectOptions: ["Art", "STEM", "Outdoor"]
                            ),
                            new RegistrationFormFieldSpec(
                                "arrival_date",
                                "Arrival Date",
                                RegistrationFieldKind.Date
                            ),
                            new RegistrationFormFieldSpec(
                                "guest_age",
                                "Age",
                                RegistrationFieldKind.WholeNumber
                            ),
                            new RegistrationFormFieldSpec(
                                "emergency_email",
                                "Emergency Email",
                                RegistrationFieldKind.Email
                            ),
                            new RegistrationFormFieldSpec(
                                "emergency_phone",
                                "Emergency Phone",
                                RegistrationFieldKind.Phone
                            ),
                        ]
                    ),
                ],
                Fees: [RegistrationTemplateFeeSpecHelper.BuildOptionalWorkshopFee()],
                Discounts:
                [
                    new RegistrationDiscountSpec(
                        "EARLY10",
                        RegistrationDiscountKind.Percentage,
                        10m,
                        StartDate: new DateOnly(2026, 1, 1),
                        EndDate: new DateOnly(2026, 12, 31),
                        IsAutoApplyEarlyBird: true
                    ),
                    new RegistrationDiscountSpec(
                        "FLAT25",
                        RegistrationDiscountKind.FixedAmountPerRegistrant,
                        25m,
                        MaxRegistrants: 2
                    ),
                ],
                GroupPlacements:
                [
                    new RegistrationGroupPlacementSpec(
                        "Cabin Assignment",
                        groupTypeId,
                        AutoPlaceOnConfirmation: true
                    ),
                ]
            )
        );
        Assert.True(tmplRes.IsSuccess);
        var template = tmplRes.Value;
        var fee = Assert.Single(template.Fees);
        var feeItem = Assert.Single(fee.Items);

        var instRes = RegistrationInstance.Create(
            new RegistrationInstanceSpec(
                template.Id.Value,
                "Summer Camp 2026",
                "summer-camp-2026",
                MaxAttendees: 2
            )
        );
        Assert.True(instRes.IsSuccess);
        var instance = instRes.Value;

        var missingConditionalDetail = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            template,
            instance,
            [
                new RegistrantSubmissionDto(
                    "Sam",
                    "Lee",
                    FieldValues: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["has_allergy"] = "true",
                        ["shirt_size"] = "M",
                    }
                ),
            ],
            existingConfirmedCount: 0,
            activeOtherHeldConfirmedSeats: 0,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int>(),
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: null,
            new DateOnly(2026, 6, 1)
        );
        Assert.True(missingConditionalDetail.IsFailure);

        var validPlan = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            template,
            instance,
            [
                new RegistrantSubmissionDto(
                    "Sam",
                    "Lee",
                    "sam@example.org",
                    FieldValues: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["has_allergy"] = "false",
                        ["shirt_size"] = "M",
                        ["tracks"] = "Art,STEM",
                        ["arrival_date"] = "2026-07-10",
                        ["guest_age"] = "15",
                        ["emergency_email"] = "parent@example.org",
                        ["emergency_phone"] = "555-0199",
                    },
                    FeeSelections:
                    [
                        new RegistrantFeeSelectionInputDto(fee.Id.Value, feeItem.Id.Value, 1),
                    ]
                ),
                new RegistrantSubmissionDto(
                    "Riley",
                    "Lee",
                    FieldValues: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["has_allergy"] = "true",
                        ["allergy_detail"] = "Peanuts",
                        ["shirt_size"] = "L",
                    }
                ),
            ],
            existingConfirmedCount: 0,
            activeOtherHeldConfirmedSeats: 1,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int>(),
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: null,
            new DateOnly(2026, 6, 1)
        );
        Assert.True(validPlan.IsSuccess);
        Assert.Equal(1, validPlan.Value.ConfirmedCount);
        Assert.Equal(1, validPlan.Value.WaitlistedCount);
        Assert.Equal("EARLY10", validPlan.Value.AppliedDiscountCode);
        Assert.Equal(108.00m, validPlan.Value.TotalAmount);
        Assert.Equal(30.00m, validPlan.Value.MinimumInitialAmountDue);

        var flatCodePlan = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            template,
            instance,
            [
                new RegistrantSubmissionDto(
                    "Alex",
                    "Kim",
                    FieldValues: new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["has_allergy"] = "false",
                        ["shirt_size"] = "S",
                    }
                ),
            ],
            existingConfirmedCount: 0,
            activeOtherHeldConfirmedSeats: 0,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int>(),
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: "FLAT25",
            new DateOnly(2026, 6, 1)
        );
        Assert.True(flatCodePlan.IsSuccess);
        Assert.Equal(75.00m, flatCodePlan.Value.TotalAmount);
    }

    [Fact]
    public void Registration_SupportsSessionHolds_Payments_Cancellation_WaitlistPromotion_AndRepoint()
    {
        var now = new DateTimeOffset(2026, 6, 1, 10, 0, 0, TimeSpan.Zero);
        var instanceId = RegistrationInstanceId.Create();
        var holdRes = RegistrationSession.CreateHold(instanceId, 2, 1, 1, 15, now);
        Assert.True(holdRes.IsSuccess);
        Assert.True(holdRes.Value.IsActiveAt(now.AddMinutes(5)));
        Assert.True(holdRes.Value.MarkExpiredIfPastDue(now.AddMinutes(20)));

        var oldPerson = Guid.CreateVersion7();
        var newPerson = Guid.CreateVersion7();
        var plan = new EvaluatedRegistrationPlan(
            ConfirmedCount: 1,
            WaitlistedCount: 1,
            BaseSubtotal: 100m,
            FeesSubtotal: 0m,
            DiscountTotal: 0m,
            AppliedDiscountCode: null,
            TotalAmount: 100m,
            MinimumInitialAmountDue: 40m,
            [
                new EvaluatedRegistrantPlan(
                    oldPerson,
                    "Taylor",
                    "Morgan",
                    "taylor@example.org",
                    Phone: null,
                    RegistrantStatus.Confirmed,
                    WaitlistPosition: null,
                    BaseCost: 100m,
                    FeesTotal: 0m,
                    DiscountAmount: 0m,
                    NetCost: 100m,
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
                    []
                ),
                new EvaluatedRegistrantPlan(
                    PersonId: null,
                    "Casey",
                    "Morgan",
                    Email: null,
                    Phone: null,
                    RegistrantStatus.Waitlisted,
                    WaitlistPosition: 1,
                    BaseCost: 0m,
                    FeesTotal: 0m,
                    DiscountAmount: 0m,
                    NetCost: 0m,
                    new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase),
                    []
                ),
            ]
        );

        var regRes = Registration.Create(
            instanceId,
            oldPerson,
            "Taylor",
            "Morgan",
            "taylor@example.org",
            "USD",
            plan,
            now
        );
        Assert.True(regRes.IsSuccess);
        var reg = regRes.Value;

        Assert.True(reg.RecordPayment(Guid.CreateVersion7(), 40m, now, "Deposit").IsSuccess);
        Assert.Equal(60m, reg.BalanceDue);

        var registrants = reg.Registrants.ToArray();
        var cancelRes = reg.CancelRegistrant(registrants[0].Id);
        Assert.True(cancelRes.IsSuccess);
        Assert.True(cancelRes.Value);

        Assert.True(reg.PromoteRegistrantFromWaitlist(registrants[1].Id, 100m));
        Assert.Equal(RegistrantStatus.Confirmed, registrants[1].Status);
        Assert.Equal(60m, reg.BalanceDue);

        Assert.True(reg.RepointPerson(oldPerson, newPerson));
        Assert.Equal(newPerson, reg.RegistrarPersonId);
        Assert.Equal(newPerson, registrants[0].PersonId);
    }

    [Fact]
    public void RegistrationCapacityAndPricingEngine_RejectsWhenCapacityFullWithoutWaitlist_OrFeeCapExceeded()
    {
        var tmpl = RegistrationTemplate
            .Create(
                new RegistrationTemplateSpec(
                    "Strict Workshop",
                    Guid.CreateVersion7(),
                    BaseCostPerRegistrant: 50m,
                    PaymentModel: RegistrationPaymentModel.PayInFull,
                    WaitlistEnabled: false,
                    Fees: [RegistrationTemplateFeeSpecHelper.BuildOptionalWorkshopFee()]
                )
            )
            .Value;
        var fee = Assert.Single(tmpl.Fees);
        var feeItem = Assert.Single(fee.Items);

        var inst = RegistrationInstance
            .Create(
                new RegistrationInstanceSpec(
                    tmpl.Id.Value,
                    "Workshop A",
                    "workshop-a",
                    MaxAttendees: 1
                )
            )
            .Value;

        var full = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            tmpl,
            inst,
            [new RegistrantSubmissionDto("A", "B")],
            existingConfirmedCount: 1,
            activeOtherHeldConfirmedSeats: 0,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int>(),
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: null,
            new DateOnly(2026, 6, 1)
        );
        Assert.True(full.IsFailure);

        var feeCapExceeded = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            tmpl,
            inst,
            [
                new RegistrantSubmissionDto(
                    "A",
                    "B",
                    FeeSelections:
                    [
                        new RegistrantFeeSelectionInputDto(fee.Id.Value, feeItem.Id.Value, 1),
                    ]
                ),
            ],
            existingConfirmedCount: 0,
            activeOtherHeldConfirmedSeats: 0,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int> { [feeItem.Id.Value] = 2 },
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: null,
            new DateOnly(2026, 6, 1)
        );
        Assert.True(feeCapExceeded.IsFailure);

        var noDiscountOk = RegistrationCapacityAndPricingEngine.EvaluateSubmission(
            tmpl,
            inst,
            [new RegistrantSubmissionDto("A", "B")],
            existingConfirmedCount: 0,
            activeOtherHeldConfirmedSeats: 0,
            currentMaxWaitlistPosition: 0,
            new Dictionary<Guid, int>(),
            existingDiscountedRegistrantCount: 0,
            suppliedDiscountCode: null,
            new DateOnly(2026, 6, 1)
        );
        Assert.True(noDiscountOk.IsSuccess);
        Assert.Equal(50m, noDiscountOk.Value.MinimumInitialAmountDue);
    }
}
