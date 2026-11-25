# Ruth

Modern open-source Relationship, Community & Operations Platform for member-driven, community, non-profit, and service organizations—built on **.NET 10**, **Fluent UI Blazor v5**, **Domain-Driven Design (DDD)**, **PostgreSQL 18** (`postgis`, `pgvector`, `age`), **Redis 8**, **RustFS (S3)**, and **LiveKit (WebRTC)**—featuring unified CRM, check-in & smart Apple/Google Wallet passes, dual charitable/commercial finance, service & facility bookings, and embedded virtual rooms.

## Documentation

- [AGENTS.md](AGENTS.md) — Repository governance, verification commands, and tool ownership contract.
- [docs/IMPLEMENTATION_PLAN.md](docs/IMPLEMENTATION_PLAN.md) — Complete Wave & Vertical Slice architecture and feature roadmap.

## Quick Start (Local Infrastructure)

```bash
cp .env.example .env
docker compose up -d
dotnet tool restore
dotnet build Ruth.slnx -warnaserror
dotnet test Ruth.slnx --no-build
```
