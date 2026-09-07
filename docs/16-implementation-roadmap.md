# Nexa Implementation Roadmap

This roadmap reflects the architecture in the design documents, the current repository state, and the reviews from architecture, product, business, and delivery perspectives. The key improvement is that it now treats delivery risk and product validation as first-class gates instead of framing everything as a purely architectural sequence.

## Current repository snapshot

The repository is a credible start for the platform, but it is still at the edges of the first major milestone:

- Go service entrypoint in `cmd/nexa/`
- data directory bootstrap in `internal/bootstrap/`
- SQLite persistence and migrations in `internal/database/sqlite/`
- app shell and setup lifecycle endpoints in `internal/app/`
- React + TypeScript frontend scaffold in `web/`

The repository is best described as a working bootstrap foundation, not a complete media product. The strongest implemented behavior is the mandatory setup-state flow: data creation, SQLite migration, and persisted setup transitions.

## Strategic intent

The roadmap must preserve the following invariants:

- setup is mandatory and server-side
- the app is not usable until setup completes
- the server owns playback state and sequencing policy
- media providers remain out-of-process and isolated from core
- extension contracts are versioned and deterministic
- product value arrives early enough to validate the architecture against real user workflows

## Updated roadmap: value-first sequencing with architectural gates

### Horizon 0: Foundation and delivery baseline
Scope:

- repository hygiene and CI
- Go formatting/lint/test enforcement
- error and logging conventions
- request correlation and health/readiness conventions
- ADR and open-question closure process
- stable ID / revision conventions

Status:

- partially complete, but not yet fully operational as a release discipline

Exit criteria:

- all code shipped with automated checks
- a release can be built and reviewed without ad hoc process
- decisions that affect public contracts are recorded and versioned

### Horizon 1: Secure bootstrap and setup gate
Scope:

- data directory creation and validation
- SQLite bootstrap and lifecycle management
- resumable setup state machine
- local admin creation
- authentication/session baseline for local admin
- setup protection that blocks the normal application until completion
- graceful startup/shutdown behavior

This is the first non-negotiable milestone and should remain ahead of all feature work.

Exit criteria:

- a clean install reaches a real setup flow and can be resumed safely after restarts
- the normal application remains inaccessible before setup completion
- admin/session behavior is secured and tested

### Horizon 2: Minimal domain and vertical slice
Scope:

- minimal domain model for one working library workflow
- object/listing primitives
- typed fields and persisted values
- stable IDs and revisions
- simple browse/detail views
- local import flow for a single content type
- bounded pagination and authorization-aware listing

This phase should produce a real working slice rather than a speculative platform abstraction.

Design note:

- the goal is not to postpone generality forever; it is to force the architecture to prove itself against a concrete user workflow before investing in deeper abstraction.

Exit criteria:

- a real user can create a library, import content, view it, and restart without losing the core state
- the domain model supports the product workflow without hardcoded content-specific assumptions
- the team has evidence for the query, permission, and persistence model before new abstractions scale up

### Horizon 3: Extension boundary and capability broker
Scope:

- extension manifest and validation
- capability registry and discovery
- version negotiation
- dependency ordering and deterministic conflict detection
- lifecycle and permissions
- one test extension that contributes a real capability

This phase moves earlier than the original roadmap because the extension contract needs to be proven before the project multiplies provider and library features.

Exit criteria:

- a test extension can register capabilities without importing internal core packages
- conflicts are resolved deterministically
- activation is reproducible and guarded by validation

### Horizon 4: Extension-aware setup and reference templates
Scope:

- extension selection during setup
- dependency-aware install order
- recommended capability providers
- extension-provided library scaffolding
- a clear path from setup to a valid runtime state

Exit criteria:

- setup can produce a meaningful runtime based on installed capabilities
- a content extension can contribute a useful structure without core knowing the media type in advance

### Horizon 5: Query engine and authorization baseline
Scope:

- query AST and validation
- SQLite compiler
- field selection and paging
- authorization-aware query execution
- first substantial compatibility suite

This is now a driven milestone, not an abstract platform step.

Exit criteria:

- real workloads can query the system without unbounded scans or unsafe filtering
- query semantics are tested and stable before broader adoption and before PostgreSQL work begins

### Horizon 6: Media asset scaffolding
Scope:

- stable asset IDs and references
- source resolvers
- managed blob storage and cache layout
- asset authorization
- stable media routes
- derivative cache and cache GC
- single-flight and priority-aware media jobs

Exit criteria:

- assets are addressed by identity, not by cache path or client-visible URL assumptions
- media generation remains server-owned and authorized
- the system can support a real artwork or media pipeline without breaking the public contract

### Horizon 7: Provider protocol and test providers
Scope:

