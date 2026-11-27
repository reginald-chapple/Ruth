# AGENTS.md — `Communication` Module

## Purpose & Boundaries
- Owns the `comms` PostgreSQL schema across two projects: `Ruth.Modules.Communication.Contracts` and `Ruth.Modules.Communication`.
- `Ruth.Modules.Communication.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, or Redis (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Communication.Contracts`.

## Durable Invariants
- Wave 3 (`W3-S1`) establishes native `.NET 10` Razor component rendering (`IRazorTemplateRenderer` / `IRazorTemplateRegistry` backed by Blazor's `HtmlRenderer`), the pure-C# safe declarative merge-token engine (`IMergeTemplateEngine`), and persisted reusable `TemplateSnippet` definitions (`comms.template_snippets`).
- Never introduce Lava/Liquid engines or arbitrary runtime C# string compilation (`RazorLight` / Roslyn eval) for database-stored templates; all dynamic template rendering uses compiled `.razor` components plus safe declarative merge tokens over read-only DTO contexts.
- `IMergeTemplateEngine` enforces strict template length, output length, token count, conditional nesting depth, and snippet recursion/cycle limits, and HTML-encodes all interpolated context values in `Html` mode.
- System `TemplateSnippet` definitions (`IsSystem == true`) cannot be deleted.
- **W3-S5 (`Transports`, `SystemCommunications`, `SystemPhoneNumbers`, `PersonalDevices`):**
  - `ICommunicationTransport` in `Ruth.Modules.Communication.Contracts` decouples channel delivery (`Email`, `Sms`, `Push`) from external gateways (`Ruth.Providers.CommunicationTransports`: RFC 5321 `smtp`, `sendgrid`, `mailgun`, `twilio-sms`) while `CommunicationTransportResolver` falls back cleanly to `dev-in-memory-email`, `dev-in-memory-sms`, and `push-stub` (`InMemoryCommunicationSink`) when external credentials are unconfigured.
  - `SystemCommunication` (`comms.system_communications`) renders transactional notifications using `IMergeTemplateEngine` + optional `IRazorTemplateRenderer` layouts, protects built-in system templates (`IsSystem == true`) from deletion, and records immutable `SystemNotificationLog` audit entries.
  - `SystemPhoneNumber` enforces strict E.164 normalization (`^\+[1-9][0-9]{6,14}$`) and blocks deletion while referenced by any `SystemCommunication`.
  - `PersonalDevice` deduplicates push registration tokens idempotently, honors opt-in/active flags during push dispatch, and `CommunicationPersonMergedConsumer` repoints `PersonalDevice`, `SystemPhoneNumber`, and `SystemNotificationLog` references on `PersonMergedIntegrationEvent`.
- **W3-S6 (`BulkCommunications`, `EmailSectionComposer`, `Preferences`, `TwoWaySms`):**
  - `CommunicationTemplate` and `EmailSectionHtmlComposer` compile structured email sections (`HeroBanner`, `RichText`, `CallToActionButton`, `TwoColumnHighlight`, `Divider`) with `IMergeTemplateEngine` and optional `IRazorTemplateRenderer` layouts while protecting built-in system templates (`community-member-digest`) from deletion.
  - `Communication` enforces explicit approval state transitions (`Draft -> PendingApproval -> Approved -> Sending -> Sent` / `Cancelled`), deduplicates normalized `CommunicationRecipient` destinations, dispatches batches via `BulkCommunicationDispatchJob` (`IScheduledJob`), injects RFC 8058 `List-Unsubscribe` / `List-Unsubscribe-Post` headers signed by `PreferenceTokenSigner` (HMAC-SHA256), and processes bounce/open/click/complaint webhooks (`CommunicationWebhookSignatureVerifier`) auto-suppressing hard bounces and spam complaints in `CommunicationSubscriptionPreference`.
  - `CommunicationResponse` and `SmsPipeline` / `SmsAction` power the two-way SMS Conversations inbox (`SmsKeywordMatcher` supporting `ExactKeyword`, `StartsWithKeyword`, `ContainsKeyword`, `RegexPattern` with 250ms timeout) and CTIA `STOP`/`UNSUBSCRIBE`/`START` opt-out/opt-in compliance.
- **W3-S7 (`Flows`, `DripAdvancementJob`, `FlowAnalytics`, `AttendanceConversions`):**
  - `CommunicationFlow` validates multi-step timed drip sequences (`CommunicationFlowStep`, `DelayMinutes`, `SendOnlyIfPreviousUnopened`), protects built-in system flows (`new-member-onboarding-drip`), and configures exit conditions (`RunAllSteps`, `ExitOnConversion`, `ExitOnConversionOrClick`, `ExitOnAnyEngagement`) and conversion goals (`AttendedSession`, `JoinedTargetGroup`, `ClickedGoalUrl`, `ManualOrExternalGoal`).
  - `CommunicationFlowInstance` and `CommunicationFlowInstanceRecipient` track cohort and rolling-enrollment progression via `CommunicationFlowAdvancementJob`, record `CommunicationFlowConversion` funnel attribution (including automatic attendance conversions via `FlowAttendanceConversionEvaluator` on `PersonAttendedIntegrationEvent`), and repoint `PersonId` references on `PersonMergedIntegrationEvent`.
- **Multi-Tenant RLS & `Live` Outbound Gating (`W7-S5`):** Every table in `comms` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), `InMemoryCommunicationSink` and `PreferenceTokenSigner` isolate captured messages and signed unsubscribe tokens strictly per `TenantId`, and outbound bulk email/SMS approval/dispatch, drip `CommunicationFlow` step delivery, outbound two-way SMS replies, and live external gateway resolution (`CommunicationTransportResolver`) require `ITenantContext.IsLive` (`TenantLifecycleStage.Live`).

## Verification
```bash
dotnet build src/Modules/Communication/Ruth.Modules.Communication/Ruth.Modules.Communication.csproj -warnaserror
dotnet test tests/Ruth.Modules.Communication.Tests/Ruth.Modules.Communication.Tests.csproj
```
