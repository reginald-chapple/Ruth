# AGENTS.md — `Workflow` Module

## Purpose & Boundaries
- Owns the `workflow` PostgreSQL schema across two projects: `Ruth.Modules.Workflow.Contracts` and `Ruth.Modules.Workflow`.
- `Ruth.Modules.Workflow.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Workflow.Contracts`. `Ruth.Modules.Workflow` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Groups.Contracts`, `Finance.Contracts`, `Engagement.Contracts`, `Communication.Contracts`, `Commerce.Contracts`, `Events.Contracts`, `Identity.Contracts`).

## Durable Invariants
- **W5-S5 (`WorkflowType`, `WorkflowActivityType`, `WorkflowActionType`, `Workflow`, `WorkflowActivity`, `WorkflowAction`, `WorkflowLog`, `WorkflowTrigger`, `IWorkflowAction`):**
  - `WorkflowType` defines organization-agnostic state-machine workflows with typed default attributes, ordered `WorkflowActivityType` / `WorkflowActionType` steps, configurable logging levels, and bounded processing attempt/step limits to prevent infinite control-flow loops.
  - `Workflow` enforces deterministic lifecycle transitions (`Pending`, `Active`, `WaitingForInput`, `Suspended`, `Completed`, `Faulted`, `Cancelled`), persisted `jsonb` workflow and activity attributes, structured `WorkflowLog` audit trails, and background queue eligibility via `NextScheduledProcessAtUtc`.
  - `WorkflowTrigger` and `WorkflowTriggerEvaluator` evaluate integration event and entity automation predicates before launching matching `WorkflowType` instances; `WorkflowPersonMergedConsumer` idempotently repoints `PersonId` references across initiators, activity assignees, and logs.
- **W5-S6 (`FormBuilderTemplate`, `WorkflowActionForm`, `WorkflowActionFormSection`, `WorkflowActionFormField`, `ConditionalVisibilityRule`, `FormVisibilityEvaluator`):**
  - `FormVisibilityEvaluator` evaluates `All`/`Any` conditional visibility rules dynamically over merged workflow and draft form values, enforcing required-field validation strictly on currently visible, non-read-only fields and routing outcome buttons to activity completion, target activity activation, or cancellation.

## Verification
```bash
dotnet build src/Modules/Workflow/Ruth.Modules.Workflow/Ruth.Modules.Workflow.csproj -warnaserror
dotnet test tests/Ruth.Modules.Workflow.Tests/Ruth.Modules.Workflow.Tests.csproj
```
