using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Ruth.Modules.Commerce.Contracts.Bookings;
using Ruth.Modules.Commerce.Contracts.Catalog;
using Ruth.Modules.Commerce.Contracts.Events;
using Ruth.Modules.Commerce.Contracts.Orders;
using Ruth.Modules.Commerce.Contracts.Pricing;
using Ruth.Modules.Commerce.Contracts.PunchPasses;
using Ruth.Modules.Commerce.Domain.Pricing;
using Ruth.Modules.Commerce.Infrastructure;
using Ruth.Modules.Commerce.Slices.Bookings;
using Ruth.Modules.Commerce.Slices.Catalog;
using Ruth.Modules.Commerce.Slices.Events;
using Ruth.Modules.Commerce.Slices.Orders;
using Ruth.Modules.Commerce.Slices.Pricing;
using Ruth.Modules.Commerce.Slices.PunchPasses;
using Ruth.Modules.Crm.Contracts.Events;
using Ruth.Persistence.DependencyInjection;
using Ruth.SharedKernel.DependencyInjection;

namespace Ruth.Modules.Commerce.Slices;

internal static class CommerceSliceRegistration
{
    public static void RegisterAllServices(IServiceCollection services)
    {
        services.TryAddSingleton<ICommerceTaxCalculator, FlatRateCommerceTaxCalculator>();
        services.TryAddScoped<MemberEligibilityResolver>();
        services.TryAddScoped<WalletPassCommerceSyncCollaborator>();
        services.TryAddScoped<ProductCatalogValidator>();
        services.TryAddScoped<CommerceCartEvaluationCollaborator>();
        services.TryAddScoped<CommerceOrderFinanceCollaborator>();
        services.TryAddScoped<BookingScheduleAndConflictLoader>();
        services.TryAddScoped<BookingTargetValidator>();
        services.TryAddScoped<BookingSettlementCollaborator>();

        _ = services.AddScheduledJob<ExpiredBookingSlotHoldCleanupJob>();
        _ = services.AddIdempotentIntegrationEventConsumer<
            PersonMergedIntegrationEvent,
            CommercePersonMergedConsumer,
            CommerceDbContext
        >();
        _ = services.AddIdempotentIntegrationEventConsumer<
            ServiceBookingConfirmedIntegrationEvent,
            ServiceBookingWalletPassSyncConsumer,
            CommerceDbContext
        >();

        RegisterW4S9CatalogAndPricingHandlers(services);
        RegisterW4S9OrderAndPunchPassHandlers(services);
        RegisterW4S10TargetAndRuleHandlers(services);
        RegisterW4S10SlotAndBookingHandlers(services);
    }

