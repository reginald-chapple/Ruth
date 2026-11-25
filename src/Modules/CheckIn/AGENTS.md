# AGENTS.md — `CheckIn` Module

## Purpose & Boundaries
- Owns the `checkin` PostgreSQL schema across two projects: `Ruth.Modules.CheckIn.Contracts` and `Ruth.Modules.CheckIn`.
- `Ruth.Modules.CheckIn.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.CheckIn.Contracts`. `Ruth.Modules.CheckIn` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`).

## Durable Invariants
- **W3-S2 (`Configurations`, `Areas`, `Eligibility`, `RoomBalancing`):**
  - `CheckInConfiguration` models organization-neutral check-in profiles (`YouthAndChildcare`, `MemberFacilityAccess`, `ClassesAndPrograms`, `VolunteerAndStaffShifts`, `EventAndConference`), validating security pickup code lengths (`0..6`, at least 3 characters when pickup codes are enabled on any area) and check-in window offsets.
  - `CheckInEligibilityRule` enforces `MinAgeYears <= MaxAgeYears`, `MinBirthDate <= MaxBirthDate`, `MinGrade <= MaxGrade`, normalized membership tier and skill/badge prerequisites, accommodation flags, and enrollment gating (`OpenToEligible`, `PreEnrolledMembersOnly`, `ScheduledShiftOnly`).
  - `CheckInAreaLocation` enforces `SoftCapacity <= FirmCapacity` (`>= 0`), unique `(CheckInAreaId, LocationId)` matrix cells, and schedule activation lists.
  - `RoomBalancingDomainService` enforces `FirmCapacity` hard stops, holds `IsOverflowRoom` cells back until primary rooms reach `SoftCapacity`, and deterministically ranks eligible rooms via `OrderedPriority`, `LeastOccupancyCount`, or `LowestCapacityRatio`.

## Verification
```bash
dotnet build src/Modules/CheckIn/Ruth.Modules.CheckIn/Ruth.Modules.CheckIn.csproj -warnaserror
dotnet test tests/Ruth.Modules.CheckIn.Tests/Ruth.Modules.CheckIn.Tests.csproj
```
