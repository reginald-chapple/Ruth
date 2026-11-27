# AGENTS.md — `Finance` Module

## Purpose & Boundaries
- Owns the `finance` PostgreSQL schema across two projects: `Ruth.Modules.Finance.Contracts` and `Ruth.Modules.Finance`.
- `Ruth.Modules.Finance.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- Other modules may reference only `Ruth.Modules.Finance.Contracts`. `Ruth.Modules.Finance` may reference only `Ruth.SharedKernel`, `Ruth.Persistence`, and other modules' `*.Contracts` assemblies (`Core.Contracts`, `Crm.Contracts`, `Identity.Contracts`).

## Durable Invariants
- **W4-S1 (`Accounts`, `Money`, `Organizations`, `Pledges`):**
  - `Money` enforces valid 3-letter ISO-4217 currency codes (`IsoCurrencyCatalog`) and exact minor-unit decimal scale without sub-minor rounding drift.
  - `FinancialAccount` models organization-neutral accounts (`CharitableFund`, `MembershipDues`, `ProgramFee`, `GrantFund`, `OperatingRevenue`) with explicit per-account `IsTaxDeductible` flags, optional parent hierarchy (cycle-checked), GL/cost-center codes, and date window enforcement (`EndDate >= StartDate`).
  - Organization/Sponsor management operates as a clean facade over `Crm.Contracts` (`PersonRecordType.Business`) enriched with Finance pledge, invoice, and transaction totals.
  - `FinancialPledge` enforces positive `TotalAmount`, `EndDate >= StartDate`, pro-rata expected fulfillment schedules, and `PersonMergedIntegrationEvent` person-ID repointing.
- **W4-S2 (`Gateways`, `SavedPaymentMethods`, `BankAccountIdentifiers`):**
  - `SavedPaymentMethod` stores only opaque gateway customer vault tokens and masked last-4 suffixes, rejecting raw PAN digit sequences.
  - `BankAccountIdentifier` validates 9-digit ABA routing checksums and persists only keyed HMAC-SHA256 digests (`RoutingAndAccountHash`) plus masked last-4 suffixes for O(1) check/ACH constituent matching.
- **W4-S3 (`Batches`, `Invoices`, `Transactions`, `CheckImages`, `Refunds`):**
  - `Invoice` tracks `Draft -> Open -> PartiallyPaid -> Paid` (and `Voided` when unpaid), enforcing `PaidAmount <= SubtotalAmount` and reversible payment allocations on refund.
  - `FinancialBatch` enforces `Pending -> Open -> Closed` transitions, requiring zero variance against `ControlAmount` (and `ControlItemCount` when non-zero) before closing, and audit-locking closed batches against mutation.
  - `FinancialTransaction` enforces positive multi-account splits summing exactly to `TotalAmount`, snapshots `IsTaxDeductible` per split from the target `FinancialAccount`, encrypts check/remittance images with AES-256-GCM before `IStorageProvider` upload, and bounds cumulative refunds to `TotalAmount`.
- **W4-S4 (`RecurringSchedules`, `SettlementSync`, `CadenceAlerts`):**
  - `RecurringScheduleTransaction` supports recurring charitable gifts, membership subscriptions, and retainers with month-end-safe schedule advancement and automatic `PastDue` escalation after consecutive gateway failures.
  - `TransactionCadenceAnalyzer` detects constituent lapse/churn risk and unusual amount spikes from historical settled transaction series with cooldown deduplication.
- **W4-S5 (`Statements`, `TaxReceipts`, `Assistance`):**
  - `FinancialStatementTemplate` and `StatementLedgerAggregator` strictly separate **Tax-Deductible Charitable Receipts** (`IsTaxDeductible == true`) from **Non-Deductible Dues/Fee/Invoice Statements** (`IsTaxDeductible == false`), pro-rating partial refunds across splits so refunded gifts are never overstated on tax receipts, and rendering both HTML5 print-CSS layouts (`IRazorTemplateRenderer` + `IMergeTemplateEngine`) and standards-compliant PDF 1.4 documents.
  - `AssistanceProgram` and `AssistanceCase` enforce verified-document prerequisites, per-case award ceilings, program budget limits, and optional `InternalFeeWaiverCredit` direct settlement against open `Invoice`s.
- **W4-S6 (`Campaigns`, `LeaderToolbox`, `PublicContributionPortal`):**
  - `FundraisingCampaign` pairs a `Groups` Group with a `FinancialAccount`, enforcing optional team and participant goal overrides, net-settled transaction progress calculation, and donor privacy modes (`PublicNameAndAmount`, `PublicNameHideAmount`, `AnonymousPublicly`, `PrivateStaffOnly`) across public portals vs. authorized `CampaignLeaderToolbox` views.
- **Multi-Tenant RLS & `Live` Payment Gating (`W7-S6`):** Every table in `finance` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), check/remittance images store under `tenants/{TenantId}/...`, and live payment gateway capture requires `ITenantContext.IsLive`.

## Verification
```bash
dotnet build src/Modules/Finance/Ruth.Modules.Finance/Ruth.Modules.Finance.csproj -warnaserror
dotnet test tests/Ruth.Modules.Finance.Tests/Ruth.Modules.Finance.Tests.csproj
```
