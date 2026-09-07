# ADR 0002: SQLite Is the Reference Database

Status: Accepted

## Context

Nexa must be deployable like a home-server appliance without requiring infrastructure services.

## Decision

SQLite is the default and reference database. PostgreSQL is an optional scaling backend.

Features are designed with SQLite semantics first and must remain genuinely useful there.

## Consequences

- One-file database baseline.
- Write transactions must remain short and contention must be engineered deliberately.
- PostgreSQL-only data semantics cannot define the canonical domain.
- Cross-backend contract tests are required once PostgreSQL is implemented.
