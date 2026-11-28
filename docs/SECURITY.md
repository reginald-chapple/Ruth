# Ruth Security Architecture, Verifiable Controls Matrix & DR Runbook

`Ruth` is a multi-tenant white-label relationship, community, and operations SaaS platform built exclusively for **IRS-recognized tax-exempt organizations**. Every security boundary—from database row-level isolation and per-tenant envelope cryptography to outbound SSRF socket guards, stealth white-label headers, hardened chiseled containers, and supply-chain CI gates—is enforced in code and continuously verified by automated test suites.

---

## 1. SaaS Isolation & Verifiable Controls Matrix

| Domain | Control Requirement | Implementation Surface | Automated Verification Suite |
| :--- | :--- | :--- | :--- |
| **PostgreSQL 18 RLS & Dual Roles** | Every table in all 15 tenant schemas enforces `ENABLE` + `FORCE ROW LEVEL SECURITY` (`PERMISSIVE` + `RESTRICTIVE` `tenant_id` policies) under non-superuser, non-`BYPASSRLS` role `ruth_app`; EF Core Global Query Filters + composite `(tenant_id, ...)` unique indexes run in-process. | `src/Ruth.Persistence/EntityFramework/ModuleDbContext.cs`, `src/Ruth.Persistence/EntityFramework/TenantRlsConnectionInterceptor.cs`, `src/Ruth.Persistence/EntityFramework/PostgresRlsRoleProvisioner.cs`, `src/Ruth.Persistence/EntityFramework/TenantRlsMigrationExtensions.cs` | `tests/Ruth.ArchitectureTests/TenantIsolationArchitectureTests.cs`, `tests/Ruth.Host.IntegrationTests/CrossTenantRlsCanaryIntegrationTests.cs`, `tests/Ruth.Persistence.Tests/` |
| **Control-Plane & Lifecycle Gating** | Platform entities (`Tenant`, `TenantDomain`, `IrsExemptVerification`, `StripeConnectAccount`, `PlatformOperator`) reside solely in `platform` schema (`42501` denied to `ruth_app`); `Sandbox` workspaces cannot execute live payment capture or outbound bulk messaging until `Live` approval. | `src/Modules/Tenancy/Ruth.Modules.Tenancy/Infrastructure/TenancyDbContext.cs`, `src/Ruth.SharedKernel/Tenancy/AmbientTenantContext.cs` | `tests/Ruth.Modules.Tenancy.Tests/`, `tests/Ruth.Host.IntegrationTests/W7S8SubsystemIsolationCanaryTests.cs` |
| **Stealth White-Label Routing, Cookies & Circuits** | Unknown hosts return zero-byte `404`; server/framework headers (`Server`, `X-Powered-By`, `X-AspNet*`) are stripped; zero `"Powered by Ruth"` public branding; HTTPS cookies upgrade to RFC 6265bis `__Host-sid` / `__Host-csrf`; Blazor circuits & SignalR hubs bind immutably to a single `TenantId`. | `src/Ruth.Host/Middleware/TenantResolutionMiddleware.cs`, `src/Ruth.Host/Middleware/StealthResponseHeadersMiddleware.cs`, `src/Ruth.Host/Middleware/TenantCircuitHandler.cs`, `src/Ruth.Host/Middleware/TenantSignalRHubFilter.cs` | `tests/Ruth.Host.IntegrationTests/W7S7StealthCookieAndCircuitIsolationTests.cs` |
| **Outbound SSRF & Inbound Webhook Integrity** | `SafeOutboundHttpHandler` resolves DNS per hop (`AllowAutoRedirect = false`) and blocks loopback, RFC1918, CGNAT, link-local `169.254.169.254` cloud metadata, IPv6 unique-local/mapped ranges, and internal service names (`postgres`, `redis`, `rustfs`). Webhooks enforce constant-time HMAC-SHA256 + 5-minute replay windows + `WebhookReplayDeduplicator`. | `src/Ruth.SharedKernel/Security/OutboundAddressGuard.cs`, `src/Ruth.SharedKernel/Security/SafeOutboundHttpHandler.cs`, `src/Ruth.SharedKernel/Security/WebhookReplayDeduplicator.cs` | `tests/Ruth.SharedKernel.Tests/Security/W8S1SafeOutboundAndWebhookSecurityTests.cs`, `tests/Ruth.Host.IntegrationTests/W8S1OutboundSsrfAndWebhookIntegrationTests.cs` |
| **HTTP Security Headers, Proxy CIDR & Rate Limiting** | Per-request 128-bit nonce `Content-Security-Policy`, `Permissions-Policy`, `Cross-Origin-Opener-Policy`, explicit `ForwardedHeaders` CIDR trust boundary, `/openapi` disabled in `Production`, and chained per-IP + per-tenant rate limiters (`429 ProblemDetails` + `Retry-After`). | `src/Ruth.Host/Middleware/StealthResponseHeadersMiddleware.cs`, `src/Ruth.Host/Middleware/ProxyAndHostGuardMiddleware.cs`, `src/Ruth.Host/DependencyInjection/HostRateLimitingExtensions.cs` | `tests/Ruth.Host.IntegrationTests/W8S2HeadersProxyAndRateLimitingIntegrationTests.cs` |
| **JWT/OIDC Pinning, Token Rotation & BOLA/IDOR** | Strict `HS256` JWT pinning (`alg: none`, `jku`/`x5u`/`jwk`/`x5c` rejected) + `tid` binding; refresh-token reuse revokes the active token family; OIDC PKCE `S256` + `state`/`nonce` + exact ordinal `redirect_uri`; salted PBKDF2-HMAC-SHA256 `RestApiKey` hashes; 100% explicit endpoint authorization metadata. | `src/Modules/Identity/Ruth.Modules.Identity/Infrastructure/Security/JwtBearerSecurityEvents.cs`, `src/Ruth.SharedKernel/Security/SafeRedirectValidator.cs` | `tests/Ruth.Host.IntegrationTests/W8S3AuthJwtOidcAndBolaIntegrationTests.cs`, `tests/Ruth.ArchitectureTests/EndpointAuthorizationArchitectureTests.cs` |
| **Per-Tenant Envelope Cryptography at Rest** | Database-stored third-party credentials (`ExternalAuthProviderConfig.ClientSecret`, `FinancialGateway.Settings`, `CommunicationTransportConfig.Credentials`, `MediaAccount.Settings`) and `AesGcmCheckImageVault` check images use random 256-bit DEKs + 96-bit nonces under `AES-256-GCM` + `HKDF-SHA256` with `TenantId` and field purpose bound into AAD (`$ruth-env-v1$...`). | `src/Ruth.SharedKernel/Security/AesGcmTenantCredentialProtector.cs`, `src/Ruth.Persistence/EntityFramework/TenantEncryptedValueConverters.cs`, `src/Modules/Finance/Ruth.Modules.Finance/Infrastructure/CheckImages/AesGcmCheckImageVault.cs` | `tests/Ruth.SharedKernel.Tests/Security/W8S4EnvelopeCryptoAndRedactorTests.cs`, `tests/Ruth.Modules.Finance.Tests/W8S4CheckImageTenantAadTests.cs`, `tests/Ruth.Host.IntegrationTests/W8S4CryptoStorageAndSanitizationIntegrationTests.cs` |
| **S3/RustFS Isolation, Magic-Byte & Polyglot Defense** | Mandatory `tenants/{TenantId}/...` keys, `x-amz-acl: private` + `PublicAccessBlock`, presigned URLs clamped to `<= 15m` (`X-Amz-Expires=900`) with `response-content-disposition=attachment`, magic-byte MIME verification, SVG/HTML/executable/polyglot blocking, path-traversal guards, and `Content-Security-Policy: default-src 'none'; sandbox` on downloads. | `src/Ruth.SharedKernel/Security/FileContentSecurityInspector.cs`, `src/Providers/Ruth.Providers.RustFsStorage/RustFsStorageProvider.cs`, `src/Modules/Core/Ruth.Modules.Core/Slices/Files/BinaryFileContentStreamer.cs` | `tests/Ruth.SharedKernel.Tests/Security/W8S4UploadSanitizerAndCrlfTests.cs`, `tests/Ruth.Host.IntegrationTests/W8S4CryptoStorageAndSanitizationIntegrationTests.cs` |
| **HTML/XSS, Merge-Token & Email CRLF Sanitization** | Strict tag/attribute/URL/CSS allowlist (`AllowlistHtmlSanitizer`), `SearchSnippetSanitizer` balanced `<mark>` escaping, `SafeMergeTemplateEngine` HTML/URL encoding, and `EmailHeaderSanitizer` CR/LF & SMTP injection blocking across `Cms`, `Search`, and all email transports (`Smtp`, `SendGrid`, `Mailgun`, `DevInMemory`). | `src/Ruth.SharedKernel/Security/AllowlistHtmlSanitizer.cs`, `src/Ruth.SharedKernel/Security/EmailHeaderSanitizer.cs`, `src/Modules/Search/Ruth.Modules.Search/Domain/Querying/SearchSnippetSanitizer.cs` | `tests/Ruth.Modules.Search.Tests/Domain/W8S4SearchSnippetAndUrlSanitizerTests.cs`, `tests/Ruth.Host.IntegrationTests/W8S4CryptoStorageAndSanitizationIntegrationTests.cs` |
| **Hardened Containers, Supply-Chain CI & CycloneDX SBOM** | Ubuntu 24.04 chiseled non-root (`1654`) runtime image, SHA256-pinned base images, `read_only: true` + `cap_drop: ["ALL"]` + `no-new-privileges:true`, `127.0.0.1` local bindings, `internal: true` backing network, authenticated Redis 8, `permissions: contents: read`, 40-char SHA-pinned GitHub Actions, `gitleaks`, `Trivy`, `dotnet list package --vulnerable` + `npm audit`, and deterministic CycloneDX 1.6 SBOM. | `deploy/docker/dotnet/Dockerfile`, `docker-compose.yml`, `.github/workflows/ci.yml`, `.gitleaks.toml`, `scripts/generate-sbom.mjs` | `tests/Ruth.ArchitectureTests/W8S5ContainerCiAndSupplyChainVerificationTests.cs` |