    private static void RegisterW4S9CatalogAndPricingHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreatePunchPassTypeCommand,
            PunchPassTypeResponse,
            CreatePunchPassTypeCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdatePunchPassTypeCommand,
            PunchPassTypeResponse,
            UpdatePunchPassTypeCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetPunchPassTypesQuery,
            IReadOnlyList<PunchPassTypeResponse>,
            GetPunchPassTypesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateProductCommand,
            ProductResponse,
            CreateProductCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateProductCommand,
            ProductResponse,
            UpdateProductCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetProductByIdOrSlugQuery,
            ProductResponse,
            GetProductByIdOrSlugQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetProductsQuery,
            IReadOnlyList<ProductResponse>,
            GetProductsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateMemberPricingRuleCommand,
            MemberPricingRuleResponse,
            CreateMemberPricingRuleCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateMemberPricingRuleCommand,
            MemberPricingRuleResponse,
            UpdateMemberPricingRuleCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetMemberPricingRulesQuery,
            IReadOnlyList<MemberPricingRuleResponse>,
            GetMemberPricingRulesQueryHandler
        >();
    }

    private static void RegisterW4S9OrderAndPunchPassHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            PreviewCommerceOrderQuoteQuery,
            CommerceOrderQuoteResponse,
            PreviewCommerceOrderQuoteQueryHandler
        >();
        _ = services.AddCommandHandler<
            SubmitCommerceOrderCommand,
            CommerceOrderResponse,
            SubmitCommerceOrderCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateCommerceOrderFulfillmentCommand,
            CommerceOrderResponse,
            UpdateCommerceOrderFulfillmentCommandHandler
        >();
        _ = services.AddCommandHandler<
            RefundCommerceOrderCommand,
            CommerceOrderResponse,
            RefundCommerceOrderCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetOrderDigitalDownloadLinkQuery,
            OrderDigitalDownloadLinkResponse,
            GetOrderDigitalDownloadLinkQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommerceOrderByNumberOrIdQuery,
            CommerceOrderResponse,
            GetCommerceOrderByNumberOrIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetCommerceOrdersQuery,
            IReadOnlyList<CommerceOrderResponse>,
            GetCommerceOrdersQueryHandler
        >();
        _ = services.AddCommandHandler<
            IssuePunchPassCommand,
            IssuedPunchPassResponse,
            IssuePunchPassCommandHandler
        >();
        _ = services.AddCommandHandler<
            RedeemPunchPassCreditsCommand,
            IssuedPunchPassResponse,
            RedeemPunchPassCreditsCommandHandler
        >();
        _ = services.AddCommandHandler<
            AdjustPunchPassBalanceCommand,
            IssuedPunchPassResponse,
            AdjustPunchPassBalanceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetIssuedPunchPassesQuery,
            IReadOnlyList<IssuedPunchPassResponse>,
            GetIssuedPunchPassesQueryHandler
        >();
    }

    private static void RegisterW4S10TargetAndRuleHandlers(IServiceCollection services)
    {
        _ = services.AddCommandHandler<
            CreateBookableServiceCommand,
            BookableServiceResponse,
            CreateBookableServiceCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateBookableServiceCommand,
            BookableServiceResponse,
            UpdateBookableServiceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetBookableServiceByIdOrSlugQuery,
            BookableServiceResponse,
            GetBookableServiceByIdOrSlugQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetBookableServicesQuery,
            IReadOnlyList<BookableServiceResponse>,
            GetBookableServicesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateRentableResourceCommand,
            RentableResourceResponse,
            CreateRentableResourceCommandHandler
        >();
        _ = services.AddCommandHandler<
            UpdateRentableResourceCommand,
            RentableResourceResponse,
            UpdateRentableResourceCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetRentableResourceByIdOrSlugQuery,
            RentableResourceResponse,
            GetRentableResourceByIdOrSlugQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetRentableResourcesQuery,
            IReadOnlyList<RentableResourceResponse>,
            GetRentableResourcesQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateAvailabilityRuleCommand,
            AvailabilityRuleResponse,
            CreateAvailabilityRuleCommandHandler
        >();
        _ = services.AddCommandHandler<
            DeleteAvailabilityRuleCommand,
            DeleteAvailabilityRuleCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetAvailabilityRulesQuery,
            IReadOnlyList<AvailabilityRuleResponse>,
            GetAvailabilityRulesQueryHandler
        >();
    }

    private static void RegisterW4S10SlotAndBookingHandlers(IServiceCollection services)
    {
        _ = services.AddQueryHandler<
            QueryAvailableBookingSlotsQuery,
            IReadOnlyList<AvailableBookingSlotDto>,
            QueryAvailableBookingSlotsQueryHandler
        >();
        _ = services.AddCommandHandler<
            CreateBookingSlotHoldCommand,
            BookingSlotHoldResponse,
            CreateBookingSlotHoldCommandHandler
        >();
        _ = services.AddCommandHandler<
            ExpireBookingSlotHoldsCommand,
            BookingSlotHoldCleanupResponse,
            ExpireBookingSlotHoldsCommandHandler
        >();
        _ = services.AddQueryHandler<
            PreviewBookingQuoteQuery,
            BookingQuoteResponse,
            PreviewBookingQuoteQueryHandler
        >();
        _ = services.AddCommandHandler<
            ConfirmBookingCommand,
            ServiceBookingResponse,
            ConfirmBookingCommandHandler
        >();
        _ = services.AddCommandHandler<
            CancelBookingCommand,
            ServiceBookingResponse,
            CancelBookingCommandHandler
        >();
        _ = services.AddQueryHandler<
            GetServiceBookingByCodeOrIdQuery,
            ServiceBookingResponse,
            GetServiceBookingByCodeOrIdQueryHandler
        >();
        _ = services.AddQueryHandler<
            GetServiceBookingsQuery,
            IReadOnlyList<ServiceBookingResponse>,
            GetServiceBookingsQueryHandler
        >();
    }
}
