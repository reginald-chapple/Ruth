# AGENTS.md — `VirtualRooms` Module

## Purpose & Boundaries
- Owns the `virtual` PostgreSQL schema across two projects: `Ruth.Modules.VirtualRooms.Contracts` and `Ruth.Modules.VirtualRooms`, paired with modular LiveKit integration in `Ruth.Providers.LiveKit`.
- `Ruth.Modules.VirtualRooms.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.VirtualRooms.Contracts`. `Ruth.Modules.VirtualRooms` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Commerce.Contracts`, `Engagement.Contracts`, `Events.Contracts`, `Identity.Contracts`).

## Durable Invariants
- **W5-S7 (`VirtualRoom`, `VirtualRoomSession`, `VirtualRoomParticipantSession`, `VirtualRoomRecordingArchive`, `IVirtualRoomProvider`):**
  - `VirtualRoom` models embedded Apache-2.0 LiveKit WebRTC rooms linked across `Group` meetings, `Commerce` 1-on-1 bookings/workshops (`ServiceBookingConfirmedIntegrationEvent`), `SupportRequest` tele-care consults, `Events` hybrid sessions, and `LMS` cohorts.
  - Room join tokens are short-lived HMAC-SHA256 JWTs mapped from `Ruth` `ISecured` ACL permissions (`Administrate`/`Edit` or assigned `HostModeratorPersonId` -> `HostModerator` with `roomAdmin`/`roomRecord`; `View` -> `Participant`/`Observer`).
  - Verified LiveKit webhooks (`participant_joined`, `participant_left`, `room_finished`, `egress_ended`) validate the `Authorization` JWT signature and constant-time SHA-256 request body hash, accumulate monotonic participant dwell seconds across reconnects, auto-record `Groups.Contracts` `Attendance` once `CumulativeDwellSeconds >= MinimumAttendanceDwellSeconds` without duplicate writes, and archive `RustFS` S3 recording objects emitting `VirtualRoomRecordingArchivedIntegrationEvent` for Wave 6 `pgvector` transcript search.
  - `VirtualRoomsPersonMergedConsumer` idempotently repoints `PersonId` references across room moderators and session participant records upon `PersonMergedIntegrationEvent`.

## Verification
```bash
dotnet build src/Modules/VirtualRooms/Ruth.Modules.VirtualRooms/Ruth.Modules.VirtualRooms.csproj -warnaserror
dotnet test tests/Ruth.Modules.VirtualRooms.Tests/Ruth.Modules.VirtualRooms.Tests.csproj
```
