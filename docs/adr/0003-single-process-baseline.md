# ADR 0003: Single-Process Baseline

Status: Accepted

## Context

Simple deployment is a product goal.

## Decision

A normal Nexa installation consists of one Nexa server process plus its data directory. In-process workers handle background jobs. Native media/privileged providers may be supervised child processes.

## Consequences

- No required service mesh, worker deployment, Redis, or separate frontend process.
- Advanced process separation may be added later behind stable service boundaries.
