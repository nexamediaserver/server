# ADR 0005: Generalized Agent Model

Status: Accepted

## Context

Nexa must represent individuals, groups, companies, labels, studios, publishers, bands, and other credited entities.

## Decision

Use a generalized `Agent` entity with extensible Agent Kinds, names/aliases, credits, and agent-to-agent relationships.

## Consequences

- No awkward `Person::Company` model.
- Content extensions can add domain-specific kinds and roles.
- Agents should share the custom-field machinery where practical.
