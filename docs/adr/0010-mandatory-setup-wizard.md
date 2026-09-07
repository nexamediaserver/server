# ADR 0010: Mandatory Resumable Setup Wizard

Status: Accepted

## Context

Nexa should provide appliance-like onboarding and extensions must contribute initial configuration.

## Decision

A new instance remains in bootstrap mode until a server-side, resumable setup state machine completes. The first admin is created early; extensions may contribute ordered setup steps.

## Consequences

- Normal application is unavailable before setup completion.
- Steps must be idempotent/resumable.
- Pre-admin setup requires a local/one-time-token security mechanism.
