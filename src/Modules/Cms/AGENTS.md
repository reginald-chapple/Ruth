# AGENTS.md — `Cms` Module

## Purpose & Boundaries
- Owns the `cms` PostgreSQL schema across two projects: `Ruth.Modules.Cms.Contracts` and `Ruth.Modules.Cms`.
- `Ruth.Modules.Cms.Domain` must remain pure C# with **zero** dependencies on EF Core, ASP.NET Core, Blazor, Npgsql, Redis, or `Ruth.Persistence` (enforced by `Ruth.ArchitectureTests`).
- `Ruth.Modules.Cms.Contracts` may only depend on `Ruth.SharedKernel` (pure C# contracts including `IRuthBlock` and `IMediaAccountSyncProvider`).

## Durable Invariants
- **W6-S1 (`Site`, `SiteDomain`, `Theme`, `Layout`, `Page`, `PageRoute`, `PageContext`, `BlockType`, `Block`, `HtmlContent`, `PageShortLink`, `RequestFilter`):**
  - Multi-host domain routing matches normalized hostnames (`SiteDomain`) and scores `PageRoute` specificity (`static` > `{param}`).
  - `CmsTheme` validates CSS custom property names (`--*`) and sanitizes values before generating `:root` bundles.
  - `HtmlContent` maintains monotonic revisions per block with explicit approval + schedule window (`StartDateTimeUtc`/`ExpireDateTimeUtc`) enforcement.
  - `PageShortLink` enforces `http`/`https` or root-relative URLs, records clicks, appends UTM parameters, and generates clean-room ISO/IEC 18004 QR SVG matrices (`QrCodeSvgGenerator`).
- **W6-S2 (`ContentChannelType`, `ContentChannel`, `ContentTopic`, `ContentChannelItem`, `MediaAccount`/`MediaFolder`/`MediaElement`, `PersonalLinkSection`/`PersonalLink`, `PersistedDataset`):**
  - `ContentChannelItem` enforces scheduled publishing (`Approved` + active window), unique per-channel slugs, parent/child associations, and emits `ContentChannelItemPublishedIntegrationEvent` via Outbox.
  - `ContentChannelFeedGenerator` generates valid RSS 2.0 XML and JSON Feed 1.1 feeds.
  - `IMediaAccountSyncProvider` supports modular Vimeo, YouTube, HLS, and deterministic in-memory sync adapters.
  - `PersistedDataset` materializes cached `jsonb` datasets on schedule (`PersistedDatasetRefreshJob`) or demand.
- **W6-S3 (`InteractionChannel`/`Component`/`Session`/`Interaction`, `PersonalizationSegment`, `AdaptiveMessage`):**
  - High-throughput bounded `Channel<TrackInteractionItemSpec>` (`BufferedInteractionChannel`) drains batches asynchronously and on explicit flush/shutdown.
  - `PersonalizationSegment` snapshots person memberships to PostgreSQL and synchronizes Redis Sets + deterministic 262,144-bit Redis Bitsets (`RedisPersonalizationSegmentCache`) with graceful local fallback.
  - `AdaptiveMessageResolver` selects prioritized segment-matched adaptations honoring active windows and view saturation limits.
  - `CmsPersonMergedConsumer` idempotently repoints `PersonId` across interactions, sessions, segment memberships, bookmarks, channel items, and HTML approvals.
- **W6-S4 (`MobileApplication`/`Layout`/`Page`/`DeepLink`, `SignageApplication`):**
  - Versioned Minimal API contracts (`/api/v1/mobile/apps/{slug}/bootstrap`, `/pages/{pageKey}`, `/deeplinks/resolve`) and Digital Signage / Apple TV / Roku Direct Publisher JSON feeds (`/api/v1/signage/{slug}/feed`, `/apple-tv`, `/roku`).
- **Multi-Tenant & RLS Isolation (`W7-S6`):** Every table in `cms` enforces `ITenantScoped` + PostgreSQL 18 `ENABLE` + `FORCE ROW LEVEL SECURITY` (`ruth_app`), `CmsTenantSeedContributor` seeds system block types per tenant, and `RedisPersonalizationSegmentCache` partitions segment keys under `t:{TenantId}:...`.

## Verification
```bash
dotnet build src/Modules/Cms/Ruth.Modules.Cms/Ruth.Modules.Cms.csproj -warnaserror
dotnet test tests/Ruth.Modules.Cms.Tests/Ruth.Modules.Cms.Tests.csproj
```