---

## 2. Cryptographic Architecture & Zero-Downtime KEK Rotation

### 2.1 Per-Tenant Envelope Structure (`$ruth-env-v1$`)
Every secret persisted through `TenantEncryptedValueConverters` is sealed as:

```text
$ruth-env-v1$<keyId>$<base64url(wrapNonce[12] || wrapTag[16] || wrappedDek[32])>$<base64url(dataNonce[12] || dataTag[16] || ciphertext)>
```

1. **Per-Record Random DEK & Nonces:** Each encryption operation generates a fresh CSPRNG 32-byte Data Encryption Key (DEK) and two independent 12-byte nonces (`RandomNumberGenerator`).
2. **HKDF-SHA256 Tenant Key Derivation:** The Key Encryption Key (KEK) identified by `ActiveKeyId` derives a tenant-and-purpose-specific wrapping key via RFC 5869 `HKDF-SHA256` using the 16-byte `TenantId` UUID as salt and `ruth:kek-wrap:v1|<keyId>|<purpose>` as context info.
3. **Dual-Layer AAD Binding:** Both DEK wrapping (`ruth:dek-aad:v1|tenant:<TenantId>|key:<keyId>|purpose:<purpose>`) and payload encryption (`ruth:data-aad:v1|tenant:<TenantId>|key:<keyId>|purpose:<purpose>`) bind `TenantId`, `keyId`, and column/setting `purpose` into `AesGcm` Associated Authenticated Data, guaranteeing deterministic `AuthenticationTagMismatchException` if ciphertext is copied across tenants or columns.

