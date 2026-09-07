# ADR 0004: Capability-Brokered Extension Contracts

Status: Accepted

## Context

Extensions may add many kinds of behavior and may conflict over providers, routes, ports, or semantics.

## Decision

Extensions interact through versioned Nexa service/capability contracts managed by a Capability Broker. Capabilities declare cardinality and scope. Activation is planned and conflict-checked before execution.

## Consequences

- Extensions do not access core internals or DB handles.
- Provider selection/conflicts are deterministic.
- Public extension APIs need explicit compatibility/version management.
