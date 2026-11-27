# AGENTS.md — `Lms` Module

## Purpose & Boundaries
- Owns the `lms` PostgreSQL schema across two projects: `Ruth.Modules.Lms.Contracts` and `Ruth.Modules.Lms`.
- `Ruth.Modules.Lms.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- `Ruth.Modules.Lms.Contracts` may only depend on `Ruth.SharedKernel`.

## Durable Invariants
- **W6-S7 (`LearningProgram`, `LearningTerm`, `LearningGradingSystem`/`Scale`, `LearningCourse` + prerequisites, `LearningClass`, `ContentPage`, `Announcement`, `LearningActivity`, `LearningParticipant`, `LearningActivityCompletion`):**
  - Supports both `OnDemandLearning` (rolling self-paced) and `AcademicCalendar` (term/cohort) program modes.
  - `CoursePrerequisiteEvaluator` enforces acyclic prerequisite graphs and verifies completion of prerequisites (or equivalent courses) prior to student enrollment unless staff explicitly bypasses.
  - `AssessmentAutoGrader` auto-awards points for Video/Article/Acknowledgment and objective answer-key Assessments, while routing FileUpload and subjective assessments to the Facilitator Grading Queue (`RequiresGrading = true`).
  - `LearningParticipant.RecalculateProgress` evaluates `LearningGradingSystem` scale thresholds and emits `LearningCourseCompletedIntegrationEvent` via Outbox upon first passing completion.
  - `LmsPersonMergedConsumer` idempotently repoints `PersonId` across enrollments and graded completions.
- **Multi-Tenant & RLS Isolation (`W7-S6`):** Every table in `lms` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`).

## Verification
```bash
dotnet build src/Modules/Lms/Ruth.Modules.Lms/Ruth.Modules.Lms.csproj -warnaserror
dotnet test tests/Ruth.Modules.Lms.Tests/Ruth.Modules.Lms.Tests.csproj
```
