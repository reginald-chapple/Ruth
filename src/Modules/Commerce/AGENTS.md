# AGENTS.md — `Commerce` Module

## Purpose & Boundaries
- Owns the `commerce` PostgreSQL schema across two projects: `Ruth.Modules.Commerce.Contracts` and `Ruth.Modules.Commerce`.
- `Ruth.Modules.Commerce.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Commerce.Contracts`. `Ruth.Modules.Commerce` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Finance.Contracts`, `Identity.Contracts`, `Tenancy.Contracts`).

## Durable Invariants
- **W4-S9 (`Product`, `ProductVariant`, `PunchPassType`, `MemberPricingRule`, `CommerceOrder`, `IssuedPunchPass`):**
  - `Product` and `ProductVariant` model `PhysicalPickupOrFlatRate`, `DigitalDownload` (`Core` `BinaryFile` backed by `RustFS` presigned URLs), and `PunchPassPack` (`PunchPassType`) items without multi-warehouse WMS or carrier-rate ERP bloat.
  - `MemberPricingRule` evaluates automatic tier and `Groups.Contracts` membership discounts, applying the most favorable eligible rule per order line.
  - `CommercePricingAndSplitEngine` enforces first-principles IRS *quid-pro-quo* split accounting: non-deductible commercial value (`min(NetLineSubtotal, FairMarketValueTotal)`) plus applicable sales tax and flat-rate shipping routes to the non-deductible `RevenueAccountId` (`OperatingRevenue` / `ProgramFee`), while any excess paid above fair-market value (`max(0, NetLineSubtotal - FairMarketValueTotal)`) routes to the tax-deductible `QuidProQuoCharitableAccountId` (`CharitableFund`) via `Finance.Contracts`.
  - `IssuedPunchPass` maintains an append-only credit/debit ledger (`PunchPassLedgerEntry`) and emits `PunchPassBalanceChangedIntegrationEvent` updates.
- **W4-S10 (`BookableService`, `RentableResource`, `AvailabilityRule`, `BookingSlotHold`, `ServiceOrResourceBooking`):**
  - `BookableService` (1-on-1 appointments and multi-seat remote/in-person workshops) and `RentableResource` (referencing `Core.Contracts` `Location` rooms, courts, studios, and equipment) share `AvailabilityRule` windows (`Core.Contracts` `Schedule` occurrences, recurring day/time windows, custom windows, and blackouts).
  - `BookingSlotEngine` evaluates conflict-free slots honoring setup and teardown buffers (`[StartUtc - SetupBuffer, EndUtc + TeardownBuffer)`), lead-time/advance-horizon bounds, blackout windows, capacity limits, and non-expired `BookingSlotHold` TTL reservations (`ExpiredBookingSlotHoldCleanupJob`).
  - Confirming a booking settles member-free, member-discounted, punch-pass, invoice, or gateway payment via `Finance.Contracts` and stages `ServiceBookingConfirmedIntegrationEvent` in the `commerce` Outbox for downstream workflows.
  - `CommercePersonMergedConsumer` idempotently repoints person references across orders, punch passes, services, availability rules, holds, and bookings upon `PersonMergedIntegrationEvent`.
- **Multi-Tenant & RLS Isolation (`W7-S6`):** Every table in `commerce` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`).

## Verification
```bash
dotnet build src/Modules/Commerce/Ruth.Modules.Commerce/Ruth.Modules.Commerce.csproj -warnaserror
dotnet test tests/Ruth.Modules.Commerce.Tests/Ruth.Modules.Commerce.Tests.csproj
```