### 2.2 Zero-Downtime KEK Rotation Procedure
1. Generate a new 256-bit (32+ byte) random master key (`openssl rand -base64 48`).
2. Move the current `Security__CredentialEncryption__ActiveKeyId` (e.g., `k2026-q1`) and its secret into `Security__CredentialEncryption__PreviousKeys__k2026-q1`.
3. Set `Security__CredentialEncryption__ActiveKeyId=k2026-q2` and `Security__CredentialEncryption__MasterKey=<new-secret>`, then perform a rolling restart of `ruth-host`.
4. Existing `$ruth-env-v1$k2026-q1$...` rows continue decrypting seamlessly via `PreviousKeys` while all new or updated credentials seal under `k2026-q2`.
5. Once all rows are re-saved under `k2026-q2` (verify zero `%$ruth-env-v1$k2026-q1$%` values remain across `identity.external_auth_providers`, `finance.financial_gateways`, `comms.communication_transport_configs`, and `cms.media_accounts`), remove `PreviousKeys__k2026-q1` from secret storage.

---

## 3. Immutable 3-2-1-1-0 Backup & Disaster Recovery Runbook (`restic` + S3 Object Lock)

`Ruth` follows a **3-2-1-1-0** ransomware-resilient backup posture:
- **3** copies of all platform & tenant data (primary cluster, local encrypted snapshot staging, off-site object vault)
- **2** distinct storage media/engines (NVMe PostgreSQL/RustFS volumes + remote S3 object storage)
- **1** off-site replica in a separate cloud account/region with **zero** shared IAM admin principals
- **1** **immutable WORM** copy enforced by **S3 Object Lock (`COMPLIANCE` mode, >= 30-day retention)**
- **0** unverified backups (automated `restic check --read-data-subset=10%` + scheduled clean-room restore drills)

### 3.1 Recovery Objectives
- **RPO (Recovery Point Objective):** `<= 15 minutes` (continuous PostgreSQL 18 WAL archiving + hourly incremental `restic` snapshots)
- **RTO (Recovery Time Objective):** `<= 60 minutes` (clean container + PostgreSQL 18 + RustFS restoration)

