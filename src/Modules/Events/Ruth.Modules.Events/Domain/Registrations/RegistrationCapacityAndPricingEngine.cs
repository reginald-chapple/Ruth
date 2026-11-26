using System.Globalization;
using Ruth.Modules.Events.Contracts.Registrations;
using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Events.Domain.Registrations;

/// <summary>Clean-room domain engine evaluating seat capacity, TTL holds, dynamic form fields, fee caps, discounts, and deposit rules.</summary>
public static class RegistrationCapacityAndPricingEngine
{
    public static Result<(int ConfirmedSeats, int WaitlistSeats)> AllocateSeatSplit(
        RegistrationTemplate template,
        RegistrationInstance instance,
        int requestedSeats,
        int existingConfirmedCount,
        int activeOtherHeldConfirmedSeats
    )
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(instance);

        if (requestedSeats <= 0 || requestedSeats > template.MaxRegistrantsPerSubmission)
        {
            return EventsRegistrationErrors.SessionHoldInvalid;
        }

        if (!instance.MaxAttendees.HasValue)
        {
            return (requestedSeats, 0);
        }

        var available = Math.Max(
            0,
            instance.MaxAttendees.Value - existingConfirmedCount - activeOtherHeldConfirmedSeats
        );
        var confirmed = Math.Min(requestedSeats, available);
        var waitlisted = requestedSeats - confirmed;

