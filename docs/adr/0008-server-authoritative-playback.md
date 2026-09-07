# ADR 0008: Server-Authoritative Playback

Status: Accepted

## Context

Nexa needs dynamic queues, sequence semantics, shuffle/radio generators, and consistent behavior across clients.

## Decision

Clients submit playback intent. The server owns PlaybackSession state, sequence generation, queue windows, overlays, repeat/shuffle policy, and current item. A separate Media Delivery Planner decides how the current playable is delivered.

## Consequences

- Clients remain domain-light.
- Reconnect/handoff can preserve queue semantics.
- Sequence generators need serializable/deterministic state.
