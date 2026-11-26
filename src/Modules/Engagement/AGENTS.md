# AGENTS.md — `Engagement` Module

## Purpose & Boundaries
- Owns the `engage` PostgreSQL schema across two projects: `Ruth.Modules.Engagement.Contracts` and `Ruth.Modules.Engagement`.
- `Ruth.Modules.Engagement.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Engagement.Contracts`. `Ruth.Modules.Engagement` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Communication.Contracts`, `Identity.Contracts`).

## Durable Invariants
- **W5-S1 (`PipelineType`, `PipelineStage`, `PipelineActivityType`, `EngagementOpportunity`, `EngagementRequest`):**
  - `PipelineType` models organization-agnostic onboarding, volunteer intake, mentorship, and referral pipelines with sequential or free-form stage rules, idle thresholds, and declarative `StageAutomation` hooks.
  - `EngagementRequest` enforces `Active`, `FutureFollowUp`, `Connected`, and `Inactive` transitions, placement group requirements (`Groups.Contracts`), and connector assignments across `EngagementOpportunity` branch rules.
- **W5-S2 (`JourneyProgram`, `MilestoneType`, `MilestoneStatus`, `PersonMilestone`, `JourneyCompletion`, `StreakType`, `Streak`, `AchievementType`, `AchievementAttempt`):**
  - `JourneyPrerequisiteValidator` enforces acyclic milestone prerequisite graphs (`MilestoneType.PrerequisiteMilestoneTypeIds`) and single/multi-completion constraints before recording `PersonMilestone` completions and `JourneyCompletion` rollups.
  - `StreakBitmaskEngine` evaluates daily and reporting-week participation bitmasks over safe `Span<byte>` / `ReadOnlySpan<byte>` buffers using `BitOperations.PopCount`, forgiving `StreakExclusion` dates without breaking active runs and bitwise-OR merging `EngagementMap` buffers on person merges.
  - `AchievementEvaluator` tracks cumulative, streak, milestone, journey, and engagement connection attempts, honoring prerequisites, over-achievement flags, and maximum success caps.
- **W5-S3 (`SupportCategory`, `SupportRequest`, `ReminderType`, `Reminder`, `NotificationMessage`, `FollowingSubscription`, `FollowingEventNotification`):**
  - `SupportRequest` remains organization-agnostic across community care, member assistance, wellbeing check-ins, mentorship, and optional prayer/pastoral intake—enforcing moderation queues, visibility scopes, community support counters, auto-flag review thresholds, internal vs. public comments, and expiration.
  - `Reminder` supports one-time and recurring entity follow-ups emitting in-app `NotificationMessage` bell items and optional `SystemNotificationRequestedIntegrationEvent` messages; `FollowingSubscription` aggregates entity events into consolidated digest notifications.
- **W5-S4 (`ConstituentContact`, `Touchpoint`, `TouchpointCadenceScheduler`):**
  - `ConstituentContact` and `TouchpointCadenceScheduler` compute deterministic outreach and connection-note due dates, first-principles relationship health scores (`0..100`), prioritized outreach queues, and `Crm.Contracts` peer-network relationship strength synchronization upon touchpoint completion.
  - `EngagementPersonMergedConsumer` idempotently repoints and deduplicates person references across pipelines, milestones, bitmask streaks, achievements, support requests, reminders, notifications, following subscriptions, and constituent portfolios upon `PersonMergedIntegrationEvent`.

## Verification
```bash
dotnet build src/Modules/Engagement/Ruth.Modules.Engagement/Ruth.Modules.Engagement.csproj -warnaserror
dotnet test tests/Ruth.Modules.Engagement.Tests/Ruth.Modules.Engagement.Tests.csproj
```