### 3.2 Provisioning the Immutable S3 Object Lock Vault
Provision a dedicated off-site S3-compatible bucket with Object Lock enabled at creation time and default `COMPLIANCE` retention:

```bash
aws s3api create-bucket \
  --bucket ruth-dr-immutable-vault \
  --region us-east-2 \
  --create-bucket-configuration LocationConstraint=us-east-2 \
  --object-lock-enabled-for-bucket

aws s3api put-public-access-block \
  --bucket ruth-dr-immutable-vault \
  --public-access-block-configuration \
  "BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true"

aws s3api put-object-lock-configuration \
  --bucket ruth-dr-immutable-vault \
  --object-lock-configuration '{
    "ObjectLockEnabled": "Enabled",
    "Rule": { "DefaultRetention": { "Mode": "COMPLIANCE", "Days": 30 } }
  }'

export RESTIC_REPOSITORY="s3:s3.us-east-2.amazonaws.com/ruth-dr-immutable-vault"
export RESTIC_PASSWORD_FILE="/etc/ruth-backup/restic-repo-passphrase"
restic init
```

### 3.3 Automated Consistent Snapshot & Integrity Verification
Run on an isolated backup host/systemd timer using least-privilege S3 credentials (`s3:ListBucket`, `s3:GetObject`, `s3:PutObject` — **never** `s3:DeleteObject` or `s3:BypassGovernanceRetention` on the backup writer role):

```bash
#!/usr/bin/env bash
set -euo pipefail

STAGE_DIR="$(mktemp -d /var/backups/ruth-stage-XXXXXX)"
trap 'rm -rf "$STAGE_DIR"' EXIT

# 1. Consistent PostgreSQL 18 logical dump (all 16 schemas + roles/RLS policies)
pg_dump --format=custom --compress=zstd:6 \
  --file="$STAGE_DIR/ruth-postgres.dump" \
  "$RUTH_BACKUP_PG_DSN"

# 2. Sync encrypted check images & tenant binary objects from RustFS
rclone sync rustfs:ruth "$STAGE_DIR/rustfs-bucket" --checksum --transfers=8

# 3. Commit encrypted, deduplicated snapshot to immutable Object-Locked repository
restic backup "$STAGE_DIR" \
  --tag "ruth-prod" \
  --tag "pg18-rustfs" \
  --host "ruth-primary"

# 4. Cryptographic repository structure + 10% pack-data verification (0-error gate)
restic check --read-data-subset=10%

# 5. Prune metadata ONLY after Object Lock 30-day compliance window expires
restic forget \
  --keep-hourly 24 \
  --keep-daily 30 \
  --keep-weekly 12 \
  --keep-monthly 12 \
  --prune
```

### 3.4 Clean-Room Disaster Recovery & Verification Drill
Execute monthly against an isolated recovery environment to maintain **0 unverified backups**:

```bash
#!/usr/bin/env bash
set -euo pipefail

RESTORE_DIR="$(mktemp -d /var/tmp/ruth-dr-drill-XXXXXX)"
trap 'rm -rf "$RESTORE_DIR"' EXIT

# 1. Restore latest immutable snapshot
restic restore latest --tag "ruth-prod" --target "$RESTORE_DIR"

# 2. Restore PostgreSQL 18 schemas, data, and RLS policies
DUMP_FILE="$(find "$RESTORE_DIR" -name 'ruth-postgres.dump' -print -quit)"
pg_restore --clean --if-exists --no-owner \
  --dbname="$RUTH_DR_PG_DSN" \
  "$DUMP_FILE"

# 3. Verify FORCE ROW LEVEL SECURITY across all 15 tenant schemas (must return 0 unprotected tables)
UNPROTECTED="$(psql "$RUTH_DR_PG_DSN" -Atc "
  SELECT COUNT(*)
  FROM pg_class c
  JOIN pg_namespace n ON n.oid = c.relnamespace
  WHERE c.relkind = 'r'
    AND n.nspname IN ('identity','core','crm','groups','checkin','comms','finance','events','commerce','engage','workflow','cms','reporting','lms','search')
    AND c.relname <> '__EFMigrationsHistory'
    AND (NOT c.relrowsecurity OR NOT c.relforcerowsecurity);
")"
test "$UNPROTECTED" = "0"

# 4. Restore RustFS objects and probe application readiness
RESTORED_BUCKET_DIR="$(find "$RESTORE_DIR" -type d -name 'rustfs-bucket' -print -quit)"
rclone sync "$RESTORED_BUCKET_DIR" rustfs-dr:ruth --checksum
dotnet /app/Ruth.Host.dll --healthcheck
```