- provider supervisor and lifecycle management
- versioned handshake and capability negotiation
- transform/probe operations
- cancellation, progress, and timeout semantics
- file and pipe transfer for bulk payloads
- crash recovery and retry behavior
- fake/test provider first, then native provider integration

This phase is deliberately constrained to a proof-of-contract pattern.

Exit criteria:

- provider behavior is isolated from core logic
- a fake provider can validate protocol semantics before libvips or FFmpeg are introduced
- core remains independent of any specific media engine implementation

### Horizon 8: Playback foundation and direct play
Scope:

- `PlaybackSession` and persisted session state
- playback intent and client capability profile
- queue windows and deterministic ordering
- repeat/shuffle behavior
- reconnect and rehydration logic
- direct-play test media and server-owned playback decisions

This is a critical gate: the server, not the client, owns queue state and playback policy.

Exit criteria:

- server-authoritative sequencing works across reconnects
- direct play is stable and testable with no FFmpeg complexity yet
- the playback model is proven before richer media delivery and transcoding are introduced

### Horizon 9: Media delivery, FFmpeg, and rich playback
Scope:

- probe descriptors and representation model
- remux and transcode planning
- subtitle planning and delivery
- FFmpeg provider integration
- segmented delivery and seek/cancel behavior
- provider fallback and playback diagnostics

Exit criteria:

- media can be delivered through the server without exposing provider-specific behavior to clients
- fallback and diagnostics are reliable in the real world
- playback remains stable across earlier provider or media-engine changes

### Horizon 10: Auth extension contracts
Scope:

- account and auth service contracts
- identity linking and directory/group mapping
- provider login contributions
- secrets service integration
- OIDC first, LDAP second

This should happen after the local admin/session model is stable and after the product has a working baseline.

Exit criteria:

- the account model remains stable even when auth providers change
- auth is not hardcoded into the app shell or admin model
- external identities map cleanly into a single server-side account model

### Horizon 11: Network service broker and external exposure
Scope:

- route and service claims
- TCP/UDP discovery claims
- diagnostics, lifecycle, and conflict detection
- OPDS and other protocol-facing extensions

Exit criteria:

- protocol-based service exposure remains isolated from core internals
- network claims are validated before activation

### Horizon 12: PostgreSQL backend and scale hardening
Scope:

- PostgreSQL repository implementations
- query compiler parity
- job claiming and installation strategy
- migration and compatibility checks
- parity test suite

The project should not do this until the SQLite semantics and compatibility model are stable.

Exit criteria:

- PostgreSQL can be used without changing public semantics or extension contracts
- SQLite remains the default and the reference backend

### Horizon 13: Advanced deployment and scale optimization
Scope:

- optional remote storage
- remote access / DDNS
- external observability
- multi-process or worker scaling
- advanced media generation optimization

This is intentionally deferred and only justified when real usage shows the need.

## Product and delivery priorities

The original roadmap was strong architecturally, but it underemphasized the product risk of a long silent build period. The updated roadmap adds explicit value horizons:

1. Foundation and setup gates
2. One real, working library workflow
3. Extension contracts proven against real usage
4. Playback and media delivery
5. Auth and network expansion
6. Scale and deployment hardening

This ensures the project ships a recognizable user outcome before deep platform abstraction becomes a dominant investment.

## Explicit gates and decision points

### Non-negotiable gates

- Setup gate: no normal app behavior before setup completes
- Core vs extension gate: no content-specific semantics hardcoded into core
- Provider gate: media engines are isolated behind a versioned protocol
- Playback gate: the server owns session state and ordering
- Scalability gate: pagination, auth filtering, and bounded queries are proven before major growth

### Execution gate for a small team

The first release path should be limited to:

- secure setup
- one useful library workflow
- durable local import or indexing
- artwork or media route delivery
- basic server-owned playback

This is the practical beta wedge that keeps the product usable while preserving the architecture.

## What not to build early

Avoid premature complexity such as:

- Redis as a required dependency
- multi-node playback or distributed locking
- arbitrary in-process native plugins
- major experimental workflow engines
- broad network protocol or auth expansion before the local baseline is stable
- PostgreSQL or advanced deployment work before the public semantics are proven

## Summary

The roadmap remains aligned with the architecture, but it is now more disciplined around product value and delivery risk. The critical edits are:

- keep setup gating and admin/session security as the first hard gate
- move the extension contract earlier and prove it with a working test extension
- build a real vertical slice before over-generalizing the platform
- make server-authoritative playback a strict milestone gate
- keep PostgreSQL, advanced network features, and scaling work deferred until there is real demand and proof

This sequencing keeps Nexa faithful to its architectural principles while giving the project a realistic path to a useful, testable product.
