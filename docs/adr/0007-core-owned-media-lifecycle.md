# ADR 0007: Core-Owned Media Lifecycle

Status: Accepted

## Context

Nexa serves posters/thumbnails and playable media while allowing multiple transform engines.

## Decision

Core owns Asset identity, stable media routes, rendition semantics, cache keys, scheduling, representations, and delivery. Extensions/providers perform transformations.

## Consequences

- Removing libvips/FFmpeg does not remove public route scaffolding.
- Providers are replaceable/fallback-capable.
- Derivatives are disposable cache, not canonical fields.
