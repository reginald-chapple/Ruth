# Ruth

Modernized open-source Church Management System (ChMS) built on **.NET 10**, **Fluent UI Blazor v5**, **Domain-Driven Design (DDD)**, **PostgreSQL 18** (`postgis`, `pgvector`, `age`), **Redis 8**, and **RustFS (S3)**.

## Documentation

- [AGENTS.md](AGENTS.md) — Repository governance, verification commands, and tool ownership contract.
- [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) — Complete Wave & Vertical Slice architecture and porting roadmap.

## Quick Start (Local Infrastructure)

```bash
cp .env.example .env
docker compose up -d
dotnet tool restore
dotnet build Ruth.slnx -warnaserror
dotnet test Ruth.slnx --no-build
```
