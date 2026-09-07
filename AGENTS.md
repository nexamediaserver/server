# Nexa Agent Guide

## Repository orientation

- `cmd/nexa/` contains the Go executable entrypoint.
- `internal/` contains server implementation packages; keep domain and application logic independent of infrastructure adapters.
- `web/` contains the Vite React TypeScript client.
- `docs/` is the canonical architecture and contract reference. Read [docs/README.md](docs/README.md) first, then the relevant topic document.
- `docs/adr/` records decisions that should not be casually reversed. New public contracts or architectural changes require an ADR or an update to the relevant specification.

## Verified local commands

From the repository root:

```bash
go test ./...
go run ./cmd/nexa
go generate ./...   # regenerate sqlc code after editing internal/database/sqlite/queries/*.sql or migrations
gofmt -l .          # exclude internal/database/sqlite/sqlcgen; must report nothing before pushing
golangci-lint run ./...
go run golang.org/x/vuln/cmd/govulncheck@latest ./...
go run github.com/google/go-licenses@latest check ./... --allowed_licenses="$(jq -r '.allowed | join(",")' .github/license-policy.json)" --ignore nexa --confidence_threshold=0.8
```

For the frontend:

```bash
npm --prefix web install
npm --prefix web run build
npm --prefix web run lint
npm --prefix web run test
npm --prefix web run dev
npm --prefix web audit --audit-level=high
npx --prefix web license-checker-rseidelsohn --production --excludePackages "web@0.0.0" --onlyAllow "$(jq -r '.allowed | join(";")' .github/license-policy.json)"
```

These are the same checks enforced by [.github/workflows/ci.yml](.github/workflows/ci.yml) (jobs `go`, `web`, `license`); run them locally before pushing to catch failures early.

The Go server defaults to `:8321`. `NEXA_ADDR` overrides the listen address and `NEXA_DATA_DIR` overrides the data directory. Current bootstrap endpoints are `GET /healthz`, `GET /api/v1/setup/status`, `POST /api/v1/setup/start`, and `POST /api/v1/setup/complete`.

## Scale Expectations

- Nexa is designed for extremely large media libraries: assume hundreds of thousands to millions of media items and plan for efficient indexing, caching, and retrieval strategies.
- Do not implement features that only work efficiently for small media libraries. Avoid unbounded queries or operations that load the entire dataset into memory. Do not return large result sets without pagination or streaming.
- Use user-scoped queries, pagination, and batching, targetted field selection, appropriate indexing, bounded concurrency, and virtualization techniques to manage large datasets efficiently.
- Long-running operations should have clear progress, useful logs, bounded resource usage, and the ability to resume or cancel gracefully.

## Architecture constraints

- Go is the server language; TypeScript/React/Vite is the client stack.
- SQLite is the baseline database. PostgreSQL is optional and must not change public semantics.
- The baseline deployment is one Nexa process plus one data directory. Do not introduce Redis, an external worker, an object store, or a reverse proxy as a required dependency.
- The production web client is intended to be embedded into the Go binary. Treat the current separate Vite app as scaffold work until the embedding pipeline exists.
- Setup is a mandatory, resumable server-side workflow. Never make setup completion depend only on browser storage or expose the normal application before setup is complete.
- Core provides primitives; extensions provide domain meaning. Avoid hardcoded content-specific enums, schema migrations, or core behavior where an extension contract is intended.
- Extensions use versioned service contracts and the capability broker. Do not expose internal Go types, database tables, router details, or a Go plugin ABI as extension APIs.
- Native media providers run out of process. Core owns media identity, delivery, cache semantics, playback sessions, and scheduling.
- Clients request playback intent; the server owns playback policy, ordering, queue state, and persistence.

## Implementation conventions

- Prefer the Go standard library, especially `net/http`, `database/sql`, `log/slog`, contexts, and streaming I/O, before adding frameworks.
- Keep database-specific code behind repository/storage interfaces. Do not let an ORM or driver define domain contracts.
- Use parameterized queries and explicit query compilation; preserve SQLite/PostgreSQL semantic parity.
- Keep large media off the Go heap. Stream through files, pipes, readers, writers, and bounded buffers.
- Use stable IDs, revisions, structured errors, request correlation, and graceful cancellation at service boundaries.
- Test behavior at boundaries. Add focused unit, integration, contract, or end-to-end coverage according to the risk described in [docs/15-testing-and-compatibility.md](docs/15-testing-and-compatibility.md).
- Do not invent unresolved public behavior silently. Consult [docs/OPEN_QUESTIONS.md](docs/OPEN_QUESTIONS.md) and record decisions through the ADR process.

## Change workflow

1. Read the owning documentation and nearby implementation before editing.
2. Keep changes within the owning package and preserve unrelated user changes.
3. Add or update a focused test for behavior changes.
4. Run the narrowest relevant validation, then `go test ./...` and the relevant `npm --prefix web` command when applicable.
5. Update documentation or an ADR when a public contract, architectural invariant, or development command changes.
