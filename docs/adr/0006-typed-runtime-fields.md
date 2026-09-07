# ADR 0006: Portable Typed Runtime Fields

Status: Accepted

## Context

Libraries and extensions need custom fields without schema migration, but queries must remain typed and efficient on SQLite and PostgreSQL.

## Decision

Use a constrained typed runtime-value model (typed EAV semantics), indexed by immutable field IDs. JSON is reserved for unstructured configuration/escape-hatch values.

## Consequences

- Field add/rename/delete does not require SQL schema migration.
- Query compiler can use typed indexes.
- Exact physical table layout remains benchmark-driven.
