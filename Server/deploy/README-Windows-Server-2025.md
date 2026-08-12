# Bee Kingdom Server - Windows Server 2025 Deployment

## Goal

Publish and run the Bee Kingdom ASP.NET Core server on Windows Server 2025.

## Current Runtime

Main project:

`Server\src\BeeKingdom.Server\BeeKingdom.Server.csproj`

Current runtime target:

- .NET 8
- Windows Server 2025 target profile
- IIS-compatible ASP.NET Core app
- SQL Server configuration prepared in `appsettings.json`

## Local Publish

From the repository root:

```powershell
.\Server\deploy\Publish-BeeKingdomServer.ps1
```

The default package is written to:

`Server\artifacts\BeeKingdom.Server`

## Local Smoke Run

```powershell
dotnet .\Server\artifacts\BeeKingdom.Server\BeeKingdom.Server.dll
```

Then verify:

```powershell
Invoke-RestMethod http://localhost:5000/health
```

If another port is configured by IIS or `ASPNETCORE_URLS`, use that URL instead.

Or run the reusable smoke test:

```powershell
.\Server\deploy\Test-BeeKingdomServer.ps1 -BaseUrl http://127.0.0.1:5088
```

The smoke test verifies:

- `GET /health`
- `POST /protocol/ping`

## Windows Server 2025 Environment

Use `ASPNETCORE_ENVIRONMENT=Production` for the Windows Server 2025 profile.

Production configuration keeps secrets out of source control. Override connection strings and sensitive values with environment variables or machine-level configuration, for example:

```powershell
$env:ConnectionStrings__BeeKingdomDb = "Server=<sql-host>;Database=BeeKingdom;Trusted_Connection=True;TrustServerCertificate=True;"
$env:ASPNETCORE_URLS = "http://127.0.0.1:5088"
```

The committed production profile documents hosting, diagnostics, and SQL Server role only. It does not make SQL persistence production-ready by itself.

## Hosting Decision

IIS remains the priority hosting path for SERVER-018 because it matches the Windows Server 2025 deployment target and ASP.NET Core hosting model already exposed by `/health`.

Windows Service remains a later option for worker-style runtime needs, but it is not selected for this cycle.

## HTTPS and IIS Baseline

For the first Windows Server 2025 deployment, use IIS with the ASP.NET Core Hosting Bundle installed on the server.

Minimum IIS shape:

- one dedicated IIS site or application for Bee Kingdom;
- application pool set to `No Managed Code`;
- app pool identity with read access to the published package;
- `ASPNETCORE_ENVIRONMENT=Production`;
- `ASPNETCORE_URLS` only when self-hosting outside IIS;
- HTTPS binding on port 443 with a certificate managed outside the repository;
- HTTP to HTTPS redirect handled by IIS or reverse proxy policy;
- connection strings injected with environment variables or machine-level configuration.

Certificate and secrets must not be committed to this repository.

## Rollback

Before replacing the active package, keep a copy of the previous known-good package directory.

Example rollback validation:

```powershell
.\Server\deploy\Rollback-BeeKingdomServer.ps1 `
  -BackupPath C:\BeeKingdom\backups\BeeKingdom.Server-previous `
  -TargetPath C:\projets\beekingdomgame-master\Server\artifacts\BeeKingdom.Server `
  -WhatIf
```

Example rollback execution:

```powershell
.\Server\deploy\Rollback-BeeKingdomServer.ps1 `
  -BackupPath C:\BeeKingdom\backups\BeeKingdom.Server-previous `
  -TargetPath C:\projets\beekingdomgame-master\Server\artifacts\BeeKingdom.Server
```

After rollback, run:

```powershell
.\Server\deploy\Test-BeeKingdomServer.ps1 -BaseUrl https://<bee-server-host>
```

## First Server Responsibilities

The first runtime layer must prove:

- server starts;
- `/health` responds;
- `/protocol/ping` responds;
- auth/account/colony endpoints are reachable;
- tests pass;
- configuration is explicit;
- production limitations are documented.

## Not Production Ready Yet

The current server is a runtime foundation, not the final MMO backend.

Do not claim final support yet for:

- live PvP;
- live chat;
- persistent alliances;
- official economy;
- official combat;
- matchmaking;
- rankings;
- monetization;
- final progression.

## Next Hardening Steps

1. Implement SQL-backed repositories for accounts, sessions, colonies, and colony snapshots.
2. Add a migration execution mode against a real SQL Server instance.
3. Add deployment logs and IIS operational runbooks.
4. Add HTTPS certificate renewal checks.
