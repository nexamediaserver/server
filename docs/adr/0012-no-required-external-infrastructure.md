# ADR 0012: No Required External Infrastructure

Status: Accepted

## Context

Nexa must remain easy to deploy.

## Decision

Baseline Nexa requires only its server process, SQLite database, and local data directory. PostgreSQL, Redis-like systems, S3, external search, external workers, and reverse proxies may be optional integrations only.

## Consequences

- Every core feature must have a baseline implementation.
- Advanced integrations cannot become hidden mandatory dependencies.
