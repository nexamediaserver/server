# ADR 0009: Native Providers Run Out of Process

Status: Accepted

## Context

FFmpeg, libvips, ImageMagick, and native integrations are complex and may crash or require platform libraries.

## Decision

Run native media/privileged providers as supervised child processes over a versioned control protocol. Do not dynamically link arbitrary native plugin code into the Nexa process.

## Consequences

- Better failure isolation and ABI stability.
- IPC/supervision/cancellation become platform responsibilities.
- Bulk media travels by streams/files/pipes rather than message copies.
