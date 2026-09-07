# ADR 0011: Contract-First Public HTTP API

Status: Accepted

## Context

Nexa will have multiple clients and extensions and cannot let internal Go structs become de facto API.

## Decision

Define the public HTTP API using OpenAPI 3.1. Generate Go/TypeScript bindings where useful. Treat the specification as source code.

## Consequences

- API changes are reviewable and testable.
- Generated code is reproducible and not hand-edited.
- Extension/internal RPC contracts remain separate from public HTTP API.
