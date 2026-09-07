# Open Questions

These items are intentionally not settled by the current architecture. Implementation agents should not silently invent public behavior for them. Resolve them through design review/ADR when they become blocking.

## Domain naming: Hub vs Library

`Library` and `Hub` are different concepts and should remain distinct:

- A `Library` is a group of folders or other sources scanned to acquire media. The acquired media is then matched to identify what it is.
- A `Hub` is a dynamic collection of media presented in the UI, such as `Next Up`, `Recently Released Movies`, or `Similar Movies`.
- Hubs can also provide related media or agents, such as a `Members` hub on a music band or an `Actors` hub on a movie.

Define the separate public models and their relationship before public API v1 naming freezes.

## Object hierarchy

Fields apply naturally to Agents and Items, and potentially other resource classes.

Question:

- Introduce a generalized `Object` identity/base abstraction?
- Or keep separate entities sharing field machinery?

Avoid premature universal-object tables until access/query requirements are benchmarked.

## Typed field physical schema

Semantic choice is settled: typed runtime values, portable across SQLite/PostgreSQL.

Physical choice remains open:

- one `field_values` table with typed nullable columns;
- per-value-type tables;
- hybrid denormalization.

Benchmark realistic large-library queries before freezing.

## Decimal representation

Need exact semantics across SQLite/PostgreSQL and JSON/API.

Choose:

- scaled integer with declared precision/scale;
- canonical decimal string + index form;
- another exact portable scheme.

Do not use binary floating point for fields requiring exact decimal semantics.

## Extension package format/signing

Need to define:

- archive layout;
- signing model;
- marketplace metadata;
- trust levels;
- unsigned extension policy;
- offline installation;
- rollback.

## WASM ABI layer

Initial preference is wazero, potentially Extism.

Need prototype comparing:

- raw Nexa ABI on wazero;
- Extism-hosted ABI;
- future Component Model/WIT path.

The Nexa service semantics must not depend on this choice.

## Extension UI isolation

Need to decide the strongest stable model for arbitrary extension UI:

- declarative only for v1;
- iframe;
- Web Component/custom element;
- sandboxed module with bridge;
- controlled DOM mount.

Do not expose internal React component imports as stable ABI.

## Native extension sandboxing per OS

Need concrete security implementations for:

- Linux;
- Windows;
- macOS;
- containers.

Provider process isolation is required architecturally; exact OS mechanisms vary.

## SQLite driver final selection

Current preference: cgo-free `modernc.org/sqlite`.

Benchmark/validate against alternatives for:

- performance;
- FTS5;
- backup API;
- WAL behavior;
- platform coverage;
- binary size;
- release maintenance.

## Database migration from SQLite to PostgreSQL

Need supported product workflow:

- offline export/import;
- built-in migration command;
- live migration later.

Do not promise seamless live switching until designed/tested.

## Secret storage

Need platform implementation:

- encrypted local secret store;
- OS keychain integration;
- master-key handling;
- headless/container story;
- backup/restore.

## Public remote-access architecture

The extension hook is settled; the actual Nexa-hosted remote/DDNS service is not.

Need decisions on:

- account service;
- domain/hostname model;
- TLS;
- relay/tunnel;
- discovery;
- privacy;
- operational infrastructure.

## Playback session durability

Need define exact persistence granularity:

- every queue mutation;
- periodic progress;
- generator state;
- ephemeral delivery state.

Goal: reconnect/restart continuity without turning playback progress into excessive SQLite writes.

## Shuffle semantics

Core deterministic random shuffle should exist.

"Smart shuffle" policy should likely be extension/content specific.

Need define which history/user signals core exposes safely to generators.

## Playlist model

Need formal distinction among:

- user-authored static playlist;
- smart/query playlist;
- playback-session queue;
- generated radio/continuation.

Do not reuse one table/model for all four without validating semantics.

## Multi-user simultaneous control

Need define ownership/control permissions for shared playback sessions and future cast/room scenarios.

## Media client protocol

HLS is an expected output, but exact baseline delivery protocols need validation:

- HLS;
- DASH;
- progressive HTTP;
- native direct file;
- WebSocket only for control.

## Hardware transcoding policy

FFmpeg provider should expose capabilities, but need explicit policy for:

- NVENC;
- VAAPI;
- QSV;
- VideoToolbox;
- device selection;
- concurrent session limits;
- HDR tone mapping.

## Cache eviction

Need choose eviction strategy and limits:

- LRU-ish;
- size quotas;
- age;
- per-class budgets;
- reserved playback headroom.

Canonical rule is settled: cache is disposable.

## Library storage/source providers

Need formal source-provider abstraction for:

- local directories;
- SMB/NFS mounted paths;
- remote/object stores;
- extension virtual sources.

Avoid baking POSIX paths into the Asset API.

## Metadata identity/deduplication

Need design external IDs and reconciliation:

- multiple metadata providers;
- duplicate people/agents;
- merge/split;
- source priority/provenance.

This will materially affect Agent quality.

## Backup/export format

Need define a supported portable backup operation including:

- DB;
- managed blobs;
- extension state;
- secrets;
- config;
- extension inventory.

## Supported platform matrix

Need finalize v1 tiering for:

- Linux amd64/arm64;
- Windows amd64/arm64?;
- macOS arm64/amd64?;
- NAS distributions.

## Licensing/distribution of official media providers

FFmpeg/libvips/ImageMagick provider packaging must be reviewed for:

- license obligations;
- codec configuration;
- patented codec jurisdiction concerns;
- update/security mechanism.

Keep provider packaging separate from core so these decisions remain separable.