        return waitlisted > 0 && !template.WaitlistEnabled
            ? EventsRegistrationErrors.CapacityFull
            : (confirmed, waitlisted);
    }

    public static Result<EvaluatedRegistrationPlan> EvaluateSubmission(
        RegistrationTemplate template,
        RegistrationInstance instance,
        IReadOnlyList<RegistrantSubmissionDto> submissions,
        int existingConfirmedCount,
        int activeOtherHeldConfirmedSeats,
        int currentMaxWaitlistPosition,
        IReadOnlyDictionary<Guid, int> existingFeeItemQuantities,
        int existingDiscountedRegistrantCount,
        string? suppliedDiscountCode,
        DateOnly today
    )
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(submissions);
        ArgumentNullException.ThrowIfNull(existingFeeItemQuantities);

        var splitRes = AllocateSeatSplit(
            template,
            instance,
            submissions.Count,
            existingConfirmedCount,
            activeOtherHeldConfirmedSeats
        );
        if (splitRes.IsFailure)
        {
            return Result.Failure<EvaluatedRegistrationPlan>(splitRes.Errors);
        }

        var (confirmedSeats, _) = splitRes.Value;
        var discountRes = ResolveDiscount(
            template,
            suppliedDiscountCode,
            today,
            confirmedSeats,
            existingDiscountedRegistrantCount
        );
        if (discountRes.IsFailure)
        {
            return Result.Failure<EvaluatedRegistrationPlan>(discountRes.Errors);
        }

        var runningFeeCounts = new Dictionary<Guid, int>(existingFeeItemQuantities);
        var plans = new List<EvaluatedRegistrantPlan>(submissions.Count);
        var nextWaitlistPos = currentMaxWaitlistPosition + 1;

        for (var i = 0; i < submissions.Count; i++)
        {
            var isConfirmed = i < confirmedSeats;
            var regRes = EvaluateSingleRegistrant(
                template,
                instance,
                submissions[i],
                isConfirmed,
                isConfirmed ? null : nextWaitlistPos++,
                discountRes.Value,
                runningFeeCounts
            );
            if (regRes.IsFailure)
            {
                return Result.Failure<EvaluatedRegistrationPlan>(regRes.Errors);
            }

            plans.Add(regRes.Value);
        }

        return BuildPlanSummary(template, instance, plans, discountRes.Value?.Code);
    }

    private static Result<EvaluatedRegistrantPlan> EvaluateSingleRegistrant(
        RegistrationTemplate template,
        RegistrationInstance instance,
        RegistrantSubmissionDto dto,
        bool isConfirmed,
        int? waitlistPosition,
        RegistrationTemplateDiscount? discount,
        Dictionary<Guid, int> runningFeeCounts
    )
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
        {
            return EventsRegistrationErrors.FormValidationFailed;
        }

        var fieldsRes = ValidateAndNormalizeFields(template, dto.FieldValues);
        if (fieldsRes.IsFailure)
        {
            return Result.Failure<EvaluatedRegistrantPlan>(fieldsRes.Errors);
        }

        var feesRes = ValidateAndPriceFees(
            template,
            dto.FeeSelections,
            isConfirmed,
            runningFeeCounts
        );
        if (feesRes.IsFailure)
        {
            return Result.Failure<EvaluatedRegistrantPlan>(feesRes.Errors);
        }

        var (feeLines, feesTotal, discountableFees) = feesRes.Value;
        return BuildRegistrantPlan(
            template,
            instance,
            dto,
            isConfirmed,
            waitlistPosition,
            discount,
            fieldsRes.Value,
            feeLines,
            feesTotal,
            discountableFees
        );
    }

    private static EvaluatedRegistrantPlan BuildRegistrantPlan(
        RegistrationTemplate template,
        RegistrationInstance instance,
        RegistrantSubmissionDto dto,
        bool isConfirmed,
        int? waitlistPosition,
        RegistrationTemplateDiscount? discount,
        Dictionary<string, string?> fieldValues,
        IReadOnlyList<RegistrantFeeSelectionResponse> feeLines,
        decimal feesTotal,
        decimal discountableFees
    )
    {
        var baseCost = isConfirmed ? instance.EffectiveBaseCost(template) : 0m;
        var effectiveFees = isConfirmed ? feesTotal : 0m;
        var discountAmt =
            isConfirmed && discount is not null
                ? CalculateDiscountAmount(discount, baseCost + discountableFees)
                : 0m;
        var netCost = Math.Max(0m, baseCost + effectiveFees - discountAmt);

        return new EvaluatedRegistrantPlan(
            dto.PersonId,
            dto.FirstName.Trim(),
            dto.LastName.Trim(),
            TrimNullable(dto.Email),
            TrimNullable(dto.Phone),
            isConfirmed ? RegistrantStatus.Confirmed : RegistrantStatus.Waitlisted,
            waitlistPosition,
            baseCost,
            effectiveFees,
            discountAmt,
            netCost,
            fieldValues,
            feeLines
        );
    }

    private static string? TrimNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Result<Dictionary<string, string?>> ValidateAndNormalizeFields(
        RegistrationTemplate template,
        IReadOnlyDictionary<string, string?>? rawValues
    )
    {
        var normalized = NormalizeRawFieldMap(rawValues);
        foreach (var field in template.Forms.SelectMany(static f => f.Fields))
        {
            if (!IsFieldVisible(field, normalized))
            {
                continue;
            }

            _ = normalized.TryGetValue(field.FieldKey, out var val);
            if (!IsFieldValueValid(field, val))
            {
                return EventsRegistrationErrors.FormValidationFailed;
            }
        }

        return normalized;
    }

    private static Dictionary<string, string?> NormalizeRawFieldMap(
        IReadOnlyDictionary<string, string?>? rawValues
    )
    {
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (rawValues is null)
        {
            return normalized;
        }

        foreach (var (k, v) in rawValues)
        {
            if (!string.IsNullOrWhiteSpace(k))
            {
                normalized[k.Trim()] = TrimNullable(v);
            }
        }

        return normalized;
    }

    private static bool IsFieldVisible(
        RegistrationTemplateFormField field,
        Dictionary<string, string?> values
    )
    {
        if (string.IsNullOrWhiteSpace(field.ShowWhenFieldKey))
        {
            return true;
        }

        _ = values.TryGetValue(field.ShowWhenFieldKey, out var triggerVal);
        return string.Equals(
            triggerVal,
            field.ShowWhenEqualsValue,
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static bool IsFieldValueValid(RegistrationTemplateFormField field, string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? !field.IsRequired
            : field.FieldKind switch
            {
                RegistrationFieldKind.Text => value.Length <= 1000,
                RegistrationFieldKind.Email => value.Contains('@', StringComparison.Ordinal),
                RegistrationFieldKind.Phone => value.Length is >= 7 and <= 32,
                RegistrationFieldKind.SingleSelect => field.SelectOptions.Any(o =>
                    string.Equals(o, value, StringComparison.OrdinalIgnoreCase)
                ),
                RegistrationFieldKind.MultiSelect => value
                    .Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .All(part =>
                        field.SelectOptions.Any(o =>
                            string.Equals(o, part, StringComparison.OrdinalIgnoreCase)
                        )
                    ),
                RegistrationFieldKind.YesNo => bool.TryParse(value, out _),
                RegistrationFieldKind.Date => DateOnly.TryParse(
                    value,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _
                ),
                RegistrationFieldKind.WholeNumber => int.TryParse(
                    value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _
                ),
                _ => false,
            };
    }

    private static Result<(
        IReadOnlyList<RegistrantFeeSelectionResponse> Lines,
        decimal Total,
        decimal DiscountableTotal
    )> ValidateAndPriceFees(
        RegistrationTemplate template,
        IReadOnlyList<RegistrantFeeSelectionInputDto>? selections,
        bool reserveInventory,
        Dictionary<Guid, int> runningFeeCounts
    )
    {
        var input = selections ?? [];
        var lines = new List<RegistrantFeeSelectionResponse>();
        var total = 0m;
        var discountable = 0m;

        foreach (var fee in template.Fees)
        {
            var matches = input.Where(s => s.FeeId == fee.Id.Value && s.Quantity > 0).ToArray();
            if (!IsFeeGroupSelectionValid(fee, matches))
            {
                return EventsRegistrationErrors.FeeSelectionInvalid;
            }

            if (
                !TryAccumulateFeeGroupLines(
                    fee,
                    matches,
                    reserveInventory,
                    runningFeeCounts,
                    lines,
                    ref total,
                    ref discountable
                )
            )
            {
                return EventsRegistrationErrors.FeeSelectionInvalid;
            }
        }

        return (lines, total, discountable);
    }

    private static bool IsFeeGroupSelectionValid(
        RegistrationTemplateFee fee,
        RegistrantFeeSelectionInputDto[] matches
    )
    {
        return (!fee.IsRequired || matches.Length > 0)
            && (
                fee.FeeType != RegistrationFeeType.SingleOption
                || (matches.Length <= 1 && !matches.Any(static m => m.Quantity > 1))
            );
    }

    private static bool TryAccumulateFeeGroupLines(
        RegistrationTemplateFee fee,
        RegistrantFeeSelectionInputDto[] matches,
        bool reserveInventory,
        Dictionary<Guid, int> runningFeeCounts,
        List<RegistrantFeeSelectionResponse> lines,
        ref decimal total,
        ref decimal discountable
    )
    {
        foreach (var sel in matches)
        {
            var item = fee.Items.FirstOrDefault(it => it.Id.Value == sel.FeeItemId);
            if (
                item is null
                || !TryReserveFeeInventory(item, sel.Quantity, reserveInventory, runningFeeCounts)
            )
            {
                return false;
            }

            var lineTotal = decimal.Round(
                item.UnitAmount * sel.Quantity,
                2,
                MidpointRounding.AwayFromZero
            );
            lines.Add(
                new RegistrantFeeSelectionResponse(
                    fee.Id.Value,
                    item.Id.Value,
                    item.Label,
                    sel.Quantity,
                    item.UnitAmount,
                    lineTotal
                )
            );
            total += lineTotal;
            discountable += fee.AllowDiscount ? lineTotal : 0m;
        }

        return true;
    }

    private static bool TryReserveFeeInventory(
        RegistrationTemplateFeeItem item,
        int quantity,
        bool reserveInventory,
        Dictionary<Guid, int> runningFeeCounts
    )
    {
        if (!reserveInventory || !item.MaxQuantityAvailable.HasValue)
        {
            return true;
        }

        _ = runningFeeCounts.TryGetValue(item.Id.Value, out var used);
        if (used + quantity > item.MaxQuantityAvailable.Value)
        {
            return false;
        }

        runningFeeCounts[item.Id.Value] = used + quantity;
        return true;
    }

    private static Result<RegistrationTemplateDiscount?> ResolveDiscount(
        RegistrationTemplate template,
        string? suppliedCode,
        DateOnly today,
        int confirmedSeats,
        int existingDiscountedRegistrantCount
    )
    {
        if (!string.IsNullOrWhiteSpace(suppliedCode))
        {
            var code = suppliedCode.Trim().ToUpperInvariant();
            var match = template.Discounts.FirstOrDefault(d =>
                string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase)
            );
            return
                match is not null
                && match.IsEligibleOn(today, confirmedSeats, existingDiscountedRegistrantCount)
                ? match
                : EventsRegistrationErrors.DiscountInvalid;
        }

        var earlyBird = template.Discounts.FirstOrDefault(d =>
            d.IsAutoApplyEarlyBird
            && d.IsEligibleOn(today, confirmedSeats, existingDiscountedRegistrantCount)
        );
        return Result.Success(earlyBird);
    }

    private static decimal CalculateDiscountAmount(
        RegistrationTemplateDiscount discount,
        decimal eligibleAmount
    )
    {
        if (eligibleAmount <= 0m)
        {
            return 0m;
        }

        var raw = discount.DiscountKind switch
        {
            RegistrationDiscountKind.Percentage => eligibleAmount * discount.DiscountValue / 100m,
            RegistrationDiscountKind.FixedAmountPerRegistrant => discount.DiscountValue,
            _ => 0m,
        };
        return Math.Min(eligibleAmount, decimal.Round(raw, 2, MidpointRounding.AwayFromZero));
    }

    private static EvaluatedRegistrationPlan BuildPlanSummary(
        RegistrationTemplate template,
        RegistrationInstance instance,
        List<EvaluatedRegistrantPlan> plans,
        string? appliedDiscountCode
    )
    {
        var confirmed = plans.Where(static p => p.Status == RegistrantStatus.Confirmed).ToArray();
        var waitlistedCount = plans.Count - confirmed.Length;
        var baseSub = decimal.Round(
            confirmed.Sum(static p => p.BaseCost),
            2,
            MidpointRounding.AwayFromZero
        );
        var feesSub = decimal.Round(
            confirmed.Sum(static p => p.FeesTotal),
            2,
            MidpointRounding.AwayFromZero
        );
        var discTot = decimal.Round(
            confirmed.Sum(static p => p.DiscountAmount),
            2,
            MidpointRounding.AwayFromZero
        );
        var total = decimal.Round(
            confirmed.Sum(static p => p.NetCost),
            2,
            MidpointRounding.AwayFromZero
        );

        var minDue = template.PaymentModel switch
        {
            RegistrationPaymentModel.Free => 0m,
            RegistrationPaymentModel.InvoiceBilling => 0m,
            RegistrationPaymentModel.PayInFull => total,
            RegistrationPaymentModel.DepositAndInstallments => Math.Min(
                total,
                decimal.Round(
                    instance.EffectiveMinimumDeposit(template) * confirmed.Length,
                    2,
                    MidpointRounding.AwayFromZero
                )
            ),
            _ => total,
        };

        return new EvaluatedRegistrationPlan(
            confirmed.Length,
            waitlistedCount,
            baseSub,
            feesSub,
            discTot,
            confirmed.Length > 0 ? appliedDiscountCode : null,
            total,
            minDue,
            plans
        );
    }
}
