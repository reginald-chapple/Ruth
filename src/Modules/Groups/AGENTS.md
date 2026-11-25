# AGENTS.md — `Groups` Module

## Purpose & Boundaries
- Owns the `groups` PostgreSQL schema across two projects: `Ruth.Modules.Groups.Contracts` and `Ruth.Modules.Groups`.
- `Ruth.Modules.Groups.Domain` must remain pure C# (plus `NetTopologySuite.Geometries` for WGS84 geographic values) with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Groups.Contracts`. `Ruth.Modules.Groups` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`).

## Durable Invariants
- **W2-S5 (`GroupTypes`, `Groups`, `Locations`, `Finder`, generalized in `W3-S0`):** `GroupType` owns `GroupTypeRole` (`MinCount <= MaxCount`, unique role names) and allowed child `GroupType` relationships. `Group` enforces cycle-free hierarchy, branch requirements when `GroupsRequireBranch` is set, single-location limits when `!AllowMultipleLocations`, PostGIS `geography(Point, 4326)` / `geography(Polygon, 4326)` spatial search (`ST_DWithin`, `ST_Distance`, `ST_Covers`), demographic values, and SCD Type-2 `GroupHistorical` snapshots.
- **W2-S6 (`Members`, `Requirements`, `Placement`, `Sync`):** `GroupMember` enforces role ownership, non-archived `(GroupId, PersonId, GroupRoleId)` uniqueness, role `MaxCount`, `GroupCapacityRule` (`Hard` block vs `Soft` warning), `MustMeetRequirementToAddMember` gating, `GroupMemberHistorical` snapshots, `GroupRequirement` single-scope invariant (`GroupId` XOR `GroupTypeId`), placement board validation, public simple registration, and scheduled `GroupSyncReconciliationJob`.
- **W2-S7 (`Scheduling` & `RSVPs`):** `GroupLocationScheduleConfig` enforces `MinimumCapacity <= DesiredCapacity <= MaximumCapacity`. `ServingScheduleEvaluator` validates group scheduling enablement, `GroupScheduleExclusion` and `PersonScheduleExclusion` blackout windows, requirement eligibility, double-booking detection, and tokenized Accept/Decline RSVP state transitions.
- **W2-S8 (`Attendance`, generalized in `W3-S0`):** `AttendanceOccurrence` enforces unique `(GroupId, LocationId, ScheduleId, OccurrenceDate)`, deterministic configurable `WeekEndDate` (`ReportingWeekCalculator`) and `OccurrenceDateKey`, `DidNotOccur` invariants, automatic `IsFirstTime` detection, branch/program metric summaries, and idempotent Transactional Outbox `PersonAttendedIntegrationEvent` emission.
- **Cross-Module Merge:** `GroupsPersonMergedConsumer` idempotently repoints `PersonId` across memberships, history, requirements, blackouts, group coordinators/admins, and attendance records upon `PersonMergedIntegrationEvent`.

## Verification
```bash
dotnet build src/Modules/Groups/Ruth.Modules.Groups/Ruth.Modules.Groups.csproj -warnaserror
dotnet test tests/Ruth.Modules.Groups.Tests/Ruth.Modules.Groups.Tests.csproj
```
