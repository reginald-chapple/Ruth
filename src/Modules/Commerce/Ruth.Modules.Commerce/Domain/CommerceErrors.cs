using Ruth.SharedKernel.Results;

namespace Ruth.Modules.Commerce.Domain;

/// <summary>Domain errors for the Commerce bounded context (W4-S9 Catalog/Orders/PunchPasses and W4-S10 Bookings/Rentals).</summary>
public static class CommerceErrors
{
    public static readonly DomainError PunchPassTypeInvalid = DomainError.Validation(
        "Commerce.PunchPassType.Invalid",
        "Punch pass type requires a non-empty name, normalized code, and positive credit units."
    );

    public static readonly DomainError PunchPassTypeCodeConflict = DomainError.Conflict(
        "Commerce.PunchPassType.CodeConflict",
        "Another punch pass type already uses this code."
    );

    public static readonly DomainError PunchPassTypeNotFound = DomainError.NotFound(
        "Commerce.PunchPassType.NotFound",
        "The requested punch pass type was not found."
    );

    public static readonly DomainError ProductInvalid = DomainError.Validation(
        "Commerce.Product.Invalid",
        "Product requires a valid name, slug, 3-letter ISO currency, valid revenue/quid-pro-quo accounts, and at least one valid SKU variant."
    );

    public static readonly DomainError ProductSlugOrSkuConflict = DomainError.Conflict(
        "Commerce.Product.SlugOrSkuConflict",
        "Another product or variant already uses this slug or SKU."
    );

    public static readonly DomainError ProductNotFound = DomainError.NotFound(
        "Commerce.Product.NotFound",
        "The requested commerce product or variant was not found."
    );

    public static readonly DomainError QuidProQuoAccountInvalid = DomainError.Validation(
        "Commerce.Product.QuidProQuoAccountInvalid",
        "Quid-pro-quo products require a distinct, active tax-deductible CharitableFund account and a non-deductible revenue account."
    );

    public static readonly DomainError InsufficientStock = DomainError.Conflict(
        "Commerce.Inventory.InsufficientStock",
        "Requested quantity exceeds available stock for one or more product variants."
    );

    public static readonly DomainError MemberPricingRuleInvalid = DomainError.Validation(
        "Commerce.MemberPricingRule.Invalid",
        "Member pricing rule requires a name, positive discount within bounds, and at least one tier or group eligibility criterion."
    );

    public static readonly DomainError MemberPricingRuleNotFound = DomainError.NotFound(
        "Commerce.MemberPricingRule.NotFound",
        "The requested member pricing rule was not found."
    );

    public static readonly DomainError OrderCartInvalid = DomainError.Validation(
        "Commerce.Order.CartInvalid",
        "Order requires buyer name/email, at least one active positive-quantity cart item of a uniform currency, and a supported fulfillment method."
    );

    public static readonly DomainError OrderNotFound = DomainError.NotFound(
        "Commerce.Order.NotFound",
        "The requested commerce order was not found."
    );

    public static readonly DomainError OrderStateTransitionInvalid = DomainError.Conflict(
        "Commerce.Order.InvalidTransition",
        "The requested fulfillment or refund operation is not valid for the order's current status."
    );

    public static readonly DomainError DigitalDownloadNotEligible = DomainError.Conflict(
        "Commerce.Order.DigitalDownloadNotEligible",
        "Digital download links are available only for settled digital order lines."
    );

    public static readonly DomainError PunchPassNotFound = DomainError.NotFound(
        "Commerce.PunchPass.NotFound",
        "The requested issued punch pass was not found."
    );

    public static readonly DomainError PunchPassRedemptionDenied = DomainError.Conflict(
        "Commerce.PunchPass.RedemptionDenied",
        "Punch pass is inactive, expired, restricted to another branch, unauthorized for beneficiary, or lacks sufficient remaining credits."
    );

    public static readonly DomainError BookableServiceInvalid = DomainError.Validation(
        "Commerce.Booking.ServiceInvalid",
        "Bookable service requires valid name, slug, duration/increment bounds, currency, and a revenue account when paid."
    );

    public static readonly DomainError BookableServiceNotFound = DomainError.NotFound(
        "Commerce.Booking.ServiceNotFound",
        "The requested bookable service was not found."
    );

    public static readonly DomainError RentableResourceInvalid = DomainError.Validation(
        "Commerce.Booking.ResourceInvalid",
        "Rentable resource requires valid name, slug, active Core Location, duration bounds, positive quantity, and a revenue account when paid."
    );

    public static readonly DomainError RentableResourceNotFound = DomainError.NotFound(
        "Commerce.Booking.ResourceNotFound",
        "The requested rentable resource was not found."
    );

    public static readonly DomainError SlugConflict = DomainError.Conflict(
        "Commerce.Booking.SlugConflict",
        "Another bookable service or rentable resource already uses this slug."
    );

    public static readonly DomainError AvailabilityRuleInvalid = DomainError.Validation(
        "Commerce.Booking.AvailabilityRuleInvalid",
        "Availability rule requires a target service or resource and a valid schedule, weekly time window, or UTC date window."
    );

    public static readonly DomainError AvailabilityRuleNotFound = DomainError.NotFound(
        "Commerce.Booking.AvailabilityRuleNotFound",
        "The requested availability rule was not found."
    );

    public static readonly DomainError SlotUnavailable = DomainError.Conflict(
        "Commerce.Booking.SlotUnavailable",
        "The requested time slot is outside availability/lead-time rules, blocked by a blackout, or conflicts with existing bookings or holds."
    );

    public static readonly DomainError HoldExpiredOrInvalid = DomainError.Conflict(
        "Commerce.Booking.HoldExpired",
        "The supplied booking slot hold token was not found, does not match this slot, or has expired."
    );

    public static readonly DomainError BookingNotFound = DomainError.NotFound(
        "Commerce.Booking.NotFound",
        "The requested service or resource booking was not found."
    );

    public static readonly DomainError BookingCancellationDenied = DomainError.Conflict(
        "Commerce.Booking.CancellationDenied",
        "Booking is already cancelled or is inside the minimum cancellation notice window without staff override."
    );

    public static readonly DomainError BookingPaymentInvalid = DomainError.Validation(
        "Commerce.Booking.PaymentInvalid",
        "Paid bookings require an electronic payment token, saved payment method, counter transaction reference, or allowed invoice billing."
    );
}
