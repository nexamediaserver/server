# Technology Stack

This document records the preferred implementation stack. Exact dependency versions should be pinned in the repository and updated deliberately; architectural contracts must not depend on minor library implementation details.

## Server language: Go

Nexa core is implemented in Go.

Rationale:

- excellent fit for I/O-heavy orchestration;
- simple concurrency model for playback sessions, jobs, scanners, listeners, and extension hosts;
- strong standard HTTP/networking stack;
- fast builds and approachable development workflow;
- excellent cross-platform single-binary deployment;
- built-in asset embedding;
- adequate performance for the core because heavyweight media processing is delegated to providers;
- straightforward subprocess, pipe, socket, and context cancellation support.

The project targets the current supported Go release family. At the time this architecture was consolidated, Go 1.27 is current.

## Core Go stack

Prefer standard library functionality before adding frameworks.

### HTTP

- `net/http`
- standard `ServeMux` where appropriate
- Nexa-owned route/resource broker above the raw router
- OpenAPI contract generation using `oapi-codegen`

Avoid adopting a large HTTP framework unless a concrete requirement cannot be met cleanly by the standard stack.

### Logging

Use `log/slog` as the structured logging foundation.

Logs should carry stable fields such as:

- request ID;
- user/session ID where safe;
- extension ID;
- playback session ID;
- job ID;
- provider ID;
- library ID.

### Database

Default:

- SQLite;
- `database/sql`;
- a cgo-free SQLite driver, currently expected to be `modernc.org/sqlite`, subject to benchmark and compatibility validation.

Optional:

- PostgreSQL;
- `pgx/v5` / `pgxpool`.

Nexa MUST keep its own repository/storage abstraction and query compiler. It MUST NOT expose SQL driver details as domain contracts.

Static, fixed-shape repository queries are implemented via `sqlc`-generated code (see [ADR 0013](adr/0013-sqlc-for-static-repositories.md)), pinned as a Go tool dependency and scoped entirely inside `internal/database/*`. The dynamic Query AST compiler remains hand-written SQL construction and is out of scope for `sqlc`.

### API specifications

- OpenAPI 3.1 for the public HTTP API;
- `oapi-codegen` for Go server/client type generation where appropriate;
- generated TypeScript client/types from the same contract or a compatible generator.

The API specification is the contract. Generated code is an implementation artifact.

### Extension/provider IPC

Prefer versioned, language-neutral messages.

Protocol Buffers are the default serialization format for managed native provider control protocols unless a later ADR supersedes this decision.

Bulk media bytes MUST NOT be copied through Protobuf messages. Use:

- file descriptors/paths under controlled access;
- pipes;
- bounded streams;
- platform handles where needed.

### WebAssembly extensions

Baseline runtime:

- `wazero` as the pure-Go WebAssembly runtime;
- optionally an ABI/runtime layer such as Extism if it materially reduces multi-language SDK work.

Nexa's extension API must remain Nexa-owned. Wazero/Extism are implementation details.

The WebAssembly Component Model/WIT may be adopted later if Go ecosystem support and interoperability justify migration.

### Frontend

- TypeScript;
- React;
- Vite;
- TanStack Router;
- TanStack Query;
- TanStack Table;
- React Hook Form;
- Zod;
- shadcn/ui with Base UI primitives.

The frontend production build is embedded into the Go server using Go's `embed` facilities.

## Media engines

Heavy media engines are optional capability providers:

- libvips: preferred general image transformation provider;
- ImageMagick: optional image fallback/provider for additional formats;
- FFmpeg: video/audio probe, remux, transcode, subtitle conversion, segmentation;
- future providers may implement equivalent capabilities.

These are not linked into the core server process as architectural dependencies.

## Deliberate non-choices

### Native Go `plugin`

Do not use Go's `plugin` package as Nexa's public extension system. It has unsuitable portability, lifecycle, ABI/build, and unloading characteristics for the product requirements.

### Redis as a baseline dependency

Do not require Redis for:

- sessions;
- queues;
- pub/sub;
- locks;
- caching.

The baseline server must run without it.

### ORM as domain architecture

Do not let GORM, Ent, or another ORM define Nexa's persistence or domain model. Nexa's runtime-defined schema and portable query AST are unusual enough that explicit repositories/query compilation are preferable.

### Framework-owned plugin systems

The extension system is a Nexa platform feature and must not be coupled to a web framework or third-party framework's lifecycle.

## Performance philosophy

The Go heap should hold control state and bounded buffers, not whole media files.

Large data paths should stream through:

- `io.Reader`/`io.Writer`;
- `os.File`;
- network connections;
- pipes;
- bounded reusable buffers.

Do not use whole-file reads for large media in playback paths.
