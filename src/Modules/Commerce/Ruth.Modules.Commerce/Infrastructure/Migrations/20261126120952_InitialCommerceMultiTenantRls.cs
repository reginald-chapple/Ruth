using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ruth.Modules.Commerce.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommerceMultiTenantRls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "commerce");

            migrationBuilder.CreateTable(
                name: "availability_rules",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BookableServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RentableResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduleId = table.Column<Guid>(type: "uuid", nullable: true),
                    DayOfWeek = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    StartLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndLocalTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CustomStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CustomEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_availability_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "bookable_services",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    DeliveryMode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SlotIncrementMinutes = table.Column<int>(type: "integer", nullable: false),
                    SetupBufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    TeardownBufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipantsPerSlot = table.Column<int>(type: "integer", nullable: false),
                    MinimumLeadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaximumAdvanceDays = table.Column<int>(type: "integer", nullable: false),
                    CancellationNoticeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    StandardPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsFreeForMembers = table.Column<bool>(type: "boolean", nullable: false),
                    MemberOverridePrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FreeOrDiscountedMemberGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    FreeOrDiscountedMemberTier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    AllowPunchPassRedemption = table.Column<bool>(type: "boolean", nullable: false),
                    PunchPassCreditsRequired = table.Column<int>(type: "integer", nullable: false),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowInvoiceBilling = table.Column<bool>(type: "boolean", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_provider_person_ids = table.Column<string>(type: "jsonb", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookable_services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "booking_slot_holds",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldToken = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    BookableServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RentableResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProviderPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SlotStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SlotEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveBlockStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveBlockEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_booking_slot_holds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "inbox_messages",
                schema: "commerce",
                columns: table => new
                {
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_inbox_messages", x => new { x.tenant_id, x.message_id, x.consumer_name });
                });

            migrationBuilder.CreateTable(
                name: "issued_punch_passes",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PassCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PunchPassTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPersonId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitialCredits = table.Column<int>(type: "integer", nullable: false),
                    RemainingCredits = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    AllowedBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowHouseholdSharing = table.Column<bool>(type: "boolean", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issued_punch_passes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "member_pricing_rules",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DiscountKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RequiredMembershipTier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RequiredGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequiredGroupTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliesToProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliesToProductKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_pricing_rules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    BuyerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    BuyerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    FulfillmentMethod = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ShippingAddressSummary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrackingReference = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MemberDiscountTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxTotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NonDeductibleRevenueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxDeductibleContributionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LinkedFinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "commerce",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    next_retry_on_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    locked_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lock_token = table.Column<Guid>(type: "uuid", nullable: true),
                    error = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImageBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsQuidProQuo = table.Column<bool>(type: "boolean", nullable: false),
                    QuidProQuoCharitableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsTaxable = table.Column<bool>(type: "boolean", nullable: false),
                    FlatTaxRatePercent = table.Column<decimal>(type: "numeric(8,4)", precision: 8, scale: 4, nullable: false),
                    AllowLocalPickup = table.Column<bool>(type: "boolean", nullable: false),
                    AllowFlatRateShipping = table.Column<bool>(type: "boolean", nullable: false),
                    FlatShippingFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "punch_pass_types",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreditUnits = table.Column<int>(type: "integer", nullable: false),
                    ValidityDays = table.Column<int>(type: "integer", nullable: true),
                    AllowedBranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowHouseholdSharing = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_punch_pass_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "rentable_resources",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResourceKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CoreLocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    QuantityAvailable = table.Column<int>(type: "integer", nullable: false),
                    MinDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaxDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SlotIncrementMinutes = table.Column<int>(type: "integer", nullable: false),
                    SetupBufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    TeardownBufferMinutes = table.Column<int>(type: "integer", nullable: false),
                    MinimumLeadTimeMinutes = table.Column<int>(type: "integer", nullable: false),
                    MaximumAdvanceDays = table.Column<int>(type: "integer", nullable: false),
                    CancellationNoticeMinutes = table.Column<int>(type: "integer", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FlatCleaningOrSetupFee = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    IsFreeForMembers = table.Column<bool>(type: "boolean", nullable: false),
                    MemberHourlyRate = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    FreeOrDiscountedMemberGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    FreeOrDiscountedMemberTier = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultFinancialGatewayId = table.Column<Guid>(type: "uuid", nullable: true),
                    AllowInvoiceBilling = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false),
                    attribute_values = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rentable_resources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "service_bookings",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfirmationCode = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    TargetKind = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    BookableServiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RentableResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeliveryMode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    ProviderPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CustomerNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ParticipantOrUnitCount = table.Column<int>(type: "integer", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveBlockStartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EffectiveBlockEndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PricingKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    ListAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LinkedFinancialTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LinkedInvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RedeemedPunchPassId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ModifiedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConcurrencyStamp = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "punch_pass_ledger_entries",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssuedPunchPassId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DeltaCredits = table.Column<int>(type: "integer", nullable: false),
                    BalanceAfter = table.Column<int>(type: "integer", nullable: false),
                    RedeemedForPersonId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_punch_pass_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_punch_pass_ledger_entries_issued_punch_passes_IssuedPunchPa~",
                        column: x => x.IssuedPunchPassId,
                        principalSchema: "commerce",
                        principalTable: "issued_punch_passes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProductName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VariantName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    ProductKind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ListUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    UnitMemberDiscountAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NetUnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FairMarketValuePerUnit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineSubtotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineTaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    LineShippingFeeAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    NonDeductibleRevenueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    RevenueAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxDeductibleContributionAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CharitableAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedMemberPricingRuleId = table.Column<Guid>(type: "uuid", nullable: true),
                    AppliedMemberPricingRuleName = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    DigitalBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    PunchPassTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    IssuedPunchPassId = table.Column<Guid>(type: "uuid", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_lines_orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "commerce",
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_variants",
                schema: "commerce",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    FairMarketValueAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DigitalBinaryFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    PunchPassTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    TrackInventory = table.Column<bool>(type: "boolean", nullable: false),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_variants_products_ProductId",
                        column: x => x.ProductId,
                        principalSchema: "commerce",
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_availability_rules_BookableServiceId",
                schema: "commerce",
                table: "availability_rules",
                column: "BookableServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_availability_rules_RentableResourceId",
                schema: "commerce",
                table: "availability_rules",
                column: "RentableResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_availability_rules_tenant_id",
                schema: "commerce",
                table: "availability_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_bookable_services_attribute_values",
                schema: "commerce",
                table: "bookable_services",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_bookable_services_IsActive_IsPublic",
                schema: "commerce",
                table: "bookable_services",
                columns: new[] { "IsActive", "IsPublic" });

            migrationBuilder.CreateIndex(
                name: "IX_bookable_services_tenant_id_Slug",
                schema: "commerce",
                table: "bookable_services",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_holds_EffectiveBlockStartUtc_EffectiveBlockEnd~",
                schema: "commerce",
                table: "booking_slot_holds",
                columns: new[] { "EffectiveBlockStartUtc", "EffectiveBlockEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_holds_IsCompleted_ExpiresAtUtc",
                schema: "commerce",
                table: "booking_slot_holds",
                columns: new[] { "IsCompleted", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_booking_slot_holds_tenant_id_HoldToken",
                schema: "commerce",
                table: "booking_slot_holds",
                columns: new[] { "tenant_id", "HoldToken" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issued_punch_passes_OwnerPersonId_Status",
                schema: "commerce",
                table: "issued_punch_passes",
                columns: new[] { "OwnerPersonId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_issued_punch_passes_tenant_id_PassCode",
                schema: "commerce",
                table: "issued_punch_passes",
                columns: new[] { "tenant_id", "PassCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_pricing_rules_IsActive_Priority",
                schema: "commerce",
                table: "member_pricing_rules",
                columns: new[] { "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_member_pricing_rules_tenant_id",
                schema: "commerce",
                table: "member_pricing_rules",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_OrderId",
                schema: "commerce",
                table: "order_lines",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_lines_tenant_id",
                schema: "commerce",
                table: "order_lines",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_BuyerPersonId",
                schema: "commerce",
                table: "orders",
                column: "BuyerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status_CreatedOnUtc",
                schema: "commerce",
                table: "orders",
                columns: new[] { "Status", "CreatedOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_orders_tenant_id_OrderNumber",
                schema: "commerce",
                table: "orders",
                columns: new[] { "tenant_id", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_tenant_id_processed_on_utc_next_retry_on_utc",
                schema: "commerce",
                table: "outbox_messages",
                columns: new[] { "tenant_id", "processed_on_utc", "next_retry_on_utc" },
                filter: "\"processed_on_utc\" IS NULL AND \"dead_lettered_on_utc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_ProductId",
                schema: "commerce",
                table: "product_variants",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_product_variants_tenant_id_Sku",
                schema: "commerce",
                table: "product_variants",
                columns: new[] { "tenant_id", "Sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_attribute_values",
                schema: "commerce",
                table: "products",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_products_IsActive_IsPublic_Kind",
                schema: "commerce",
                table: "products",
                columns: new[] { "IsActive", "IsPublic", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_products_tenant_id_Slug",
                schema: "commerce",
                table: "products",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_punch_pass_ledger_entries_IssuedPunchPassId",
                schema: "commerce",
                table: "punch_pass_ledger_entries",
                column: "IssuedPunchPassId");

            migrationBuilder.CreateIndex(
                name: "IX_punch_pass_ledger_entries_tenant_id",
                schema: "commerce",
                table: "punch_pass_ledger_entries",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "IX_punch_pass_types_tenant_id_Code",
                schema: "commerce",
                table: "punch_pass_types",
                columns: new[] { "tenant_id", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_rentable_resources_attribute_values",
                schema: "commerce",
                table: "rentable_resources",
                column: "attribute_values")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_rentable_resources_CoreLocationId",
                schema: "commerce",
                table: "rentable_resources",
                column: "CoreLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_rentable_resources_tenant_id_Slug",
                schema: "commerce",
                table: "rentable_resources",
                columns: new[] { "tenant_id", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_CustomerPersonId",
                schema: "commerce",
                table: "service_bookings",
                column: "CustomerPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_Status_EffectiveBlockStartUtc_EffectiveBlo~",
                schema: "commerce",
                table: "service_bookings",
                columns: new[] { "Status", "EffectiveBlockStartUtc", "EffectiveBlockEndUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_service_bookings_tenant_id_ConfirmationCode",
                schema: "commerce",
                table: "service_bookings",
                columns: new[] { "tenant_id", "ConfirmationCode" },
                unique: true);
            Ruth.Persistence.EntityFramework.TenantRlsMigrationExtensions.EnableTenantRowLevelSecurity(
                migrationBuilder,
                "commerce"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "availability_rules",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "bookable_services",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "booking_slot_holds",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "inbox_messages",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "member_pricing_rules",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "order_lines",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "product_variants",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "punch_pass_ledger_entries",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "punch_pass_types",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "rentable_resources",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "service_bookings",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "products",
                schema: "commerce");

            migrationBuilder.DropTable(
                name: "issued_punch_passes",
                schema: "commerce");
        }
    }
}
