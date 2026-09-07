# ADR 0013: sqlc for Static Repositories; Query AST Compiler Stays Hand-Written

Status: Accepted

## Context

Nexa's persistence layer splits into two shapes:

- Dozens of static, fixed-shape repositories (accounts, libraries, jobs, playback sessions,
  extension registrations, capability broker state, ...) where SQL is known at development time.
- The typed-EAV custom-field model and the Nexa Query AST compiler
  ([docs/05-persistence-and-query.md](../05-persistence-and-query.md)), where SQL is built at
  runtime from validated, resolved field IDs and operators, per backend (SQLite/PostgreSQL).

Today all repository code is hand-written `database/sql` with manual struct scanning
(see [internal/database/sqlite/sqlite.go](../../internal/database/sqlite/sqlite.go)). As the
roadmap adds many more repositories, this pattern accumulates repetitive, error-prone scanning
code with no compile-time check that SQL text still matches the schema or Go struct fields.

[docs/02-technology-stack.md](../02-technology-stack.md) rejects ORMs (GORM/Ent) as domain-model
definers, but does not preclude thinner SQL tooling confined to the infrastructure adapter layer.
SQLite remains the reference backend; PostgreSQL is optional and must preserve semantics without
necessarily executing identical queries.

We evaluated `sqlc` (schema-aware, build-time SQL-to-Go codegen) and `sqlx` (runtime struct-scan
helper over `database/sql`) for the static-repository slice.

## Decision

Adopt `sqlc` for static, fixed-shape repositories. Do not adopt `sqlx`.

- `sqlc` generates typed Go from `.sql` query files, checked against the schema at build time.
  Its `schema:` source must be the existing `golang-migrate` `*.up.sql` files under
  `internal/database/sqlite/migrations` (and the PostgreSQL migrations when that backend is
  implemented) — there is no separate hand-maintained schema snapshot. CI must fail if committed
  generated code is stale relative to queries/schema.
- Generated types (`Queries`, row structs) are confined to `internal/database/{sqlite,postgres}`
  and never appear in exported signatures elsewhere. Hand-written repository interfaces, returning
  domain types, remain the only contract visible to domain/application code.
- The typed-EAV custom-field tables and the Query AST compiler are explicitly out of scope for
  sqlc/sqlx and remain hand-written parameterized SQL/`pgx` string-building — sqlc cannot express
  SQL that is only known at request time, and this split is not a gap to "complete" later.
- PostgreSQL query files are deferred until PostgreSQL support is actually implemented, to avoid
  speculative duplicate-query maintenance. When added, a shared repository-level contract test
  suite must run against both backends to catch semantic drift between the two hand-maintained
  `.sql` sets.
- No dynamic/reflective query construction is permitted through sqlc-generated code; that
  responsibility stays exclusively with the Query AST compiler.

## Consequences

- Adding a new static repository requires writing `.sql` query files and running codegen rather
  than hand-scanning rows; column/type/argument mistakes are caught at build time instead of at
  runtime.
- The codebase carries two SQL-execution idioms: generated code for static repositories and
  hand-rolled SQL for the dynamic query engine. This is treated as an accepted, documented split
  rather than an inconsistency to eliminate.
- Adding PostgreSQL support later requires maintaining a second, parallel set of `.sql` query
  files per repository (sqlc does not share query text across engines) — an accepted duplication
  cost, consistent with "PostgreSQL need not execute queries identically."
- A codegen step (`sqlc generate`, invoked via `go generate` or a Makefile target) is added to the
  development workflow; generated code is committed so plain `go build`/`go vet` do not require
  the sqlc binary.
