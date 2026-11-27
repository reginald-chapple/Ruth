# AGENTS.md — `CheckIn` Module

## Purpose & Boundaries
- Owns the `checkin` PostgreSQL schema across two projects: `Ruth.Modules.CheckIn.Contracts` and `Ruth.Modules.CheckIn`.
- `Ruth.Modules.CheckIn.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.CheckIn.Contracts`. `Ruth.Modules.CheckIn` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Communication.Contracts`).

## Durable Invariants
- **W3-S2 (`Configurations`, `Areas`, `Eligibility`, `RoomBalancing`):**
  - `CheckInConfiguration` models organization-neutral check-in profiles (`YouthAndChildcare`, `MemberFacilityAccess`, `ClassesAndPrograms`, `VolunteerAndStaffShifts`, `EventAndConference`), validating security pickup code lengths (`0..6`, at least 3 characters when pickup codes are enabled on any area) and check-in window offsets.
  - `CheckInEligibilityRule` enforces `MinAgeYears <= MaxAgeYears`, `MinBirthDate <= MaxBirthDate`, `MinGrade <= MaxGrade`, normalized membership tier and skill/badge prerequisites, accommodation flags, and enrollment gating (`OpenToEligible`, `PreEnrolledMembersOnly`, `ScheduledShiftOnly`).
  - `CheckInAreaLocation` enforces `SoftCapacity <= FirmCapacity` (`>= 0`), unique `(CheckInAreaId, LocationId)` matrix cells, and schedule activation lists.
  - `RoomBalancingDomainService` enforces `FirmCapacity` hard stops, holds `IsOverflowRoom` cells back until primary rooms reach `SoftCapacity`, and deterministically ranks eligible rooms via `OrderedPriority`, `LeastOccupancyCount`, or `LowestCapacityRatio`.
- **W3-S3 (`Sessions`, `SecurityPickupCode`, `KioskSearch`, `RoomManager`, `MobileCheckIn`):**
  - `SecurityPickupCodeGenerator` uses unambiguous uppercase letters (`BCDFGHJKLMNPQRSTVWXYZ`) and digits (`23456789`) with leetspeak-normalized `SearchValues<string>` profanity filtering, and `DailySecurityPickupCode` enforces `(ServiceDate, Code)` database uniqueness across concurrent kiosks.
  - `AttendanceCheckInSession` and `CheckInAttendanceEntry` manage kiosk, staff, and mobile pre-check-in (`CHK-*` express token) lifecycles, check-out transitions, staff room overrides, and `PersonMergedIntegrationEvent` person-ID repointing.
  - Kiosk search batches household/member candidate resolution via `LookupKioskHouseholdsQuery` (`Crm.Contracts`) without cross-module `DbContext` coupling.
- **W3-S4 (`Labels`, `ZplCompiler`, `TcpRawSocketDispatcher`, `WebUsbPrint`):**
  - `CheckInLabel` validates 203/300 DPI thermal layouts (`CheckInLabelLayoutSchema`) and protects built-in system definitions (`participant-safety-badge`, `guardian-pickup-receipt`) from deletion.
  - `ZplLabelCompiler` and `ZplTextSanitizer` escape all interpolated merge values using Zebra ZPL II `^FH_` hexadecimal encoding (`_5E`, `_7E`, `_5C`, `_5F`) to prevent ZPL command injection (`^XA`/`^XZ`/`~` control injection).
  - Server-side printing dispatches raw UTF-8 ZPL over TCP port `9100` (`IZplPrinterDispatcher` / `TcpRawSocketZplPrinterDispatcher`), while client kiosks use `wwwroot/ruth-label-print.js` (WebUSB bulk-OUT with HTML print fallback).
- **Multi-Tenant & RLS Isolation (`W7-S5`):** Every table in `checkin` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), scoping `DailySecurityPickupCode` and `CheckInLabel` (`CheckInTenantSeedContributor`) uniqueness per tenant.

## Verification
```bash
dotnet build src/Modules/CheckIn/Ruth.Modules.CheckIn/Ruth.Modules.CheckIn.csproj -warnaserror
dotnet test tests/Ruth.Modules.CheckIn.Tests/Ruth.Modules.CheckIn.Tests.csproj
```
