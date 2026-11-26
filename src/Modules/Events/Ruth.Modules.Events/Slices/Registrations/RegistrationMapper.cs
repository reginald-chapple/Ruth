using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.Modules.Events.Domain.Registrations;

namespace Ruth.Modules.Events.Slices.Registrations;

internal static class RegistrationMapper
{
    public static RegistrationTemplateResponse MapTemplate(RegistrationTemplate template)
    {
        return new RegistrationTemplateResponse(
            template.Id.Value,
            template.Name,
            template.FinancialAccountId,
            template.CategoryId,
            template.DefaultFinancialGatewayId,
            template.CurrencyCode,
            template.BaseCostPerRegistrant,
            template.MinimumInitialDepositPerRegistrant,
            template.PaymentModel,
            template.WaitlistEnabled,
            template.SeatHoldTtlMinutes,
            template.MaxRegistrantsPerSubmission,
            template.IsActive,
            MapForms(template),
            MapFees(template),
            MapDiscounts(template),
            MapPlacements(template),
            template.CreatedOnUtc,
            template.ModifiedOnUtc
        );
    }

    private static RegistrationFormResponse[] MapForms(RegistrationTemplate template)
    {
        return
        [
            .. template
                .Forms.OrderBy(static f => f.DisplayOrder)
                .Select(static f => new RegistrationFormResponse(
                    f.Id.Value,
                    f.Name,
                    f.DisplayOrder,
                    [
                        .. f
                            .Fields.OrderBy(static fld => fld.DisplayOrder)
                            .Select(static fld => new RegistrationFormFieldResponse(
                                fld.Id.Value,
                                fld.FieldKey,
                                fld.Label,
                                fld.FieldKind,
                                fld.IsRequired,
                                fld.SelectOptions,
                                fld.ShowWhenFieldKey,
                                fld.ShowWhenEqualsValue,
                                fld.DisplayOrder
                            )),
                    ]
                )),
        ];
    }

    private static RegistrationFeeResponse[] MapFees(RegistrationTemplate template)
    {
        return
        [
            .. template
                .Fees.OrderBy(static fee => fee.DisplayOrder)
                .Select(static fee => new RegistrationFeeResponse(
                    fee.Id.Value,
                    fee.Name,
                    fee.FeeType,
                    fee.IsRequired,
                    fee.AllowDiscount,
                    fee.DisplayOrder,
                    [
                        .. fee
                            .Items.OrderBy(static it => it.DisplayOrder)
                            .Select(static it => new RegistrationFeeItemResponse(
                                it.Id.Value,
                                it.Code,
                                it.Label,
                                it.UnitAmount,
                                it.MaxQuantityAvailable,
                                it.DisplayOrder
                            )),
                    ]
                )),
        ];
    }

    private static RegistrationDiscountResponse[] MapDiscounts(RegistrationTemplate template)
    {
        return
        [
            .. template.Discounts.Select(static d => new RegistrationDiscountResponse(
                d.Id.Value,
                d.Code,
                d.DiscountKind,
                d.DiscountValue,
                d.StartDate,
                d.EndDate,
                d.MaxRegistrants,
                d.MinRegistrants,
                d.IsAutoApplyEarlyBird
            )),
        ];
    }

    private static RegistrationGroupPlacementResponse[] MapPlacements(RegistrationTemplate template)
    {
        return
        [
            .. template.GroupPlacements.Select(static p => new RegistrationGroupPlacementResponse(
                p.Id.Value,
                p.Name,
                p.GroupTypeId,
                p.DefaultTargetGroupId,
                p.DefaultGroupRoleId,
                p.AutoPlaceOnConfirmation
            )),
        ];
    }

    public static RegistrationInstanceResponse MapInstance(
        RegistrationInstance instance,
        RegistrationTemplate template,
        int confirmedCount,
        int activeHeldSeats,
        int waitlistedCount,
        DateTimeOffset nowUtc
    )
    {
        int? remaining = instance.MaxAttendees.HasValue
            ? Math.Max(0, instance.MaxAttendees.Value - confirmedCount - activeHeldSeats)
            : null;

        return new RegistrationInstanceResponse(
            instance.Id.Value,
            instance.TemplateId.Value,
            template.Name,
            instance.Name,
            instance.Slug,
            template.CurrencyCode,
            template.PaymentModel,
            instance.EffectiveBaseCost(template),
            instance.EffectiveMinimumDeposit(template),
            template.WaitlistEnabled,
            template.SeatHoldTtlMinutes,
            instance.MaxAttendees,
            confirmedCount,
            activeHeldSeats,
            waitlistedCount,
            remaining,
            instance.IsOpenAt(nowUtc),
            instance.RegistrationOpensUtc,
            instance.RegistrationClosesUtc,
            instance.PaymentDeadlineDate,
            instance.EventItemOccurrenceId,
            instance.LinkedTargetGroupId,
            instance.BranchId,
            instance.ContactEmail,
            instance.AdditionalInstructions,
            instance.IsActive,
            instance.CreatedOnUtc
        );
    }

    public static RegistrationSessionResponse MapSession(RegistrationSession session)
    {
        return new RegistrationSessionResponse(
            session.Id.Value,
            session.InstanceId.Value,
            session.SessionToken,
            session.RequestedSeatCount,
            session.HeldConfirmedSeats,
            session.HeldWaitlistSeats,
            session.Status,
            session.ExpiresAtUtc,
            session.RegistrationId?.Value,
            session.CreatedOnUtc
        );
    }

    public static RegistrationResponse MapRegistration(Registration registration)
    {
        return new RegistrationResponse(
            registration.Id.Value,
            registration.InstanceId.Value,
            registration.ConfirmationCode,
            registration.RegistrarPersonId,
            registration.RegistrarFirstName,
            registration.RegistrarLastName,
            registration.RegistrarEmail,
            registration.Status,
            registration.CurrencyCode,
            registration.AppliedDiscountCode,
            registration.SubtotalAmount,
            registration.DiscountTotalAmount,
            registration.TotalAmount,
            registration.PaidAmount,
            registration.BalanceDue,
            registration.LinkedInvoiceId,
            [.. registration.Registrants.OrderBy(static r => r.CreatedOnUtc).Select(MapRegistrant)],
            [
                .. registration
                    .Payments.OrderBy(static p => p.PaidAtUtc)
                    .Select(static p => new RegistrationPaymentRecordDto(
                        p.Id.Value,
                        p.FinancialTransactionId,
                        p.Amount,
                        p.PaidAtUtc,
                        p.Note
                    )),
            ],
            registration.CreatedOnUtc
        );
    }

    public static RegistrationRegistrantResponse MapRegistrant(RegistrationRegistrant registrant)
    {
        return new RegistrationRegistrantResponse(
            registrant.Id.Value,
            registrant.RegistrationId.Value,
            registrant.PersonId,
            registrant.FirstName,
            registrant.LastName,
            registrant.Email,
            registrant.Phone,
            registrant.Status,
            registrant.IsOnWaitlist,
            registrant.WaitlistPosition,
            registrant.BaseCost,
            registrant.FeesTotal,
            registrant.DiscountAmount,
            registrant.NetCost,
            registrant.PlacedGroupId,
            registrant.PlacedGroupMemberId,
            registrant.FieldValues,
            [
                .. registrant.FeeSelections.Select(static s => new RegistrantFeeSelectionResponse(
                    s.FeeId,
                    s.FeeItemId,
                    s.Label,
                    s.Quantity,
                    s.UnitAmount,
                    s.LineTotal
                )),
            ],
            registrant.CreatedOnUtc
        );
    }
}
