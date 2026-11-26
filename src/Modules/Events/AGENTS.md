# AGENTS.md — `Events` Module

## Purpose & Boundaries
- Owns the `events` PostgreSQL schema across two projects: `Ruth.Modules.Events.Contracts` and `Ruth.Modules.Events`.
- `Ruth.Modules.Events.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, SignalR, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Events.Contracts`. `Ruth.Modules.Events` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Finance.Contracts`, `Communication.Contracts`, `Identity.Contracts`).

## Durable Invariants
- **W4-S7 (`Calendars`, `EventItems`, `Occurrences`, `iCal`, `InteractiveSessions`):**
  - `EventCalendar` and `EventItem` model organization-agnostic public and internal calendars, audience classifications (`EventItemAudience`), and multi-branch/location/schedule occurrences (`EventItemOccurrence`).
  - `IcsCalendarFeedBuilder` compiles strictly standards-compliant RFC 5545 `VCALENDAR` 2.0 feeds (`75`-octet line folding, escaped property values, UTC `DTSTART`/`DTEND` timestamps, and recurrence expansion via `Core.Contracts` `Schedule` occurrences).
  - `InteractiveSession` manages live SignalR town-hall, conference, and classroom polls, Q&A moderation queues, single-response participant deduplication when `!AllowMultipleResponses`, and real-time stage projections (`/hubs/events/interactive`).
- **W4-S8 (`RegistrationTemplate`, `RegistrationInstance`, `RegistrationSession`, `Registration`, `Waitlists`):**
  - `RegistrationTemplate` configures dynamic forms/conditional fields, single/multiple fee options with inventory caps, promo/early-bird discounts, payment models (`Free`, `PayInFull`, `DepositAndInstallments`, `InvoiceBilling`), seat-hold TTLs, and `Groups.Contracts` placement rules.
  - `RegistrationCapacityAndPricingEngine` evaluates confirmed vs waitlisted seat allocation accounting for non-expired `RegistrationSession` holds, validates visible dynamic fields, enforces fee item caps, and calculates discounts and minimum initial deposits.
  - Cancelling a confirmed registrant or expiring a stale `RegistrationSession` hold (`ExpiredRegistrationSessionCleanupJob`) triggers FIFO waitlist promotion (`WaitlistPromotionService`) and stages `RegistrantPromotedFromWaitlistIntegrationEvent` in the `events` Outbox.
  - `EventsPersonMergedConsumer` idempotently repoints `EventItem`, `InteractiveSession`, `Registration`, and `RegistrationRegistrant` person references upon `PersonMergedIntegrationEvent`.

## Verification
```bash
dotnet build src/Modules/Events/Ruth.Modules.Events/Ruth.Modules.Events.csproj -warnaserror
dotnet test tests/Ruth.Modules.Events.Tests/Ruth.Modules.Events.Tests.csproj
```
