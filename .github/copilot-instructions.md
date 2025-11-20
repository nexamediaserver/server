# Nexa Media Server – Copilot Instructions

## Architecture Snapshot

- Solution ties together API (`src/NexaMediaServer.API`), domain contracts (`.Core`), storage/services (`.Infrastructure`), shared helpers (`.Common`), and tests under `tests/`.
- `src/NexaMediaServer.API/Program.cs` bootstraps Serilog, OpenTelemetry, ASP.NET Identity, GraphQL Server, Hangfire, and static SPA hosting; register new services via `AddCoreServices` / `AddInfrastructureServices` instead of editing Program directly.
- Domain abstractions live in `.Core` (e.g., `ILibrarySectionService`, `IMetadataService`) and are implemented in `.Infrastructure`; keep APIs async-friendly and DI-ready.

## Persistence & Startup

- EF Core uses pooled SQLite contexts configured in `Infrastructure/DependencyInjection.cs`; files live under `IApplicationPaths` (defaults to XDG paths or `/config` in containers).
- `ApplyMigrationsAsync("NEXA_SKIP_MIGRATIONS")` runs migrations unless the env var is set; `ApplicationDbContextInitialiser` seeds roles + the default admin using `NEXA_ADMIN_EMAIL` / `NEXA_ADMIN_PASSWORD` when no users exist.
- Always resolve `MediaServerContext` via `IDbContextFactory<MediaServerContext>` in background tasks (Hangfire jobs, GraphQL subscriptions) to avoid scoped-service leaks.
- Keep migrations, seeding, and directory creation idempotent—API, job hosts, and CLI commands can all start concurrently.

## Background Jobs & Scanning

- Library scans (`LibraryScannerService`) stream `FileScanner` batches through bounded `Channel`s and bulk insert via `EFCore.BulkExtensions.Sqlite`; extend this pipeline instead of re-reading the filesystem.
- Extensibility parts (`PartsRegistry`) discover `IScannerIgnoreRule`, `IItemResolver`, `IMetadataAgent`, `IFileAnalyzer`, and `IImageProvider` at startup; drop new behaviors by implementing these interfaces under `Infrastructure/Services/*`.
- Hangfire queue names are fixed (`scans`, `metadata_agents`, `file_analyzers`, `image_generators`, `trickplay`); annotate jobs with `[Queue]` so dashboards and throttles stay accurate.
- `JobNotificationService` aggregates Hangfire state and pushes `JobNotification` events through `IJobNotificationPublisher` (topic `JOB_NOTIFICATION`); prefer publishing status updates over ad-hoc polling.

## GraphQL & Controllers

- GraphQL schema (`API/Types`) projects EF entities via expressions in `MetadataMappings`; keep resolvers returning `IQueryable` so HotChocolate helpers (`UsePaging`, `UseFiltering`, `UseSorting`) can compose.
- For fields prone to N+1 queries, use DataLoaders in `API/DataLoaders` instead of resolving via `MediaServerContext` inside resolvers.
- REST controllers (`MediaController`, `ImagesController`) require `[Authorize]`, validate file paths/extensions, and should reuse repositories/services rather than touching the filesystem directly.
- `dotnet run --project src/NexaMediaServer.API` hosts GraphQL at `/graphql`, exposes OpenAPI/Scalar + Hangfire dashboards in Development, and serves the SPA under `/web` when `wwwroot/build` exists.

## Media Metadata & Assets

- Metadata processing stages: scanners create skeleton entities, `MetadataService.RefreshMetadataAsync` fans out to agents, `AnalyzeFilesAsync` merges `IFileAnalyzer` output, `ImageService` (NetVips) stores artwork, and trickplay thumbnails come from `BifService`.
- `ImagesController` caches transcodes in `CacheDirectory/images`, emits ETags, and exposes both VTT tracks and JPEG thumbnails; use `ImageTranscodeRequest` to ensure consistent caching keys.
- Title normalization lives in `Common/SortName.cs` with regression tests in `tests/NexaMediaServer.Tests.Unit/SortNameTests.cs`; update tests whenever sort semantics change.
- File-type heuristics live in `Common/MediaFileExtensions.cs` and resolver logic (e.g., `Infrastructure/Services/Resolvers/MovieResolver.cs`); keep scanners/resolvers aligned with these canonical lists.

## Frontend Client

- `src/NexaMediaServer.API/ClientApp` is Vite + React 19 using pnpm 10 and Node ≥24; run `dotnet run --project src/NexaMediaServer.API` for local SPA work as the API backend is required and proxies the frontend client.
- `graphql.config.ts`, `schema.graphql`, and `codegen.ts` drive GraphQL types—run `pnpm codegen` after backend schema changes so hooks stay in sync.
- Lint/tests: `pnpm lint`, `pnpm test` (Vitest), and `pnpm format` must pass before committing client changes.
- The server serves the production build from `src/NexaMediaServer.API/wwwroot/build`; keep build-compatible assets under `ClientApp/public`.

## Build, Tests & Tooling

- Backend uses .NET SDK 10.0.100 (`global.json`); `dotnet build NexaMediaServer.sln` and `dotnet test tests/NexaMediaServer.Tests.Unit/NexaMediaServer.Tests.Unit.csproj` are the baseline commands.
- `Directory.Build.props` turns warnings into errors and enables nullable, analyzers, and XML docs; fix StyleCop/Roslynator issues instead of suppressing them.
- Dependencies are centrally managed in `Directory.Packages.props`; change versions there, not per-project.
- Prefer typed `ILogger` partial methods (existing throughout services) over ad-hoc logging so Serilog + OpenTelemetry enrichment stays consistent.

## Telemetry & Debugging

- `builder.AddTelemetry()` wires ASP.NET, EF Core, HotChocolate, Hangfire, and Prometheus exporters; `app.UseOpenTelemetryPrometheusScrapingEndpoint()` exposes `/metrics` for scraping.
- `ApplicationPaths` (Infrastructure/Services) determines `DataDirectory`, `LogDirectory`, `CacheDirectory`, etc.; always request directories through `IApplicationPaths` so they exist across platforms and containers.
- Development exposes Hangfire dashboard + Scalar docs; for runtime issues also subscribe to `METADATA_ITEM_UPDATED` / `JOB_NOTIFICATION` GraphQL topics to observe live updates.
