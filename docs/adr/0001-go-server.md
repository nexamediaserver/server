# ADR 0001: Implement Nexa Core Server in Go

Status: Accepted

## Context

Nexa's core workload is primarily orchestration: HTTP, SQLite, network protocols, extension lifecycle, playback state, jobs, IPC, streaming, and authorization. CPU-heavy media transforms are delegated to providers.

## Decision

Implement Nexa core in Go. Use standard-library facilities where practical, especially `net/http`, `context`, `embed`, and `log/slog`.

Rust remains acceptable for external providers/tools where low-level FFI or memory control is advantageous.

## Consequences

- Fast builds and simple cross-platform deployment.
- Natural concurrency for sessions/listeners/jobs.
- GC exists; large media must stream through bounded buffers rather than accumulate in heap.
- Go's native `plugin` mechanism is not used for third-party extensions.
