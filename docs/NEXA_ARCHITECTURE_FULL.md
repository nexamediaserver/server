# Nexa Architecture and Implementation Reference — Consolidated Edition

This file concatenates the modular documentation set for tools or agents that prefer a single reference document.
The modular files remain canonical for editing.


---

<!-- BEGIN README.md -->

# Nexa Architecture and Implementation Reference

Status: **Architecture baseline**
Last consolidated: **2026-09-06**

This documentation set defines the intended architecture of the Nexa server and web client. It is a reference for implementation work, design reviews, and future architectural decisions. It is deliberately not an implementation prompt.

## What Nexa is

Nexa is an extensible, self-hosted media and content server designed to combine two properties that are usually in tension:

1. **Appliance-like deployment and operation.** A baseline installation should be as easy to deploy as Plex Media Server: one server process, one data directory, a mandatory browser-based setup wizard, SQLite by default, local file storage by default, and no required external infrastructure.
2. **Platform-like extensibility.** Extensions can add content/library types, fields, agent kinds, metadata providers, media transformation providers, playback sequencing behavior, authentication providers, network protocols, setup steps, APIs, remote-access/DDNS features, and other capabilities.

The architecture follows two core principles:

> **Core provides primitives; extensions provide meaning.**

> **Clients request intent; the server owns policy and state.**

## Architectural invariants

The following are settled unless superseded by an explicit Architecture Decision Record (ADR):

- The Nexa server is implemented in **Go**.
- The web client is implemented in **TypeScript/React**.
- **SQLite is the reference/default database**, not a compatibility mode.
- **PostgreSQL is an optional scaling backend**.
- A stock installation requires no PostgreSQL, Redis, external object store, external worker, reverse proxy, or separate web runtime.
- The production web application is embedded into the server binary.
- The server has a **mandatory, resumable first-run setup wizard**.
- The domain model is runtime-extensible. Content-specific concepts are not hardcoded into the core unless they are true platform primitives.
- People, groups, companies, labels, studios, publishers, and similar credited entities are represented by a generalized **Agent** model.
- Media assets are first-class objects. Posters, thumbnails, portraits, videos, audio, subtitles, and other byte-backed resources are referenced by asset identity rather than by cache paths.
- Nexa core owns media delivery, cache semantics, transformation requests, playback sessions, and scheduling.
- Media engines such as FFmpeg, libvips, and ImageMagick are providers, not core dependencies.
- Native media providers run **out of process**.
- The client does not own authoritative playback order. The server creates and mutates server-side playback sessions and dynamically materializes queue windows.
- Extensions interact with core services through **versioned service contracts and a capability broker**, not internal Go implementation types.
- Native Go `plugin` loading is not used as the public extension mechanism.
- Extension conflicts are detected before activation. "Last loaded wins" behavior is forbidden.

## Documentation map

1. [Vision and Principles](01-vision-and-principles.md)
2. [Technology Stack](02-technology-stack.md)
3. [System Architecture](03-system-architecture.md)
4. [Domain Model](04-domain-model.md)
5. [Persistence and Query Model](05-persistence-and-query.md)
6. [Extension Platform](06-extension-platform.md)
7. [Media Assets and Transformation](07-media-assets-and-transformation.md)
8. [Playback Architecture](08-playback-architecture.md)
9. [Accounts, Authentication, and Authorization](09-accounts-auth-and-authorization.md)
10. [Network Services and Protocol Extensions](10-network-services-and-protocols.md)
11. [Setup Wizard and Deployment](11-setup-wizard-and-deployment.md)
12. [API and Client Contracts](12-api-and-client-contracts.md)
13. [Jobs, Events, and Observability](13-jobs-events-observability.md)
14. [Security and Trust Boundaries](14-security.md)
15. [Testing and Compatibility](15-testing-and-compatibility.md)
16. [Implementation Roadmap](16-implementation-roadmap.md)
17. [Open Questions](OPEN_QUESTIONS.md)
18. [Glossary](GLOSSARY.md)
19. [Architecture Decision Records](adr/README.md)

### Draft interface specifications

- [Extension Manifest](specs/extension-manifest.md)
- [Capability Model](specs/capability-model.md)
- [Native Media Provider Protocol](specs/media-provider-protocol.md)
- [Playback Session Contract](specs/playback-session.md)
- [Data Directory Layout](specs/data-directory.md)

## How to use these documents

Implementation should follow the normative language in these documents:

- **MUST / MUST NOT**: architectural invariant or compatibility requirement.
- **SHOULD / SHOULD NOT**: strong default; deviation needs a documented reason.
- **MAY**: optional implementation choice.

Where an implementation detail is not specified, prefer the simplest design that preserves the invariants above. Do not invent extension-visible contracts ad hoc; new public contracts require an ADR or an update to the relevant specification document.

<!-- END README.md -->

---

<!-- BEGIN 01-vision-and-principles.md -->

# Vision and Principles

## Product goal

Nexa should feel simple enough for a home user to install and operate, while remaining powerful enough to become a general-purpose media/content platform.

A normal user should be able to:

1. install Nexa;
2. launch it;
3. open a browser;
4. complete the mandatory setup wizard;
5. install recommended extensions;
6. create an administrator account;
7. create libraries;
8. optionally enable remote access;
9. use the server.

An advanced administrator should be able to replace or extend many parts of the system without forking Nexa.

## Principle 1: Core provides primitives; extensions provide meaning

Core concepts should be stable and domain-independent:

- objects/items;
- agents;
- fields and field definitions;
- relationships and credits;
- libraries/hubs;
- views;
- query AST;
- media assets and representations;
- users, roles, sessions, and permissions;
- playback sessions;
- extension capabilities;
- jobs and events;
- network resources.

Domain concepts such as `movie`, `episode`, `album`, `record_label`, `game_developer`, or `book_author` should normally come from extensions.

This keeps the core small and allows entirely new content types without core migrations or releases.

## Principle 2: Clients request intent; the server owns policy and state

Clients should be thin with respect to domain policy.

For playback, a client says in effect:

> "Play this target in sequence."

The server decides:

- what the target means;
- the sequence context;
- what should play now;
- what comes next;
- how shuffle or radio behavior works;
- when the dynamic queue should be extended;
- whether explicit user queue mutations override generated continuation;
- which media representation should be used;
- whether the resource is direct-played, remuxed, or transcoded.

Clients report capabilities and user actions. They do not become authoritative policy engines.

The same philosophy applies elsewhere: the server should be the authoritative source for permissions, field semantics, extension selection, and persistent state.

## Principle 3: Simple usage does not pay for advanced usage

The baseline must not require services that only advanced deployments need.

Baseline:

```text
Nexa
├── SQLite
└── local filesystem
```

Optional scaling integrations may include:

- PostgreSQL;
- S3-compatible blob storage;
- external identity providers;
- external reverse proxies;
- advanced search providers;
- separate worker processes;
- multi-node coordination.

Those integrations must not make the baseline architecture second-class.

## Principle 4: Extensions consume contracts, not internals

No extension may depend on:

- unexported Go packages;
- database table layout as an application API;
- internal pointers or in-memory structures;
- Go ABI/plugin compatibility;
- direct access to the main database connection;
- core HTTP router implementation details.

Extensions use versioned service contracts exposed by the extension host/capability broker.

## Principle 5: Explicit ownership and conflict handling

Anything globally scarce or semantically exclusive must have an explicit owner:

- network ports;
- route mounts;
- exclusive capabilities;
- selected providers;
- default sequence resolvers;
- primary remote-access provider.

Conflicts are resolved before activation, either deterministically by policy or explicitly by the administrator. Loading order is never a conflict-resolution mechanism.

## Principle 6: Reproducible data; disposable cache

Derivatives and temporary playback outputs are caches or ephemeral resources, not canonical data.

Deleting the cache directory must not destroy a Nexa installation. It should only cause regeneration.

## Principle 7: Fail gracefully

Removing a capability provider should reduce capability, not corrupt the server.

Examples:

- removing libvips while ImageMagick remains should transparently change provider selection;
- removing all image transformers should leave direct-serving of compatible originals possible;
- removing FFmpeg should disable remux/transcode but preserve direct play;
- a failed extension should be isolatable without crashing Nexa core;
- a setup wizard interruption should resume safely.

## Principle 8: Stable identifiers over display names

Runtime-configurable entities must use immutable IDs internally.

Renaming a field, library, agent kind, role, view, or extension-visible resource must not invalidate references.

Display labels are presentation metadata, not identity.

## Principle 9: Cross-platform behavior is a first-class requirement

Primary supported environments should include:

- Linux;
- Windows;
- macOS;
- containers;
- common home-server/NAS environments where practical.

Platform-specific capabilities may differ, especially hardware transcoding, but public server semantics should remain consistent.

<!-- END 01-vision-and-principles.md -->

---

<!-- BEGIN 02-technology-stack.md -->

# Technology Stack

This document records the preferred implementation stack. Exact dependency versions should be pinned in the repository and updated deliberately; architectural contracts must not depend on minor library implementation details.

## Server language: Go

Nexa core is implemented in Go.

Rationale:

- excellent fit for I/O-heavy orchestration;
- simple concurrency model for playback sessions, jobs, scanners, listeners, and extension hosts;
- strong standard HTTP/networking stack;
- fast builds and approachable development workflow;
- excellent cross-platform single-binary deployment;
- built-in asset embedding;
- adequate performance for the core because heavyweight media processing is delegated to providers;
- straightforward subprocess, pipe, socket, and context cancellation support.

The project targets the current supported Go release family. At the time this architecture was consolidated, Go 1.27 is current.

## Core Go stack

Prefer standard library functionality before adding frameworks.

### HTTP

- `net/http`
- standard `ServeMux` where appropriate
- Nexa-owned route/resource broker above the raw router
- OpenAPI contract generation using `oapi-codegen`

Avoid adopting a large HTTP framework unless a concrete requirement cannot be met cleanly by the standard stack.

### Logging

Use `log/slog` as the structured logging foundation.

Logs should carry stable fields such as:

- request ID;
- user/session ID where safe;
- extension ID;
- playback session ID;
- job ID;
- provider ID;
- library ID.

### Database

Default:

- SQLite;
- `database/sql`;
- a cgo-free SQLite driver, currently expected to be `modernc.org/sqlite`, subject to benchmark and compatibility validation.

Optional:

- PostgreSQL;
- `pgx/v5` / `pgxpool`.

Nexa MUST keep its own repository/storage abstraction and query compiler. It MUST NOT expose SQL driver details as domain contracts.

### API specifications

- OpenAPI 3.1 for the public HTTP API;
- `oapi-codegen` for Go server/client type generation where appropriate;
- generated TypeScript client/types from the same contract or a compatible generator.

The API specification is the contract. Generated code is an implementation artifact.

### Extension/provider IPC

Prefer versioned, language-neutral messages.

Protocol Buffers are the default serialization format for managed native provider control protocols unless a later ADR supersedes this decision.

Bulk media bytes MUST NOT be copied through Protobuf messages. Use:

- file descriptors/paths under controlled access;
- pipes;
- bounded streams;
- platform handles where needed.

### WebAssembly extensions

Baseline runtime:

- `wazero` as the pure-Go WebAssembly runtime;
- optionally an ABI/runtime layer such as Extism if it materially reduces multi-language SDK work.

Nexa's extension API must remain Nexa-owned. Wazero/Extism are implementation details.

The WebAssembly Component Model/WIT may be adopted later if Go ecosystem support and interoperability justify migration.

### Frontend

- TypeScript;
- React;
- Vite;
- TanStack Router;
- TanStack Query;
- TanStack Table;
- React Hook Form;
- Zod;
- shadcn/ui with Base UI primitives.

The frontend production build is embedded into the Go server using Go's `embed` facilities.

## Media engines

Heavy media engines are optional capability providers:

- libvips: preferred general image transformation provider;
- ImageMagick: optional image fallback/provider for additional formats;
- FFmpeg: video/audio probe, remux, transcode, subtitle conversion, segmentation;
- future providers may implement equivalent capabilities.

These are not linked into the core server process as architectural dependencies.

## Deliberate non-choices

### Native Go `plugin`

Do not use Go's `plugin` package as Nexa's public extension system. It has unsuitable portability, lifecycle, ABI/build, and unloading characteristics for the product requirements.

### Redis as a baseline dependency

Do not require Redis for:

- sessions;
- queues;
- pub/sub;
- locks;
- caching.

The baseline server must run without it.

### ORM as domain architecture

Do not let GORM, Ent, or another ORM define Nexa's persistence or domain model. Nexa's runtime-defined schema and portable query AST are unusual enough that explicit repositories/query compilation are preferable.

### Framework-owned plugin systems

The extension system is a Nexa platform feature and must not be coupled to a web framework or third-party framework's lifecycle.

## Performance philosophy

The Go heap should hold control state and bounded buffers, not whole media files.

Large data paths should stream through:

- `io.Reader`/`io.Writer`;
- `os.File`;
- network connections;
- pipes;
- bounded reusable buffers.

Do not use whole-file reads for large media in playback paths.

<!-- END 02-technology-stack.md -->

---

<!-- BEGIN 03-system-architecture.md -->

# System Architecture

## High-level topology

A baseline Nexa installation is one operating-system process plus one data directory.

```text
                      Browser / Nexa Client
                               │
                         HTTP / WebSocket
                               │
┌──────────────────────────────▼──────────────────────────────┐
│                         Nexa Server                        │
│                                                           │
│  API / Route Broker                                       │
│  Setup / Accounts / Authorization                         │
│  Domain Services                                          │
│  Query Engine                                             │
│  Playback Orchestrator                                    │
│  Media Delivery Planner                                   │
│  Media Asset Service                                      │
│  Job Scheduler / Event Bus                                │
│  Extension Manager / Capability Broker                    │
│  Network Service Broker                                   │
│                                                           │
└──────────────┬─────────────────┬──────────────────┬─────────┘
               │                 │                  │
          SQLite default     local blobs       extension hosts
          PostgreSQL opt.    S3 optional       WASM/subprocesses
```

The frontend is compiled separately but embedded into the server release artifact.

## Core subsystem boundaries

### Bootstrap and setup

Responsibilities:

- detect whether setup is complete;
- restrict access while unconfigured;
- maintain resumable setup state;
- create the first administrator;
- coordinate extension-contributed setup steps;
- transition atomically into normal operation.

### Accounts and authorization

Responsibilities:

- Nexa user identities;
- credentials/linked identities;
- roles and permissions;
- sessions/tokens;
- user preferences;
- provider integration for OIDC, LDAP, etc.

### Domain services

Responsibilities:

- libraries/hubs;
- items/objects;
- agents;
- fields;
- roles and relationships;
- views and saved queries;
- extension-defined semantic registries.

Domain services MUST NOT know whether the active database is SQLite or PostgreSQL.

### Query engine

Responsibilities:

- validate a Nexa query AST;
- resolve field/relationship semantics;
- compile it to backend-specific parameterized SQL;
- expose consistent sorting/filtering/pagination semantics;
- use backend-native acceleration without changing public semantics.

### Media asset service

Responsibilities:

- stable media asset identity;
- source resolution;
- metadata/probe results;
- source revision tracking;
- persistent representations;
- derivative requests;
- cache keys and invalidation.

### Playback orchestrator

Responsibilities:

- accept playback intent;
- resolve playback context;
- create authoritative playback sessions;
- select and maintain sequence generators;
- materialize a moving queue window;
- apply explicit user queue mutations;
- persist sufficient session/generator state;
- coordinate transition between entries.

### Media delivery planner

Responsibilities:

- inspect the current playable and its representations;
- inspect client capabilities;
- inspect available provider capabilities;
- decide direct play, remux, transcode, subtitle handling, and output format;
- request work from media providers;
- expose delivery resources through Nexa-owned HTTP endpoints.

### Jobs and events

Responsibilities:

- persistent deferred/scheduled work;
- retries/backoff;
- priorities;
- cancellation;
- background maintenance;
- typed application events.

The job system must run in-process for baseline deployment.

### Extension manager and capability broker

Responsibilities:

- extension package validation;
- dependency resolution;
- permission grants;
- capability registration;
- service version negotiation;
- provider selection;
- conflict detection;
- activation/deactivation;
- health and lifecycle;
- setup contributions.

### Network service broker

Responsibilities:

- HTTP route claims;
- TCP listeners;
- UDP listeners;
- multicast memberships;
- discovery advertisements;
- protocol-specific resource ownership;
- port/path conflicts;
- shutdown and health integration.

## Internal communication

Core packages use ordinary Go interfaces and types.

Do not force all internal calls through a generic RPC abstraction. The public extension boundary and the internal implementation boundary are different concerns.

Extension boundaries use versioned contracts.

## Package direction

Dependency direction should approximately follow:

```text
transport / adapters
        │
        ▼
application services
        │
        ▼
domain / platform contracts
        ▲
        │
infrastructure adapters
```

Avoid circular dependencies and globally shared mutable application state.

## Suggested repository layout

```text
cmd/
  nexa/

internal/
  app/
  bootstrap/
  setup/
  accounts/
  authz/
  objects/
  agents/
  fields/
  libraries/
  views/
  query/
  database/
    sqlite/
    postgres/
  storage/
  media/
    assets/
    cache/
    delivery/
    planner/
    providers/
  playback/
    orchestrator/
    sequence/
    sessions/
  jobs/
  events/
  extensions/
    manager/
    broker/
    wasm/
    native/
  network/
  api/
  observability/

api/
  openapi/

proto/
  extension/
  provider/

web/

docs/
```

The exact package split may evolve. Public architectural boundaries must remain explicit even if physical package names change.

## Process model

Baseline:

```text
one Nexa process
├── HTTP server
├── job workers
├── playback sessions
├── extension WASM runtime
├── managed provider supervisors
└── protocol listeners
```

Native media engines and privileged native extensions are separate child processes where isolation is required.

Advanced deployments may later allow selected worker roles to move to separate processes, but this is not a baseline requirement.

## Shutdown

Graceful shutdown MUST:

1. stop accepting new external work;
2. stop or quiesce extension/network listeners;
3. cancel playback/provider work with bounded grace periods;
4. stop job claiming;
5. flush durable state;
6. close database/storage resources.

Cancellation must propagate using Go contexts for core operations wherever practical.

<!-- END 03-system-architecture.md -->

---

<!-- BEGIN 04-domain-model.md -->

# Domain Model

## Overview

The Nexa domain model is intentionally generic. It must support media/content types not known when core is compiled.

The key concepts are:

- Library/Hub
- Item/Object
- Field Definition and Field Value
- Agent and Agent Kind
- Credit and Role
- Relationships
- Media Asset
- View
- User

## Libraries / Hubs

A library is a configured content domain.

Typical examples:

- Movies
- Television
- Music
- Games
- Books
- Comics

These examples are provided by extensions, not hardcoded core enums.

A library contains configuration such as:

```text
id
slug
name
icon
content type/template
field definitions
views
permissions
metadata configuration
storage/source configuration
extension-specific settings
```

Creating or modifying a library must not require a database schema migration.

## Items / Objects

An Item is a record belonging to a library/hub.

Core properties should remain minimal:

```text
id
library_id
created_at
updated_at
revision
lifecycle/status metadata
```

Domain-specific values come from fields and relationships.

The architecture may later generalize Item into a broader Object abstraction. Until that decision is finalized, public contracts should avoid assuming that every field-bearing entity is necessarily an Item.

## Fields

A Field Definition describes a runtime-defined field.

Minimum conceptual properties:

```text
id
namespace/key
display name
field type
target object class
required
configuration
validation
indexing/query hints
position/display metadata
```

Field values always reference immutable field IDs, not labels.

Field types are registered capabilities. Core provides basic scalar types; extensions may add domain-specific field types.

Core field types are expected to include:

- short text;
- long text;
- integer;
- decimal;
- boolean;
- date;
- datetime;
- URL;
- email;
- enum;
- multi-enum;
- relation;
- file/media asset;
- image/media asset;
- user reference.

## Agents

The core abstraction is `Agent`, not `Person`.

An Agent represents an entity that can participate in credits or relationships:

- individual person;
- group of persons;
- company;
- organization;
- studio;
- record label;
- publisher;
- band;
- orchestra;
- team;
- collective;
- extension-defined entity kind.

The UI may use friendlier labels such as "People & Organizations".

### Agent identity

Conceptual core properties:

```text
id
kind_id
primary/display name
created_at
updated_at
revision
```

Agents should use the same custom-field machinery as content records wherever practical.

### Agent kinds

Agent kinds are runtime-extensible and namespaced.

Examples:

```text
core.person
core.group
core.organization

music.band
music.orchestra
music.record_label

games.developer
games.publisher

movies.production_company
books.publisher
```

An agent kind may expose broader capabilities/semantic parents such as `organization-like` without requiring language-level inheritance.

Do not force a universal taxonomy that makes a record label and game publisher artificially identical. Generic behavior comes from capabilities; domain meaning remains namespaced.

### Names and aliases

Agents need multiple names.

Conceptual model:

```text
agent_names
-----------
agent_id
name
name_type
language
script
is_primary
valid_from?
valid_to?
```

This supports:

- aliases;
- stage names;
- pseudonyms;
- historical names;
- translated/transliterated names;
- localized display selection.

## Credits

A role is not a property of an Agent. It is the meaning of a relationship between an Item/Object and an Agent.

Conceptual credit:

```text
id
subject_object_id
agent_id
role_id
credited_as
position
metadata
```

Roles are namespaced and extension-defined.

Examples:

```text
movies.director
movies.actor
music.artist
music.composer
music.label
games.developer
games.publisher
books.author
books.translator
```

`credited_as` preserves the exact name used for a specific work.

## Agent-to-agent relationships

Groups, companies, and organizations require explicit relationships.

Examples:

- `member_of`;
- `subsidiary_of`;
- `imprint_of`;
- `successor_of`;
- extension-defined relation types.

Conceptual relationship:

```text
source_agent_id
target_agent_id
relation_type_id
valid_from
valid_to
metadata
```

Temporal validity is important for band membership and corporate history.

## Item/Object relationships

Content-to-content relationships must also be first class.

Examples:

- episode belongs to season;
- season belongs to series;
- sequel/prequel;
- track belongs to disc/album;
- adaptation of;
- alternate version;
- collection membership.

The exact physical storage may share infrastructure with generic relationships, but relation semantics remain typed and namespaced.

## Views

A View is a saved presentation/query configuration over a library or compatible object scope.

It may include:

```text
columns/fields
filter AST
sorting
grouping
pagination defaults
display type
display-specific configuration
```

Display types may include:

- table;
- cards;
- gallery;
- kanban;
- calendar;
- timeline;
- extension-defined views.

Views must not own duplicated canonical content data.

## Identity rules

All persistent entities use immutable stable IDs.

Renaming:

- a field;
- agent;
- library;
- role;
- relation type;
- view;
- extension-provided semantic resource

must not break references.

Use namespaced symbolic IDs for extension-defined registries and opaque immutable IDs for instances.

<!-- END 04-domain-model.md -->

---

<!-- BEGIN 05-persistence-and-query.md -->

# Persistence and Query Model

## Goals

Persistence must satisfy all of the following:

- SQLite is fully first-class.
- PostgreSQL is optional.
- Custom fields do not require schema migrations.
- Typed filtering and sorting remain efficient.
- Backend-specific acceleration is allowed.
- Domain semantics are independent of SQL dialect.
- Cache state is disposable.
- Migration and recovery are predictable.

## SQLite is the reference backend

The default database is SQLite.

Recommended baseline connection settings include:

```sql
PRAGMA journal_mode = WAL;
PRAGMA synchronous = NORMAL;
PRAGMA foreign_keys = ON;
PRAGMA busy_timeout = 5000;
```

Exact settings must be benchmarked on supported platforms.

### SQLite write behavior

SQLite permits many concurrent readers but effectively serializes writers.

Nexa should therefore deliberately keep write transactions short and may use an internal write coordinator/queue for mutation-heavy workflows.

Bulk operations should commit in bounded chunks rather than hold long transactions.

Do not rely on experimental SQLite concurrency branches/features as a baseline requirement.

## PostgreSQL

PostgreSQL is a scaling backend for deployments needing properties such as:

- high concurrent write volume;
- multi-process or future multi-node operation;
- HA/replication;
- operational tooling;
- database-managed advanced features.

PostgreSQL must preserve Nexa semantics, but it need not execute queries identically to SQLite.

## No JSONB-centric canonical model

Do not make PostgreSQL JSONB the canonical custom-field representation and then emulate it poorly on SQLite.

SQLite JSON and PostgreSQL JSONB may be used for unstructured configuration or escape-hatch values, but core query semantics should not depend on backend-specific JSON behavior.

## Custom field storage

Use a constrained typed-EAV style model.

Conceptually:

```text
items
-----
id
library_id
created_at
updated_at
revision

field_definitions
-----------------
id
library_id/scope
namespace/key
display_name
field_type
configuration_json
validation_json
required
position
...

field_values
------------
object_id
field_id
position

text_value
integer_value
real_value
decimal_value
boolean_value
date_value
datetime_value
json_value
reference_value
...
```

Only the column appropriate to the field type is populated for scalar values.

Alternative physical layouts, including per-type value tables, are allowed if benchmarks show clear benefits. The semantic model remains typed.

### Indexes

Indexes should be designed around actual filter/sort access patterns, e.g.:

```sql
(field_id, integer_value)
(field_id, text_value)
(field_id, date_value)
```

Do not create every possible index blindly. Index policy may become configurable or adaptive later.

## Query AST

Public querying uses a Nexa query AST, not SQL-shaped URL conventions.

Example:

```json
{
  "filter": {
    "and": [
      {
        "field": "field-id-year",
        "operator": "gte",
        "value": 1980
      },
      {
        "field": "field-id-year",
        "operator": "lt",
        "value": 1990
      }
    ]
  },
  "sort": [
    {
      "field": "field-id-title",
      "direction": "asc"
    }
  ],
  "page": {
    "limit": 50
  }
}
```

Supported AST concepts should include:

- `and`;
- `or`;
- `not`;
- scalar comparisons;
- emptiness;
- containment/membership;
- relation predicates;
- credit/agent predicates;
- full-text search;
- extension-defined operators where safely registered;
- sorting;
- grouping/aggregation where supported;
- cursor/page constraints.

## Query validation

Before SQL compilation the query engine MUST:

1. resolve target library/object scope;
2. resolve field/relation IDs;
3. validate operator compatibility with field types;
4. validate supplied value types;
5. enforce authorization;
6. enforce query complexity/resource limits;
7. normalize the AST.

Extensions must not inject raw SQL into the query compiler.

## Backend-specific compilers

```text
Nexa Query AST
      │
      ├── SQLite compiler
      │     ├── ordinary indexes
      │     └── FTS5
      │
      └── PostgreSQL compiler
            ├── PG indexes
            └── PG-native full text/optimizations
```

Semantics are shared. Execution strategy may differ.

## Search

Default full-text search uses SQLite FTS5.

PostgreSQL deployments may use PostgreSQL full-text search.

A future dedicated search provider may be added behind a stable search abstraction if real requirements justify it.

## Migrations

Schema migrations are owned by Nexa core.

Rules:

- migrations are monotonic and versioned;
- every migration is tested from supported historical versions;
- migrations must be safe to resume or fail clearly;
- destructive migrations require backups/explicit recovery policy;
- extensions MUST NOT run arbitrary migrations against Nexa core tables.

Extensions needing persistent private state should use an extension-owned storage API or namespaced storage facility.

## Cache database

Derivative/media cache metadata may use a separate SQLite database such as `cache.db`.

Loss of cache metadata MUST NOT damage canonical Nexa data.

Avoid writing cache last-access timestamps into the main Nexa database for every media request.

## Backups

The architecture should support a coherent backup unit:

- canonical database;
- configuration;
- managed blobs;
- extension state;
- secrets according to a documented secure export policy.

Derivative caches and transient playback segments are excluded from required backups.

<!-- END 05-persistence-and-query.md -->

---

<!-- BEGIN 06-extension-platform.md -->

# Extension Platform

## Purpose

Extensibility is a core platform property, not a secondary plugin API.

Extensions may add:

- library/content templates;
- field types;
- field definitions;
- agent kinds;
- credit roles;
- relationship types;
- views;
- metadata providers;
- sequence resolvers and generators;
- media providers;
- authentication/directory providers;
- remote-access providers;
- setup wizard steps;
- background jobs;
- event listeners;
- network protocols;
- HTTP APIs/routes;
- frontend UI contributions;
- other versioned capabilities.

## Extension classes

Nexa recognizes multiple execution classes because one sandbox/runtime is not appropriate for every extension.

### Declarative extensions

Use data/schema declarations for:

- library templates;
- field/role definitions;
- setup forms;
- settings schemas;
- simple views;
- metadata mappings;
- capability metadata.

Prefer declarative configuration whenever executable code is unnecessary.

### WebAssembly extensions

Use sandboxed WebAssembly for ordinary executable extensions such as:

- metadata logic;
- sequence generation;
- account integrations where supported;
- automation/business logic;
- service consumers/producers that fit the sandbox.

The runtime is initially expected to use wazero, potentially with Extism as an implementation layer.

### Managed native extensions

Use child processes for capabilities requiring privileged/native integration unsuitable for WASM:

- unusual OS integration;
- network protocol implementations where sandbox limitations are material;
- specialized native libraries.

### Native media providers

A specialized managed-native class for FFmpeg, libvips, ImageMagick, and future media engines.

They use Nexa's media provider protocol and do not own Nexa HTTP endpoints.

## No in-process native plugin ABI

Extensions MUST NOT depend on Go's `plugin` package or an unstable in-process ABI.

Native code isolation is an explicit stability boundary.

A native extension/provider crash should not ordinarily crash Nexa core.

## Package structure

A conceptual extension package may contain:

```text
example.nexa/
├── manifest.toml
├── wasm/
│   └── extension.wasm
├── native/
│   ├── linux-amd64/...
│   ├── linux-arm64/...
│   ├── windows-amd64/...
│   └── darwin-arm64/...
├── web/
│   └── ...
├── schemas/
└── assets/
```

The final archive/signature format is an open implementation decision.

## Manifest

The manifest must declare enough information for safe planning before activation:

```text
extension ID
version
Nexa compatibility range
execution class
provided capabilities
consumed/required capabilities
optional/recommended dependencies
permissions
resource claims
setup contributions
settings schemas
platform artifacts
conflicts/replacements where applicable
```

Extension IDs are globally namespaced, e.g. reverse-DNS style.

## Core Service API

Extensions consume versioned Nexa services, conceptually including:

```text
nexa.accounts
nexa.authorization
nexa.secrets

nexa.objects
nexa.fields
nexa.agents
nexa.libraries
nexa.query

nexa.media
nexa.playback

nexa.jobs
nexa.events
nexa.storage
nexa.http-client

nexa.api
nexa.network
nexa.discovery

nexa.settings
nexa.setup
nexa.logging
```

Service APIs are versioned independently where useful.

Examples:

```text
nexa.accounts@1
nexa.media@2
```

An extension negotiates supported versions during activation.

## Capability Broker

The Capability Broker owns:

- registration;
- discovery;
- version compatibility;
- permission validation;
- provider selection;
- ordered hooks;
- dependencies;
- conflicts;
- activation;
- health;
- lifecycle;
- deactivation.

Extensions do not locate one another by scanning processes or importing implementation packages.

## Capability cardinality

Capabilities declare how multiple providers coexist.

### MULTI

Any number of providers may coexist.

Examples:

- metadata providers;
- image transform providers.

Selection happens per request/configuration.

### ORDERED

Multiple providers/listeners run in deterministic configured order.

Examples:

- event listeners;
- transformation/post-processing hooks where explicitly designed.

### SELECT_ONE

Multiple providers may be installed, but one provider must be selected for a given scope.

Examples:

- default TV sequence semantics;
- primary identity provider in a constrained configuration;
- primary remote-access integration.

### EXCLUSIVE

Only one claim can exist for the relevant scope/resource.

Examples:

- a specific network port/socket binding;
- an exclusive route mount;
- another uniquely owned operating-system resource.

## Capability scope

Selection/conflict scope may be:

- global;
- server;
- user;
- library;
- hub/content type;
- playback session;
- request.

Provider selection rules must make scope explicit.

## Conflict detection

Activation uses a planning phase:

```text
install/enable request
        │
        ▼
read manifest
        │
        ▼
resolve dependencies
        │
        ▼
collect resource claims
        │
        ▼
validate permissions
        │
        ▼
detect conflicts
        │
   ┌────┴─────┐
   │          │
 valid      conflict
   │          │
activate    report/resolve
```

"Last loaded wins" is forbidden.

Conflicts include:

- duplicate symbolic ownership;
- incompatible capability versions;
- unsatisfied required capabilities;
- HTTP path collisions;
- TCP/UDP port collisions;
- conflicting exclusive providers;
- unresolved SELECT_ONE providers;
- incompatible extension-declared conflicts;
- insufficient permissions.

## Namespaced registries

These resources must be globally namespaced:

- extension IDs;
- field type IDs;
- agent kind IDs;
- role IDs;
- relation type IDs;
- capability IDs;
- event IDs;
- extension settings IDs.

## Hooks versus events

Use typed service/provider contracts when Nexa needs an answer.

Examples:

- authenticate this credential;
- determine sequence semantics;
- transform this image;
- resolve metadata.

Use events when Nexa is announcing something that already happened.

Examples:

- item updated;
- playback started;
- playback completed;
- user created;
- library scan completed.

Do not use a broadcast event mechanism for operations requiring a single authoritative result.

## Extension persistence

Extensions must not access the canonical database directly.

Provide a namespaced persistent storage API.

The storage contract should support:

- key/value or structured blobs for simple state;
- transactional semantics where needed;
- quota/usage reporting;
- migration/version metadata for extension-owned state.

If extensions eventually need richer structured persistence, add it deliberately as a platform service rather than exposing raw SQLite/PostgreSQL connections.

## Extension permissions

Permissions are capability based and visible to the administrator.

Examples:

```text
objects.read
objects.write
agents.read
accounts.read
playback.control
network.http-route
network.udp
network.multicast
storage.private
http.outbound
secrets.read:<namespace>
```

High-risk permissions require explicit grants.

## Extension lifecycle

States should include at least:

```text
installed
disabled
planning
active
degraded
failed
upgrading
```

Activation and deactivation must be idempotent.

Provider processes must be supervised and health checked.

## Upgrades

Extension upgrades must validate compatibility before replacement.

Do not silently load an extension against an unsupported service API.

The server should retain enough information to report why an extension is disabled after a Nexa upgrade.

## Frontend extensions

Core React internals are not a stable plugin ABI.

Expose stable contribution points such as:

```text
registerFieldEditor
registerView
registerItemAction
registerAgentAction
registerSettingsPage
registerRoute/page
registerSetupContribution
```

Prefer declarative UI schemas where possible.

For arbitrary UI, give the extension a controlled DOM/mount surface with a versioned bridge rather than direct access to internal React component trees.

<!-- END 06-extension-platform.md -->

---

<!-- BEGIN 07-media-assets-and-transformation.md -->

# Media Assets and Transformation

## Architectural rule

> **Nexa owns the media lifecycle; extensions provide media capabilities.**

Nexa core owns:

- asset identity;
- source resolution;
- media transformation requests;
- provider selection;
- derivative cache keys;
- HTTP delivery;
- resource scheduling;
- persistent representations;
- playback-session integration.

Media providers own mechanics such as decoding, resizing, encoding, remuxing, transcoding, subtitle conversion, and segmentation.

## Media assets

An Asset is a stable Nexa identity for media bytes.

Conceptual properties:

```text
id
kind/media class
source descriptor
source revision
declared/detected MIME/type
size
content hash where available
probe metadata
created/updated timestamps
```

Fields and domain records reference `AssetId`, not cache paths or public URLs.

## Asset sources

An asset may resolve to:

- Nexa-managed blob storage;
- a file in a library/source location;
- extension-owned storage;
- remote content;
- a persistent generated representation.

Asset identity does not imply Nexa copied the entire source into managed storage.

This is essential for large video files.

## Managed blob storage

Default implementation:

```text
/data/blobs/<content-derived-or-managed-layout>
```

Optional implementation:

- S3-compatible object storage.

Expose a `BlobStore`-like internal abstraction.

Where practical, managed blobs should support content hashing and deduplication.

## Media provider capabilities

Providers advertise semantic capabilities, not tool command lines.

Examples:

```text
media.image.decode.jpeg
media.image.decode.heic
media.image.resize
media.image.crop
media.image.encode.webp

media.probe.audio-video
media.remux
media.video.transcode
media.audio.transcode
media.subtitle.convert
media.subtitle.burn
media.segment.hls

codec.decode.hevc
codec.encode.h264.nvenc
codec.encode.h264.software
```

Provider capability descriptions should include constraints such as:

- accepted formats/codecs;
- dimensions;
- alpha/animation support;
- HDR handling;
- hardware device;
- concurrency/cost hints.

## Semantic transformations

Core asks for outcomes.

Example image transformation:

```text
width: 300
height: 450
fit: cover
gravity: center
output: auto
quality: balanced
preserve_alpha: true
preserve_animation: false
```

Example video target:

```text
container: HLS
video codec: H.264
max resolution: 1920x1080
max bitrate: 12 Mbps
HDR policy: tone-map to SDR
audio codec: AAC
channels: 2
subtitle mode: external
```

Providers translate these semantics into libvips/ImageMagick/FFmpeg-specific operations.

Provider-specific knobs may exist in advanced settings but are not part of the baseline semantic contract.

## Stable delivery scaffolding

Core owns stable media routes even if no transformer is installed.

Conceptual routes:

```text
/media/assets/{asset-id}/original
/media/assets/{asset-id}/image/{rendition}
/playback/sessions/{session-id}/...
```

An extension never creates its own ad-hoc thumbnail route for core media.

Removing a provider changes provider availability, not media URL architecture.

## Rendition profiles

Images should normally be requested via named profiles rather than arbitrary dimensions.

Examples:

```text
avatar-xs
avatar-sm
poster-grid
poster-detail
backdrop-small
backdrop-large
logo
```

Profiles define normalized semantic transforms.

Extensions may register additional profiles.

Responsive variants may use constrained buckets such as `@1x`, `@2x`.

Arbitrary transforms, if exposed, require strict authorization/resource limits and must not become an unbounded cache-key generator.

## Format negotiation

Core decides the preferred output based on:

- client `Accept`/capabilities;
- rendition semantics;
- installed provider capabilities;
- administrator policy.

Example preference:

```text
AVIF -> WebP -> JPEG
```

The transformer does not decide browser policy.

## Derivative cache

A derivative is reproducible cache content.

A cache key should incorporate at least:

```text
source revision
normalized transformation recipe
output format
Nexa transform contract revision
provider cache/behavior revision where required
```

Conceptually:

```text
DerivativeKey = hash(...)
```

Cache path example:

```text
/data/cache/media/9f/24/<hash>.avif
```

Changing the source or recipe naturally produces a new key. Old derivatives become garbage-collectable rather than requiring complex invalidation updates.

## Cache separation

Do not store high-frequency cache access bookkeeping in the canonical database.

Suggested data directory:

```text
/data/
├── nexa.db
├── blobs/
└── cache/
    ├── media/
    ├── transcode/
    ├── tmp/
    └── cache.db
```

`cache.db` is disposable.

Hot access metadata may be kept in memory and flushed in batches.

## Single-flight generation

Concurrent requests for the same missing derivative MUST coalesce.

Only one transformation should execute per derivative key per process; other callers await its result.

Future multi-node deployments may extend the same abstraction with distributed leases.

## Work scheduling and priority

Core owns media work priorities.

Suggested classes:

```text
1 interactive playback
2 interactive image derivative
3 playback look-ahead
4 UI prefetch
5 background artwork generation
6 library analysis
7 maintenance
```

Providers advertise capacity/limits.

Core must prevent background thumbnail work from starving active playback.

## Resource limits

Transformation requests must have core-level budgets independent of provider settings:

- maximum source/output dimensions;
- maximum decoded pixels;
- memory budget;
- execution timeout;
- temporary disk budget;
- concurrency;
- maximum generated output size.

ImageMagick-like providers require restrictive policies for untrusted input.

## Native media provider process model

Media providers are supervised child processes.

Control protocol:

```text
Nexa <---- framed versioned messages ----> Provider
```

Expected operations include:

```text
Hello/Handshake
Capabilities
Probe
Transform
StartSession
Seek/Reposition where applicable
Cancel
Progress
Complete
Error
Health
```

Use Protobuf unless superseded by an ADR.

Bulk data travels through controlled files/pipes/streams, not giant Protobuf payloads.

## Cancellation

Every expensive provider operation MUST support cancellation.

Cancellation must terminate or reclaim abandoned FFmpeg/libvips/ImageMagick work within bounded time.

Orphaned media worker processes are unacceptable.

## Persistent representations

Distinguish canonical sources, persistent optimized representations, and ephemeral playback cache.

```text
Original Asset
├── persistent representation: mobile H.264
├── persistent representation: AV1
└── ...

Playback Session
└── ephemeral HLS/transcode segments
```

Persistent representations are first-class metadata and may be selected for later playback.

Ephemeral session outputs are garbage collected.

## Probe metadata

Probe results should be cached against source revision.

Probe information may include:

- container;
- streams;
- codecs/profiles/levels;
- dimensions;
- frame rate;
- sample rate/channels;
- bitrate;
- HDR/color data;
- subtitle formats;
- duration;
- keyframe/seek-relevant metadata.

The public model should not be tied to FFprobe JSON structure.

<!-- END 07-media-assets-and-transformation.md -->

---

<!-- BEGIN 08-playback-architecture.md -->

# Playback Architecture

## Architectural rule

> **The client submits playback intent. Nexa owns the authoritative playback session, sequence, queue, and delivery decisions.**

The client must never be required to construct or maintain the canonical queue.

## Two planners, two questions

Playback is split into:

### Playback Orchestrator

Answers:

> **WHAT should play?**

Responsibilities:

- interpret the requested target and playback intent;
- determine playback context;
- create/manage session state;
- select sequence resolver/generator;
- materialize upcoming entries;
- maintain history;
- apply shuffle/repeat/radio policy;
- apply user queue mutations;
- extend dynamic sequences.

### Media Delivery Planner

Answers:

> **HOW should this playable reach this client?**

Responsibilities:

- choose source/representation;
- direct play vs remux vs transcode;
- choose codecs/container/bitrate/resolution;
- subtitle handling;
- provider selection.

These systems are related but must not be collapsed into one extension contract.

## Playback intent API

A client should send a high-level request such as:

```json
{
  "target": {
    "type": "item",
    "id": "..."
  },
  "mode": "play",
  "inSequence": true
}
```

Optional user choices may include:

```text
shuffle
repeat mode
explicit start position
selected media version
selected audio/subtitle track
```

The exact HTTP schema is defined by OpenAPI.

The client does not send the complete sequence for ordinary contextual playback.

## Playback session

A server-owned session contains conceptually:

```text
session_id
user_id
client_id
playback context
sequence generator type
serialized/durable generator state
history
current entry
materialized future window
explicit queue overlays
shuffle/repeat policy
delivery state
last activity
```

Session persistence requirements may vary by session type, but enough state must survive client reconnect and should survive ordinary server restart where practical.

## Playback context

The same item can have different sequence meaning based on how playback starts.

Examples:

- playing Episode 6 from within a season;
- playing the same episode from a manually ordered playlist;
- playing a track from an album;
- playing the track from "radio from this track";
- playing an item from a saved query.

The context must be explicit in server state.

## Sequence resolvers

Content extensions provide sequence semantics.

A resolver takes:

```text
target
intent
user
library/content context
```

and returns a sequence specification.

Example:

```text
resolver: television
target: episode 6
intent: play in sequence
->
generator: ordered-episodes
context: season/series
start: episode 6
```

Core does not hardcode what "next episode" means.

## Sequence generators

Generators produce chunks of future playables.

Conceptual contract:

```text
next_chunk(context, count) -> [Playable...]
```

Generators may be:

- static;
- ordered children;
- query-backed;
- deterministic shuffle;
- radio/recommendation;
- extension defined.

The entire future sequence need not exist in memory or persistent storage.

## Dynamic queue window

The client receives a bounded view:

```text
previous: 2-5 entries
current: 1 entry
upcoming: N entries
```

As upcoming content is consumed, the server asks the generator for another chunk.

Clients may request more history/upcoming entries for UI, but the server remains authoritative.

## Deterministic randomization

Shuffle/radio generators should use a server-created seed plus serializable generator state.

Benefits:

- reconnect does not reshuffle;
- playback handoff keeps order;
- restart can restore order;
- behavior is testable;
- "why did this play?" diagnostics are possible.

Uniform random shuffle is not the only valid algorithm. Extensions may implement smart shuffle subject to deterministic/stateful contracts.

## Queue overlays

Explicit user queue actions should overlay generated continuation instead of corrupting generator state.

Conceptual effective order:

```text
history
current
play-next overlay
generated continuation
explicit tail additions
```

Operations include:

- Play Next;
- Add to Queue;
- Remove;
- Reorder explicitly queued items;
- Skip;
- Clear explicit queue;
- regenerate/refresh continuation where policy permits.

The semantics of each mutation must be server defined.

## Repeat

Repeat modes should be represented as server policy:

```text
none
current
sequence/context
explicit queue
extension-defined where justified
```

Do not require the client to restart playback manually to implement repeat.

## Client capability report

Clients report facts such as:

- supported containers;
- video/audio codecs;
- codec profiles/levels;
- subtitle formats;
- maximum dimensions/bitrate;
- HDR capabilities;
- direct-streaming constraints.

The capability report is input to the Media Delivery Planner.

A client MUST NOT dictate an encoder command.

## Delivery decision

For each current playable:

```text
playable
+ persistent representations
+ client capabilities
+ network/server policy
+ provider capabilities
= delivery plan
```

Delivery plan classes include:

- direct play;
- remux/direct stream;
- audio-only transcode;
- subtitle conversion;
- video transcode;
- segmented streaming.

## Remux before transcode

If codecs are acceptable and only the container/subtitle packaging is incompatible, the planner should prefer remux/conversion over unnecessary re-encoding.

## Session output ownership

Nexa HTTP owns:

- authorization;
- manifests;
- media URLs;
- range handling;
- playback session lifetime;
- segment delivery;
- accounting;
- remote-access integration.

FFmpeg or another provider is a worker, not the public media web server.

## Seeking

Seeking must be a first-class session operation.

The orchestration/delivery system must:

- map client seek intent to the current playable;
- cancel obsolete look-ahead;
- reposition/restart provider work;
- avoid generating large unused ranges;
- resume segment production around the requested location.

Provider protocols must support cancellation and the relevant reposition semantics.

## Progress and watched state

Clients report playback progress/events.

The server decides:

- when progress is persisted;
- when an item is considered played/completed;
- resume position behavior;
- how watched state affects future generators.

Extensions may contribute content-specific completion thresholds if the platform explicitly exposes such a hook.

## Multi-client handoff

Because session and sequence state are server-owned, future playback handoff should be possible without rebuilding the queue client-side.

A receiving client supplies its capabilities; the Media Delivery Planner may choose a new delivery plan while the Playback Orchestrator preserves sequence state.

## Failure behavior

If a provider fails mid-playback, core may:

- retry with another provider;
- select a different representation;
- restart the same provider;
- terminate the delivery with a structured error.

Provider fallback must not silently change the logical current queue item.

## Observability

A playback session should expose diagnostic state for administrators:

- resolved context;
- generator/provider IDs;
- random seed/state summary where safe;
- current queue window;
- source representation;
- direct/remux/transcode reason;
- selected provider/hardware path;
- bitrate/resolution;
- errors/retries.

<!-- END 08-playback-architecture.md -->

---

<!-- BEGIN 09-accounts-auth-and-authorization.md -->

# Accounts, Authentication, and Authorization

## Core ownership

Nexa core owns the user/account model.

External authentication systems authenticate or provision identities; they do not replace Nexa's internal user object.

A Nexa user may contain:

```text
id
display/profile data
preferences
roles/permissions
playback history
ratings/user data
sessions
linked identities
created/updated state
```

## Separate concepts

Keep these concerns distinct:

### Authentication

Who is the caller?

### Identity linkage/provisioning

Which external identity maps to which Nexa user?

### Authorization

What may the Nexa user do?

Do not let an OIDC/LDAP provider become the authorization engine by accident.

## Native/local authentication

The baseline server must support a local administrator account without requiring any extension or external identity service.

Additional local-user features may remain core or be moved behind providers only if first-run reliability is preserved.

## External identity providers

Extensions may add:

- OpenID Connect;
- LDAP;
- SAML;
- Kerberos;
- passkey/WebAuthn-related integrations;
- other providers.

Provider capabilities may include:

```text
authenticate
identity discovery
directory lookup
group lookup
user provisioning
group-to-role mapping
logout/federation hooks
```

## Linked identities

Conceptually:

```text
user
  └── linked_identity
        provider_id
        provider_subject
        claims/metadata
        linkage timestamps
```

Provider subjects and issuer/provider identity together must be stable and unique.

## Role/group mapping

An extension may map external groups to Nexa roles, e.g.:

```text
LDAP group "media-admins"
  -> Nexa role "Library Administrator"
```

Core authorization still evaluates the final Nexa permissions.

## Authorization model

The model should support at least:

- server administration;
- extension administration;
- library-specific administration;
- library/content read access;
- content modification;
- playback permission;
- user/profile management;
- provider/network configuration.

Permission checks must be performed server-side even if the UI hides inaccessible controls.

## Extension access to account services

Extensions consume a versioned `nexa.accounts`/authorization service rather than database tables.

Permissions constrain what extensions can access.

Examples:

```text
accounts.lookup.basic
accounts.read.groups
accounts.provision
auth.register-provider
authorization.map-external-groups
```

Avoid exposing password hashes, secrets, or session internals.

## Setup administrator

The administrator is created early in the setup wizard.

Before the first admin exists, setup access must be protected:

- localhost-only by default where feasible; and/or
- a one-time setup token printed to the local console/log.

After admin creation, the one-time bootstrap credential is invalidated permanently.

## Sessions

Core owns browser/API sessions.

Session persistence must work with SQLite alone.

External providers may influence authentication, but successful authentication yields a Nexa-owned session/token.

## Secrets

Extension credentials such as OIDC client secrets or API tokens must use a core secrets service, not plaintext extension configuration where avoidable.

The secrets API must enforce namespace and permission boundaries.

Secret values should not be returned to frontend clients after initial submission unless explicitly required.

## Account extension conflicts

Multiple identity providers may coexist.

Login UI/provider selection should be explicit and deterministic.

If a configuration requires one primary provider, this is a SELECT_ONE capability scoped appropriately rather than a load-order convention.

## Remote access identity

A Plex-like Nexa remote-access/DDNS/account extension may introduce an external Nexa service identity while still mapping access to local Nexa users and permissions.

The remote-access extension must not bypass local authorization.

## Auditability

Security-sensitive account operations should produce audit-capable events/logs:

- admin creation;
- identity linking/unlinking;
- role changes;
- provider changes;
- authentication failures/rate limits;
- session revocation;
- extension permission grants.

<!-- END 09-accounts-auth-and-authorization.md -->

---

<!-- BEGIN 10-network-services-and-protocols.md -->

# Network Services and Protocol Extensions

## Purpose

Some extensions need to expose externally visible protocols rather than merely call Nexa core APIs.

Examples:

- DLNA/UPnP;
- OPDS;
- remote-access/DDNS;
- discovery protocols;
- specialized HTTP APIs;
- future casting/control protocols.

Nexa therefore needs a network resource broker, not merely an extension HTTP callback.

## Core owns network resources

Extensions MUST NOT blindly bind arbitrary sockets or mutate the core router.

They request resources from `nexa.network`/`nexa.api`.

Supported resource types may include:

- HTTP route mounts;
- TCP listeners;
- UDP listeners;
- multicast memberships;
- mDNS/DNS-SD advertisements;
- SSDP/discovery integration;
- WebSocket endpoints;
- protocol-specific advertisements.

Core tracks ownership and lifecycle.

## HTTP route model

Extensions receive an automatically safe namespaced route space such as:

```text
/api/extensions/{extension-id}/...
```

for ordinary APIs.

Protocol compatibility may require canonical routes such as:

```text
/opds
```

Those require explicit route claims.

Core route spaces such as these should be reserved:

```text
/api/core/*
/setup/*
/media/*
/playback/*
/admin/*
```

Exact reserved paths are defined by the public route registry.

## Route claim conflicts

Claims must specify enough information to detect overlap before activation.

Examples:

```text
extension A claims /opds/*
extension B claims /opds/admin/*
```

The router/resource planner decides whether the second is nested-compatible or conflicting based on explicit claim rules.

Never allow activation order to decide ownership.

## TCP/UDP claims

A socket resource is identified by properties such as:

```text
address scope
protocol
port
IP family
reuse semantics
multicast group if applicable
```

Two extensions claiming incompatible ownership of the same binding must fail planning before either is activated.

## DLNA/UPnP

A DLNA extension may need:

- SSDP on UDP multicast;
- device/service descriptions;
- event subscriptions;
- HTTP content/control endpoints;
- media delivery URLs.

The extension should reuse Nexa's asset/media delivery services where possible rather than implementing separate transcoding or authorization logic.

Nexa core still owns playback/media policy where requests correspond to Nexa playback semantics.

## OPDS

An OPDS extension primarily projects Nexa objects/libraries into the OPDS protocol and claims appropriate HTTP routes.

It consumes:

- query/object services;
- account/authorization services;
- media assets;
- optional search.

It must not read database tables directly.

## Remote access / DDNS

A remote-access provider may provide:

- server registration;
- account linking;
- DDNS/subdomain assignment;
- TLS/tunnel integration;
- reachability testing;
- discovery.

The setup wizard can add extension-provided configuration steps only when such a provider is installed/enabled.

## TLS and reverse proxies

Baseline Nexa should work directly on a local HTTP endpoint.

Advanced deployment may place a reverse proxy in front of Nexa.

Extension network APIs must not assume that public URLs are equivalent to local listener addresses.

Core should provide canonical external/base URL resolution and trusted-proxy configuration.

## Discovery

Discovery resources should be brokered to avoid duplicate announcements, port collisions, or inconsistent server identity.

A stable Nexa instance ID should be used where protocols require durable identity.

## Network extension permissions

Network claims are privileged.

Permissions may include:

```text
network.http-route
network.tcp
network.udp
network.multicast
network.discovery
network.public-advertisement
```

The administrator must be able to inspect active listeners and which extension owns each one.

## Diagnostics

Expose an administrator-facing network diagnostics view:

```text
listener
protocol
address/port
owning extension
status
last error
advertisement state
```

This is especially important on home networks where firewall and multicast behavior are difficult to debug.

## Lifecycle

On extension disable/upgrade/failure:

- stop accepting new connections;
- release listeners/multicast memberships;
- unregister advertisements;
- drain/cancel in-flight work according to the protocol contract;
- update diagnostics.

A failed network extension must not leave hidden background listeners running.

<!-- END 10-network-services-and-protocols.md -->

---

<!-- BEGIN 11-setup-wizard-and-deployment.md -->

# Setup Wizard and Deployment

## Product invariant

A fresh Nexa instance MUST complete a mandatory setup wizard before exposing the normal application.

The wizard is a stateful server bootstrap workflow, not a client-side tour.

## Bootstrap states

Conceptually:

```text
UNINITIALIZED
    ↓
SETUP_IN_PROGRESS
    ↓
SETUP_COMPLETE
```

Failure/restart never implies completion.

Canonical state may include:

```text
instance_id
setup_state
setup_revision
setup_started_at
setup_completed_at
completed step records
extension setup state
```

## Resumability

Every setup step MUST be idempotent or explicitly resumable.

Closing the browser, losing power, restarting Docker, or an extension setup failure must return the administrator to a valid state.

Do not store setup progress only in browser local storage.

## Suggested setup flow

The exact UX may evolve, but the architecture should support:

1. Welcome/server identity
2. Create administrator
3. Extension selection/install
4. Extension-contributed configuration
5. Library creation
6. Library/content-specific setup
7. Optional account/auth integrations
8. Optional remote access/DDNS
9. Review/diagnostics
10. Complete setup

The first administrator should be created early so later setup is authenticated.

## Pre-admin bootstrap protection

Before an admin exists:

- bind setup locally by default where practical;
- and/or issue a high-entropy one-time setup token to the console.

The token is invalidated permanently once the administrator exists.

## Extension-contributed steps

Extensions may contribute setup steps with ordering constraints.

Conceptual declaration:

```text
step ID
extension/provider ID
after/before constraints
required/optional
form/schema or custom UI contribution
completion probe
```

The setup coordinator builds a dependency/order graph.

A step cannot secretly bypass setup completion rules.

## Declarative forms

Prefer schema-driven forms for:

- provider language/region;
- API credentials;
- folder selection metadata;
- login endpoints;
- simple options.

Use custom executable/frontend UI only when declarative forms cannot express the required interaction.

## Extension recommendations

Library/content extensions can declare recommended capabilities.

Example:

```text
Movies extension
  recommends image.transform
  recommends media.probe
  recommends video.playback
```

The wizard may display friendly product names such as:

```text
Nexa Image Processing (libvips)
Nexa Video Playback (FFmpeg)
```

rather than requiring users to understand implementation engines.

Advanced users may choose alternate providers/fallback order.

## Library templates

Extensions register recommended library templates.

The wizard can offer:

```text
Movies
Television
Music
Games
Books
Comics
```

without those options being hardcoded into core.

Users may accept recommended fields/views or customize them before creation.

## Remote access

Remote-access/DDNS setup appears only when an appropriate extension is installed/enabled.

Core provides setup contribution points and server identity/network services; the extension owns provider-specific workflow.

## Baseline deployment

Normal data layout should resemble:

```text
nexa executable

/data/
├── nexa.db
├── config/
├── blobs/
├── extensions/
├── extension-state/
└── cache/
```

Exact OS-specific data directory defaults should follow platform conventions.

## Single-binary goal

The production binary should embed:

- web UI assets;
- migrations;
- core schemas/default resources;
- OpenAPI/static support assets where useful.

Official extension packages may be bundled with installers/distributions while remaining architecturally external extensions.

This permits setup without depending on marketplace availability.

## Windows

The distribution should support:

- interactive first launch;
- optional Windows Service installation;
- predictable data directory;
- clean upgrade/uninstall behavior that does not delete user data by default.

## Linux

Support:

- standalone binary;
- systemd service package;
- container image;
- standard writable data directory.

## macOS

Support a normal server binary/package. A lightweight application wrapper/menu-bar control is optional product UX, not a core architectural dependency.

## Container

Baseline container use should require one service:

```yaml
services:
  nexa:
    image: nexa/nexa
    ports:
      - "8321:8321"
    volumes:
      - ./nexa:/data
```

No required Compose stack of database/cache/search services.

## Advanced deployment

Optional settings may enable:

- PostgreSQL;
- S3-compatible blob storage;
- external reverse proxy;
- external identity;
- future worker separation.

Advanced components must be configured through stable adapters and must not change domain semantics.

## Upgrade model

Server upgrades should:

1. validate data-directory compatibility;
2. acquire an upgrade lock;
3. back up or require backup according to migration policy;
4. run core migrations;
5. validate extension compatibility;
6. mark incompatible extensions disabled/degraded rather than loading unsafely;
7. start services;
8. expose actionable diagnostics.

## Recovery

The product should support clear recovery from:

- interrupted DB migration;
- incompatible extension;
- failed cache;
- missing media provider;
- extension state corruption;
- unavailable library roots.

A missing disposable cache is never a fatal recovery condition.

## Portability

Where possible, a Nexa instance should be movable by preserving:

- canonical DB;
- managed blobs;
- config/secrets through supported backup/export;
- extension packages/state.

Source library paths may require remapping across hosts.

<!-- END 11-setup-wizard-and-deployment.md -->

---

<!-- BEGIN 12-api-and-client-contracts.md -->

# API and Client Contracts

## API philosophy

Nexa exposes stable, versioned contracts. Internal Go structs are not the API.

The public HTTP API is described with OpenAPI 3.1.

The extension Core Service API is separately versioned and may use RPC/message contracts appropriate to its execution boundary.

## Public API categories

Conceptual API groups:

```text
/setup
/session/auth
/users
/libraries
/objects
/agents
/fields
/views
/query
/media
/playback
/extensions
/admin
```

Actual path names/versioning are defined in the OpenAPI spec.

## API versioning

Breaking public API changes require an explicit versioning strategy.

Do not put unstable internal implementation details into v1 merely because they are convenient.

Prefer opaque IDs and semantic models.

## OpenAPI workflow

The OpenAPI specification should be reviewed as source code.

Generated Go and TypeScript bindings should be reproducible.

CI verifies generated code is up to date.

Do not hand-edit generated files.

## Query endpoint

Complex queries should use a JSON body, e.g.:

```text
POST /api/v1/libraries/{id}/query
```

rather than encoding an unbounded AST into query-string conventions.

Read-only semantic operations may still use POST where the request body is a complex query plan.

## Media URLs

Media delivery routes are stable core resources.

They must enforce authorization server-side.

Cache-friendly derivative URLs may be immutable/content-keyed internally, but callers should normally use stable semantic asset/rendition APIs.

## Playback APIs

Clients use intent-oriented operations:

```text
start playback
get session/window
play next
add to queue
skip
seek
pause/resume state where server coordination requires it
select track/version
update capability profile
report progress
end session
```

The client never sends the canonical complete queue for contextual dynamic playback.

## Realtime updates

WebSocket or another server-push mechanism may be used for:

- playback session updates;
- job progress;
- extension state;
- library scan updates;
- notifications.

Realtime transport is not the sole persistence mechanism. Clients reconnect by re-reading authoritative server state.

## Client capability profile

The player reports a structured capability profile.

Capabilities should be semantic, not browser user-agent guesses alone.

Example dimensions:

```text
containers
video codecs/profiles/levels
audio codecs
subtitle formats
HDR/color capabilities
max dimensions
bitrate constraints
streaming/seek constraints
```

Native clients may persist named device profiles server-side.

## Error model

Public APIs need a consistent structured error envelope containing:

```text
stable error code
human-readable summary
optional details
correlation/request ID
field validation errors where relevant
retryability where useful
```

Do not expose raw SQL/provider stack traces.

## Pagination

Large collections and queries must be paginated.

Prefer cursor-based pagination where ordering can be made stable. Offset pagination may be acceptable for selected UI cases.

Pagination semantics must be identical enough across SQLite/PostgreSQL for clients not to branch on backend.

## Optimistic concurrency

Mutable resources should carry a revision/version where concurrent edits matter.

APIs may use:

- revision fields;
- ETags/If-Match;
- another explicit optimistic concurrency token.

Silent last-write-wins should not be the universal mutation policy.

## Idempotency

Operations that may be retried safely should provide idempotency where needed, especially:

- setup mutations;
- extension install/enable transitions;
- selected job submissions;
- remote-provider callbacks.

## Frontend architecture

The React frontend is a Nexa API client, not a privileged bypass.

It uses the same authorization and server semantics expected of future native clients.

Frontend code should not recreate server policy for:

- playback sequence;
- permissions;
- query semantics;
- media transcode decisions;
- extension conflict resolution.

## Generated API clients

Generated TypeScript code should be wrapped in thin domain-specific client modules rather than leaking generator-specific APIs throughout the UI.

This makes generator replacement feasible.

## Extension HTTP APIs

Ordinary extension HTTP endpoints live under an extension namespace.

Protocol extensions may claim canonical paths through the network/route broker.

Extension APIs must use core auth/authorization facilities unless the protocol explicitly requires another authentication model approved by the platform.

## API observability

Every API request should receive/carry a correlation ID.

Diagnostics should identify:

- route owner (core or extension);
- authenticated user where safe;
- duration/status;
- playback/job IDs where applicable.

<!-- END 12-api-and-client-contracts.md -->

---

<!-- BEGIN 13-jobs-events-observability.md -->

# Jobs, Events, and Observability

## Baseline requirement

Nexa must run background work without Redis or an external queue.

The default job scheduler and workers run inside the Nexa server process and persist canonical job state in the active Nexa database.

## Job model

Conceptual job fields:

```text
id
kind
owner/core-or-extension
payload/reference
status
priority
created_at
run_at
attempts
max_attempts
locked/claimed state
progress
last_error
cancellation state
```

Do not put unbounded binary payloads into job rows.

## Example job types

- library scans;
- metadata refresh;
- artwork retrieval;
- thumbnail pre-generation;
- media probe;
- optimized representation generation;
- exports;
- extension work;
- cleanup/garbage collection;
- index rebuild;
- remote-provider synchronization.

## SQLite behavior

The SQLite scheduler must account for a single-writer model.

Claim/update transactions should be short.

Workers may run concurrently after work is claimed.

Do not hold a write transaction throughout job execution.

## PostgreSQL behavior

PostgreSQL may use backend-native efficient claiming/locking strategies, while preserving the same job semantics.

## Priority

Core priority classes must integrate media work and general background work.

Interactive playback/media has precedence over background maintenance.

An extension must not be able to declare every task highest priority.

## Retry

Retry policy includes:

- retryable vs terminal error classification;
- bounded exponential backoff;
- maximum attempts;
- optional dead-letter/failed state;
- administrator retry action.

Jobs involving external side effects need idempotency safeguards.

## Cancellation

Long-running jobs must receive cancellation signals where feasible.

Providers/extensions must cooperate with cancellation.

## Scheduling

Support:

- immediate;
- delayed (`run_at`);
- recurring/scheduled jobs where needed.

Avoid implementing a second unrelated scheduler inside extensions.

## Events

The core event system announces domain/runtime facts.

Examples:

```text
object.created
object.updated
agent.updated
library.scan.started
library.scan.completed
playback.started
playback.completed
user.created
extension.activated
```

Event IDs are namespaced and versioned when payload compatibility requires it.

## Events are not commands

Events announce facts. They are not a mechanism for obtaining an authoritative answer.

If Nexa needs a result from an extension, use a provider/service contract.

## Delivery semantics

The baseline event system may provide at-least-once delivery for durable extension subscriptions if durability is required.

Event consumers must be designed for duplicate delivery where applicable.

Not every high-frequency internal signal needs durable persistence.

Distinguish:

- durable domain events;
- transient runtime notifications;
- UI progress messages.

## Extension jobs/events

Extensions use core services to schedule work and subscribe/publish within their permissions.

Extension shutdown must stop new job dispatch to the extension and handle in-flight tasks according to lifecycle policy.

## Structured logging

Use `log/slog`.

Every subsystem should log machine-readable fields rather than parsing prose.

Common fields:

```text
request_id
extension_id
provider_id
job_id
playback_session_id
library_id
user_id where appropriate
operation
duration
error_code
```

Never log secrets, credential material, access tokens, or raw passwords.

## Metrics

Expose optional internal metrics for:

- HTTP request latency/status;
- active playback sessions;
- direct/remux/transcode counts;
- provider utilization;
- transcode/derivative queue depth;
- job queue depth/latency;
- SQLite busy/transaction latency;
- extension failures;
- cache hits/misses/size;
- library scan throughput.

The baseline UI may consume an internal admin diagnostics API. Prometheus/OpenTelemetry export can be optional.

## Tracing

Distributed tracing is not a baseline deployment dependency.

Internal trace/correlation IDs should still connect:

```text
HTTP request
-> playback session
-> media plan
-> provider job
```

Optional OpenTelemetry export may be added behind configuration/extension.

## Health

Health is layered:

### Process health

Can Nexa serve?

### Readiness

Are canonical storage, migrations, and mandatory core services usable?

### Extension/provider health

Which optional capabilities are degraded?

A failed optional provider should not mark the entire server unavailable unless a configured critical dependency requires it.

## Admin diagnostics

Provide a diagnostics surface summarizing:

- DB backend/status;
- storage roots;
- active extensions;
- extension conflicts/failures;
- network listeners;
- provider capabilities;
- media work/resource usage;
- job failures;
- playback delivery reasons;
- cache status.

Diagnostics should prefer actionable explanations over raw stack traces.

<!-- END 13-jobs-events-observability.md -->

---

<!-- BEGIN 14-security.md -->

# Security and Trust Boundaries

## Security model

Nexa is a server that:

- reads potentially hostile media and metadata;
- exposes network services;
- executes third-party extensions;
- invokes complex native media tools;
- stores user credentials/tokens;
- may be exposed remotely.

Security boundaries must be explicit.

## Trust zones

```text
Untrusted clients
      │
      ▼
HTTP/API authorization boundary
      │
      ▼
Nexa core
  │       │        │
  │       │        └── secrets/account store
  │       │
  │       └── sandboxed WASM extensions
  │
  └── supervised native providers/extensions
             │
             └── untrusted media inputs
```

## Setup security

Unconfigured servers are vulnerable to administrator takeover.

Before first-admin creation:

- setup should be localhost-only by default where practical and/or require a one-time bootstrap token;
- token entropy must be sufficient;
- token expiration/reissue policy must be explicit;
- normal APIs are unavailable.

After first-admin creation, bootstrap credentials are invalid.

## Authentication

- password storage uses a modern password hashing scheme with safe parameters;
- authentication endpoints are rate-limited;
- external provider callbacks validate issuer/state/nonce as relevant;
- sessions/tokens are revocable;
- sensitive cookies use appropriate `HttpOnly`, `Secure`, and SameSite settings depending on deployment.

Exact crypto libraries/parameters require a security-focused ADR/review at implementation time.

## Authorization

Authorization is server-side and deny-by-default for protected resources.

Every resource access path must account for:

- user identity;
- library/object access;
- administrative scope;
- extension ownership where relevant.

Media URLs are not authorization bypasses.

## CSRF/CORS

Browser APIs need an explicit same-origin/CORS policy.

State-changing cookie-authenticated requests need CSRF protection appropriate to the session model.

Do not enable permissive `*` CORS as a convenience default.

## Extension sandboxing

WASM extensions receive only granted host functions/capabilities.

No implicit access to:

- server filesystem;
- arbitrary environment variables;
- database;
- network;
- secrets;
- other extensions.

Outbound HTTP is brokered and permission controlled.

## Native provider isolation

Native media providers are privileged relative to WASM and require extra controls.

Where practical:

- run as child processes with minimal environment;
- expose only controlled input/output resources;
- restrict filesystem visibility;
- apply OS sandboxing where feasible per platform;
- set time/memory/process limits;
- kill provider process trees on cancellation/timeout.

The architecture must not rely on providers being memory-safe.

## Untrusted media

All media and metadata are untrusted input.

Protect against:

- decompression bombs;
- parser bugs;
- malformed containers;
- path traversal;
- unsafe ImageMagick delegates/coders;
- malicious archive-like formats;
- huge dimension/resource requests;
- command injection into FFmpeg/ImageMagick arguments.

Provider commands MUST be constructed from structured arguments, never concatenated shell strings.

## Image transformation limits

Core enforces:

- maximum source/output dimensions;
- decoded-pixel limits;
- memory;
- temporary disk;
- runtime;
- output size.

ImageMagick providers must use a restrictive server-side security policy.

## Cache abuse

Named/bucketed rendition profiles are the default.

Do not allow unauthenticated callers to generate arbitrary unique dimension/quality combinations.

Rate/complexity-limit custom transformation endpoints.

## Query abuse

Validate and limit query AST complexity:

- maximum nesting;
- number of predicates;
- relation depth;
- sort/group limits;
- page size;
- expensive full-text/regex operators.

Extensions cannot inject raw SQL.

## Filesystem paths

Library roots are configured capabilities.

Normalize and validate all paths.

Do not serve arbitrary server filesystem paths because a client supplied them.

Symlink policy must be explicit and tested.

## SSRF

Metadata/remote-fetch extensions may cause server-side HTTP requests.

The outbound HTTP broker should support policy controls against:

- loopback/internal network access where inappropriate;
- cloud metadata endpoints;
- disallowed schemes;
- DNS rebinding risks;
- unbounded redirects/download sizes.

Some trusted integrations may require private-network access and should request that permission explicitly.

## Secrets

Use a core secret store/service.

Rules:

- secrets are namespaced;
- extensions see only granted secrets;
- secrets are redacted from logs/errors;
- frontend reads do not return stored secret values by default;
- backup/export semantics are explicit.

## Extension provenance

The distribution system should support package integrity and signature verification.

Whether third-party unsigned extensions are allowed is a product-policy decision, but the administrator must be able to distinguish trust/provenance.

## Dependency security

CI should include:

- Go vulnerability scanning;
- frontend dependency auditing;
- extension/provider artifact verification;
- reproducible dependency lock/update review.

Avoid dynamically downloading executable provider binaries without integrity verification.

## Audit events

Security-sensitive changes should be auditable:

- admin/user creation;
- role/permission changes;
- extension install/enable/permission grant;
- auth provider changes;
- remote-access changes;
- network listener claims;
- secret rotation where metadata can be logged safely.

<!-- END 14-security.md -->

---

<!-- BEGIN 15-testing-and-compatibility.md -->

# Testing and Compatibility

## Testing philosophy

Architecture contracts are tested behavior, not prose alone.

The suite should strongly favor black-box and contract tests around boundaries that must remain stable across:

- SQLite/PostgreSQL;
- extension implementations;
- client types;
- media providers;
- operating systems.

## Test layers

### Unit tests

For:

- domain validation;
- query normalization;
- sequence generator logic;
- conflict algorithms;
- cache-key generation;
- provider selection;
- permission evaluation.

### Integration tests

For:

- SQLite repositories/migrations;
- PostgreSQL repositories/migrations;
- HTTP API;
- extension runtime;
- provider IPC;
- setup state machine;
- job scheduler.

### Contract tests

Every provider/service interface gets a reusable conformance suite.

Examples:

```text
DatabaseBackendContract
BlobStoreContract
AuthProviderContract
SequenceGeneratorContract
MediaProviderContract
NetworkClaimContract
ExtensionLifecycleContract
```

Third-party implementations should be able to run the same tests where possible.

### End-to-end tests

Cover:

- fresh first-run setup;
- extension installation;
- library creation;
- indexing;
- poster derivative generation;
- direct playback;
- transcode playback;
- reconnect/seek;
- provider failure/fallback;
- upgrades.

## Database parity

Every semantically supported query operation must be tested against SQLite and PostgreSQL.

The test should assert Nexa results, not identical SQL.

Run randomized/property-style query fixtures where useful.

## SQLite stress tests

Test:

- concurrent readers/writes;
- long scans plus interactive writes;
- import chunking;
- busy timeout behavior;
- crash/restart during jobs;
- WAL checkpoint/recovery;
- large libraries.

SQLite is not a "small test backend"; production-scale tests are mandatory.

## Playback determinism tests

Given:

```text
content set
playback context
generator seed
generator state
user mutations
```

the resulting logical sequence must be reproducible.

Test:

- reconnect;
- restart serialization;
- chunk extension;
- shuffle;
- Play Next overlays;
- explicit tail queue;
- sequence exhaustion;
- extension update compatibility where state format changes.

## Media planner tests

Use fixture media descriptors rather than requiring real encoding for every planner test.

Assert reasons:

```text
direct play because all streams supported
remux because container unsupported
transcode because video codec unsupported
subtitle convert/burn reason
representation preference
```

A smaller end-to-end corpus verifies actual FFmpeg/provider behavior.

## Cache tests

Derivative keys must change when and only when relevant inputs change:

- source revision;
- normalized transform;
- output format;
- provider behavior revision.

Test concurrent single-flight generation.

Test deletion of the entire cache directory followed by recovery.

## Extension conflict tests

Create fixtures for:

- duplicate namespaced IDs;
- unsatisfied dependencies;
- incompatible service versions;
- multiple SELECT_ONE providers;
- route overlap;
- TCP/UDP port conflicts;
- capability ordering;
- enable/disable/upgrade transitions.

Loading order must not affect the result.

## Extension security tests

Test denial of ungranted:

- filesystem;
- network;
- secrets;
- object writes;
- network claims.

Fuzz host-call/message decoding boundaries.

## Fuzzing

High-value fuzz targets:

- query AST decoding/validation;
- extension manifests;
- provider protocol messages;
- media metadata parser adapters;
- route/resource claims;
- API request models;
- migration/version metadata.

Use Go fuzzing where appropriate.

## API compatibility

Maintain generated clients against the published OpenAPI contract.

Breaking changes require version policy.

Snapshot/golden tests may be used for schemas but should not become brittle substitutes for semantic tests.

## Migration tests

For each supported historical release:

```text
old DB fixture
-> upgrade
-> validate schema/data
-> run core behavior tests
```

Test interrupted/failed migration paths where feasible.

## Cross-platform CI

At minimum:

- Linux amd64;
- Windows amd64;
- macOS arm64.

Add Linux arm64 early because home-server/NAS usage is important.

Provider capability tests may be platform-specific, but core behavior must remain portable.

## Static analysis

CI should include:

- `go test`;
- `go vet`;
- formatting;
- vulnerability scanning;
- frontend lint/typecheck/test;
- generated-code consistency;
- dependency/license policy checks.

Additional linters should be adopted deliberately; avoid a giant unstable lint bundle whose noise obscures defects.

## Performance tests

Benchmark architecture-sensitive paths:

- large query filtering/sorting;
- typed field lookup;
- SQLite write contention;
- library scan ingestion;
- derivative cache hit/miss;
- streaming overhead;
- playback session fanout;
- extension RPC overhead.

Performance budgets should be written after baseline measurements rather than invented without data.

## Failure injection

Test failures such as:

- provider process crash;
- killed Nexa during setup;
- killed Nexa during DB write;
- full cache disk;
- unavailable library mount;
- external metadata timeout;
- extension activation failure;
- PostgreSQL unavailable;
- corrupt cache database.

Canonical data safety takes precedence over automatic recovery tricks.

<!-- END 15-testing-and-compatibility.md -->

---

<!-- BEGIN 16-implementation-roadmap.md -->

# Implementation Roadmap

This roadmap orders work to validate irreversible architectural boundaries early. It is not a sprint plan.

## Phase 0: Repository and contracts

Establish:

- Go module/workspace;
- frontend project;
- CI;
- formatting/testing;
- OpenAPI generation skeleton;
- documentation/ADR process;
- stable ID utility conventions;
- error model;
- logging/correlation conventions.

Deliverable: server boots, embedded UI loads, test pipeline works.

## Phase 1: Bootstrap + SQLite foundation

Implement:

- data directory;
- SQLite adapter and migrations;
- instance state;
- setup state machine;
- local admin creation;
- authentication/session baseline;
- mandatory setup gate;
- graceful startup/shutdown.

Prove:

- clean first launch;
- interrupted setup resumes;
- normal application is inaccessible before completion.

## Phase 2: Core domain primitives

Implement:

- libraries/hubs;
- items/objects;
- field definitions;
- typed field values;
- agents;
- agent names;
- roles/credits;
- relationships;
- basic views;
- stable IDs/revisions.

Prove no schema migration is required to add content-specific fields or agent kinds.

## Phase 3: Query engine

Implement:

- query AST;
- validator;
- SQLite compiler;
- typed indexes;
- FTS5;
- pagination;
- authorization integration.

Build a substantial contract suite before adding PostgreSQL.

## Phase 4: Extension manager + capability broker

Implement first with safe in-process test providers and/or WASM:

- manifest;
- capability registry;
- service version negotiation;
- dependency graph;
- cardinality/scope;
- conflict planner;
- permissions;
- extension state;
- lifecycle;
- extension private storage.

Prove deterministic conflict outcomes.

## Phase 5: Extension-aware setup

Implement:

- extension selection/install;
- declarative setup contributions;
- dependency ordering;
- recommended capability providers;
- extension-provided library templates.

At this point a content extension can create a useful library without core knowing the media type.

## Phase 6: Media asset scaffolding

Before transcoding:

- Asset IDs;
- source resolvers;
- managed blobs;
- asset authorization;
- stable media routes;
- rendition profiles;
- derivative cache;
- cache keys;
- single-flight;
- cache GC;
- media job priority framework.

Use a simple test transformer provider first.

## Phase 7: Native provider protocol

Implement:

- provider supervisor;
- handshake/versioning;
- capability advertisement;
- transform/probe operations;
- progress;
- cancellation/timeouts;
- file/pipe bulk transfer;
- crash recovery.

Then implement official libvips provider.

Optional ImageMagick provider can validate multi-provider/fallback behavior.

## Phase 8: Server-authoritative playback

Implement logical playback before FFmpeg complexity:

- PlaybackSession;
- playback intent;
- context;
- sequence resolvers;
- static/ordered generators;
- deterministic shuffle;
- queue windows;
- overlays;
- repeat;
- reconnect/state persistence;
- client capability profiles.

Use directly playable test files initially.

## Phase 9: Media delivery planning + FFmpeg

Implement:

- probe descriptors;
- representation model;
- direct play;
- remux;
- transcode planning;
- subtitle planning;
- FFmpeg provider;
- segmented delivery;
- seek/cancel;
- provider fallback;
- playback diagnostics.

Ensure core HTTP remains the public delivery layer.

## Phase 10: Auth extension contracts

Implement:

- account/auth service contract;
- linked external identities;
- provider login contribution;
- directory/group mapping;
- secrets service.

Build OIDC first as a reference provider; LDAP can validate directory semantics.

## Phase 11: Network service broker

Implement:

- route claims;
- TCP/UDP claims;
- multicast/discovery claims;
- diagnostics;
- conflict detection/lifecycle.

Build an OPDS extension as an HTTP-focused reference and DLNA as the more demanding network-protocol validation.

## Phase 12: PostgreSQL backend

Only after SQLite semantics and query contracts are stable:

- PostgreSQL repositories;
- query compiler;
- job claiming;
- backend parity suite;
- migration/installation switching strategy.

Do not use PostgreSQL to change public semantics.

## Phase 13: Advanced deployment/providers

As requirements justify:

- S3-compatible blob store;
- remote-access/DDNS extension;
- optional external observability;
- optimized media generation;
- separate workers/multi-process scaling.

## Architectural checkpoints

Before moving past each relevant phase, review:

### Before Phase 4

Are domain concepts independent of extension implementation?

### Before Phase 6

Can assets be referred to without URLs/cache paths?

### Before Phase 8

Is playback sequencing entirely server-owned?

### Before Phase 9

Can media providers be replaced without changing HTTP/client contracts?

### Before Phase 11

Can extensions consume core services without internal package/database access?

### Before v1 API freeze

Are all extension-visible IDs, services, errors, and capability semantics versioned and documented?

## What not to implement early

Avoid prematurely adding:

- microservices;
- Redis;
- Elasticsearch/Meilisearch;
- distributed locks;
- multi-node playback;
- arbitrary in-process native plugins;
- generic workflow engines;
- enormous plugin UI frameworks;
- database abstraction features without a real second-backend need.

Preserve extension points, but let observed requirements drive advanced implementations.

<!-- END 16-implementation-roadmap.md -->

---

<!-- BEGIN OPEN_QUESTIONS.md -->

# Open Questions

These items are intentionally not settled by the current architecture. Implementation agents should not silently invent public behavior for them. Resolve them through design review/ADR when they become blocking.

## Domain naming: Hub vs Library

The existing design uses both concepts.

Questions:

- Is `Hub` the internal generalized concept and `Library` one presentation/type?
- Should the entire public model standardize on `Library`?
- Are there non-library Hubs that justify keeping both?

Resolve before public API v1 naming freezes.

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

<!-- END OPEN_QUESTIONS.md -->

---

<!-- BEGIN GLOSSARY.md -->

# Glossary

## Agent

A generalized entity that can participate in credits or relationships. Examples include an individual person, group, company, studio, label, publisher, band, orchestra, or extension-defined kind.

## Agent Kind

An extensible semantic classification for an Agent, such as `core.person`, `core.organization`, `music.band`, or `games.publisher`.

## Asset / Media Asset

A stable Nexa identity referring to a source of media bytes. An asset may point to managed blob storage, a library file, a remote source, or another storage provider.

## Capability

A named, versioned function or service an extension provides or consumes.

## Capability Broker

The core subsystem that discovers capabilities, validates versions and permissions, selects providers, resolves ordering, detects conflicts, and manages extension activation.

## Credit

A relationship between an Item/Object and an Agent with a role, optional credited-as name, and ordering metadata.

## Derivative

A reproducible transformed representation of an asset, normally stored in disposable cache storage. Examples: a 240x360 AVIF poster or a resized avatar.

## Extension

A package that adds semantics, capabilities, UI, integrations, protocols, or providers to Nexa through supported extension contracts.

## Hub / Library

A configured collection/domain in Nexa. "Library" is the common end-user term; "Hub" may be used for the more general programmable concept where appropriate. The implementation should converge on one canonical internal term before the public API stabilizes.

## Item / Object

A content record managed by a Hub/Library. The architecture may generalize Items into a wider Object model, but the exact hierarchy remains an open modeling decision.

## Media Delivery Planner

The component that decides HOW a specific playable should be delivered: direct play, remux, transcode, representation selection, subtitles, bitrate/resolution, etc.

## Media Provider

An extension/provider capable of probing or transforming media. Examples: FFmpeg, libvips, ImageMagick.

## Playback Orchestrator

The server component that decides WHAT should play, maintains authoritative session/queue state, invokes sequence generators, and applies queue mutations.

## Playback Session

A server-owned runtime/persistable state object representing an active playback context for a user/client.

## Representation

A persistent, intentional alternate media form associated with an asset, as opposed to disposable cache. Example: a pre-optimized mobile H.264 copy.

## Rendition Profile

A named semantic image/media transformation profile such as `poster-grid`, `avatar-sm`, or `backdrop-large`.

## Sequence Generator

A server-side stateful or deterministic generator that supplies additional playables for a playback session, such as ordered episodes, album order, shuffle, radio, or extension-defined logic.

## Setup Provider / Setup Step

Core or extension-defined work exposed in the mandatory first-run setup flow.

## Source Revision

A stable value that changes when the bytes or material identity of an Asset's source changes. Used in derivative cache keys and invalidation.

## Typed EAV

A constrained entity-attribute-value model in which custom field values are stored in typed columns/tables, preserving efficient typed indexing while allowing runtime-defined fields.

<!-- END GLOSSARY.md -->

---

<!-- BEGIN specs/extension-manifest.md -->

# Extension Manifest Contract — Draft Specification

Status: Draft; intended to guide implementation.

## Goals

The manifest must allow Nexa to reason about an extension before executing its code.

The manifest therefore describes:

- identity/version;
- Nexa compatibility;
- execution artifacts;
- provided capabilities;
- required/recommended capabilities;
- permissions;
- resource claims;
- setup contributions;
- configuration schemas;
- conflicts/replacements.

## Example shape

Illustrative TOML:

```toml
id = "org.example.movies"
version = "1.0.0"
name = "Movies"
nexa = ">=1.0 <2.0"

[execution]
kind = "wasm"
entrypoint = "wasm/extension.wasm"

[[provides]]
capability = "library.template"
id = "org.example.movies.library"
version = 1
cardinality = "multi"

[[requires]]
capability = "media.image.transform"
version = ">=1"
optional = true
recommended = true

[[permissions]]
id = "objects.read"

[[permissions]]
id = "objects.write"

[[setup.steps]]
id = "metadata"
after = ["core.extensions"]
before = ["core.libraries"]
required = false
schema = "schemas/setup-metadata.json"

[[claims.http]]
path = "/example-protocol"
mode = "exclusive"
```

The final syntax may change; semantics should not.

## Identity

`id` MUST be globally namespaced and stable.

`version` MUST use a deterministic comparable version scheme, expected to be semantic versioning unless an ADR says otherwise.

Display name is not identity.

## Compatibility

Manifest declares a Nexa compatibility range and extension-service capability versions.

Nexa MUST reject activation when mandatory compatibility is unsatisfied.

## Artifacts

Platform-specific native artifacts identify:

```text
os
architecture
optional libc/runtime constraints
path
hash
signature/provenance metadata
```

Never select binaries by untrusted filename convention alone.

## Provided capabilities

Each provided capability includes:

```text
capability ID
provider instance ID if needed
contract version
cardinality
scope
priority/default hints
configuration schema
```

Cardinality values:

```text
MULTI
ORDERED
SELECT_ONE
EXCLUSIVE
```

## Requirements

Requirements distinguish:

- required;
- optional;
- recommended.

A recommended missing capability may be surfaced in setup without blocking activation.

## Permissions

Permissions are explicit and install/enable UI should be able to explain them.

Permissions do not become granted simply because an extension asks for them.

## Resource claims

Claims are resources Nexa must reserve before activation, e.g.:

- HTTP routes;
- TCP ports;
- UDP ports;
- multicast groups;
- singleton semantic resources.

Claims are validated globally/scoped before executing extension startup.

## Setup steps

Steps have:

```text
stable step ID
ordering constraints
required flag
schema/custom UI contribution
completion validation
```

Cyclic setup ordering is an activation/configuration error.

## Configuration

Settings are schema described and namespaced.

Secrets are referenced through the core secrets service rather than represented directly in ordinary readable config after storage.

## Manifest parsing

Manifest parsing is untrusted-input parsing.

Requirements:

- strict schema;
- bounded sizes;
- reject duplicate keys/IDs;
- reject unknown security-sensitive enum values;
- normalize before conflict planning;
- do not execute extension code merely to discover static claims.

Dynamic claims may be supported only after static sandboxed preflight and must still pass the same planner.

## Manifest evolution

The package/manifest format itself is versioned separately from Nexa server version.

Unknown future mandatory manifest features cause a clear incompatibility error rather than partial activation.

<!-- END specs/extension-manifest.md -->

---

<!-- BEGIN specs/capability-model.md -->

# Capability Model — Draft Specification

## Capability identity

A capability has:

```text
namespaced ID
contract major version
provider
cardinality
scope
health/state
```

Example:

```text
nexa.media.image.transform@1
org.example.television.sequence@1
```

## Provider selection

Selection order is explicit:

1. hard per-scope administrator/user choice;
2. capability-specific policy;
3. compatible preferred priority;
4. deterministic fallback.

Ties that cannot be resolved safely produce a configuration conflict.

Extension load order MUST NOT participate.

## Scope resolution

A provider may be selected at:

```text
global
server
library
user
session
request
```

More-specific explicit configuration wins over broader defaults if the capability permits it.

## MULTI

All compatible providers remain available. Callers may choose based on request constraints.

Example: image transforms.

## ORDERED

Nexa stores a deterministic order. Every element receives the operation only where the contract explicitly defines chaining.

Example: post-processing pipeline.

Failure semantics must be specified by each ORDERED capability; do not assume "continue on error".

## SELECT_ONE

Several providers can be installed, but exactly one is chosen in a scope when capability is required.

Example: default sequence semantics for a content type.

## EXCLUSIVE

A resource may have at most one owner.

Example: exact socket binding.

## Version compatibility

Major capability contract versions are not assumed compatible.

A provider may expose multiple contract versions simultaneously during migrations.

The broker selects a mutually supported version.

## Health

Provider state includes:

```text
available
degraded
unavailable
starting
stopping
```

Selection avoids unavailable providers.

Fallback behavior is capability specific and must not violate logical semantics.

## Permission check

Provider registration does not bypass permissions.

Both:

- extension's permission to register/provide;
- caller's permission to invoke/consume

must be enforceable where relevant.

## Diagnostics

Broker exposes:

```text
capability
providers
selected provider by scope
selection reason
version
health
conflicts
missing requirements
```

to administrators.

<!-- END specs/capability-model.md -->

---

<!-- BEGIN specs/media-provider-protocol.md -->

# Native Media Provider Protocol — Draft Specification

## Purpose

Defines the control-plane expectations between Nexa core and native media provider child processes.

This protocol is independent of FFmpeg/libvips/ImageMagick command syntax.

## Transport

Initial transport preference:

- child-process stdin/stdout or local IPC;
- framed messages;
- Protocol Buffers;
- separate stderr/log channel or structured log messages.

No public TCP port is required for ordinary provider operation.

## Handshake

Provider starts and receives/sends handshake including:

```text
protocol versions supported
provider ID/version
implementation build
capabilities
platform/hardware inventory
concurrency/resource limits
```

Nexa selects a mutually supported protocol version before work begins.

## Operations

### Probe

Input:

```text
source handle/descriptor
requested probe depth
limits
```

Output:

```text
normalized MediaDescriptor
warnings
```

### Transform

For finite transforms such as images/subtitles.

Input:

```text
source
normalized semantic recipe
target output handle/path/stream
resource budget
```

Events:

```text
accepted
progress
completed
failed
```

### StartPlaybackTransform

For long-lived/segment-producing playback work.

Input includes:

```text
source
delivery plan
selected streams
start position
output mode
resource budget
```

Returns a provider session ID.

### Seek/Reposition

If provider execution model supports persistent session repositioning.

Otherwise Nexa may cancel and create a replacement provider session.

### Cancel

Must be idempotent.

Provider must release work within bounded timeout.

### Health

Allows core to distinguish a failed job from failed provider process.

## Source/output handles

Do not serialize entire media payloads into Protobuf.

Use controlled mechanisms such as:

- file path within an explicitly granted root;
- inherited file descriptor/handle;
- named/anonymous pipe;
- local temporary output directory;
- stream descriptor negotiated by the protocol.

Provider must not infer arbitrary filesystem access from receiving one source.

## Normalized media descriptor

The descriptor should model semantics such as:

```text
container
duration
overall bitrate
video streams
audio streams
subtitle streams
attachments/chapters where supported
codec/profile/level
dimensions
frame rate
sample rate/channels/layout
HDR/color metadata
language/title/disposition
```

Do not expose FFprobe JSON as the public contract.

## Error model

Errors carry:

```text
stable provider error code
operation
retryability
human-readable diagnostic
provider-specific detail for admin logs
```

Provider stderr is diagnostic, not the machine API.

## Timeouts

Core owns operation deadlines.

Provider receives budget/deadline information and is killed if it ignores cancellation beyond configured grace.

## Process crash

On provider process exit:

- all outstanding operations fail;
- core records diagnostic;
- supervisor may restart provider under backoff;
- planner may retry with fallback provider if semantics allow.

## Security

Provider invocation never goes through a shell command string.

Arguments are structured.

Temporary paths are created by core/provider runtime using secure APIs.

Provider environment is minimized.

## Cache behavior revision

A provider advertises a cache-behavior revision when its output semantics may materially change for identical normalized requests. Core may incorporate this into derivative cache keys.

This value is not the provider package version by default; routine bugfix upgrades should not necessarily invalidate all derivatives.

<!-- END specs/media-provider-protocol.md -->

---

<!-- BEGIN specs/playback-session.md -->

# Playback Session Contract — Draft Specification

## Session authority

The server is authoritative.

A client may cache the latest received view for display, but it is never the source of truth.

## Session state

Conceptual state:

```text
session_id
user_id
controlling client/group
created_at
last_activity
context
current logical playable
position
sequence generator descriptor
generator state
random seed
history
future materialized window
play-next overlay
explicit tail
repeat policy
selected tracks/version preferences
delivery session summary
```

## Playable

A `Playable` is a logical playback unit resolved by content extensions.

Minimum identity:

```text
object/item ID
optional subresource/chapter/part identity
semantic metadata needed for display
```

Media source selection is not necessarily fixed until delivery planning.

## Start request

Input:

```text
target
intent
sequence flag/context hint
optional shuffle/repeat
optional start position
optional user-selected representation/tracks
client capability profile reference
```

Output:

```text
session ID
current entry
queue window
delivery information
```

## Queue window

The queue response identifies entries with stable session-entry IDs so client operations can refer to them even if the same object occurs more than once.

```text
history entries
current entry
up-next overlay
generated entries
explicit tail entries
continuation available/unknown/exhausted
```

## Mutations

Mutations are addressed to the session and include an expected revision where race protection matters.

Examples:

```text
skip
play-next
append
remove queued entry
move explicit entry
set shuffle policy
set repeat
seek
select audio/subtitle/representation
stop
```

The server returns the resulting revision/window or pushes an update.

## Revision

Playback session logical queue state carries a monotonically changing revision.

Concurrent controllers can detect stale mutations.

## Generator state

A generator provides:

```text
type/version
opaque serialized state
deterministic seed where relevant
context reference
```

Extension generator state must have a compatibility/version story.

Core does not inspect extension-private generator bytes beyond resource limits, but does own lifecycle and persistence.

## Window extension

When generated future entries fall below a threshold, core requests a bounded additional chunk.

Generation must avoid duplicate chunk execution under concurrency.

## Exhaustion

Generator explicitly returns:

```text
more
temporarily unavailable
exhausted
error
```

Do not infer exhaustion merely from an empty transient response without a contract.

## Queue overlays

Generator state is not rewritten for ordinary Play Next/Add to Queue actions.

Core owns overlay structures.

When overlays are consumed, generated continuation resumes.

## Delivery linkage

Logical session entry identity remains stable even if delivery plan changes or provider restarts.

A transcode failure must not accidentally advance the logical queue.

## Persistence

Durable state should include enough information for ordinary reconnect/restart continuity.

Ephemeral transcode segment paths/provider PIDs are not durable session state.

## Events

Session may emit:

```text
session.started
entry.started
entry.progress
entry.completed
queue.changed
delivery.changed
session.ended
session.error
```

Exact durable/transient event distinction is implementation-defined.

<!-- END specs/playback-session.md -->

---

<!-- BEGIN specs/data-directory.md -->

# Data Directory Layout — Draft Specification

## Principle

Canonical data and disposable data must be visibly separable.

Suggested baseline:

```text
data/
├── nexa.db
├── config/
├── blobs/
├── extensions/
├── extension-state/
├── logs/                 # optional depending platform/log strategy
└── cache/
    ├── cache.db
    ├── media/
    ├── transcode/
    └── tmp/
```

## Canonical

Must be backed up:

```text
nexa.db
config required for instance operation
managed blobs
extension-state
extension inventory/packages depending installation model
secret material through supported backup mechanism
```

## Disposable

May be deleted with Nexa stopped:

```text
cache/cache.db
cache/media
cache/transcode
cache/tmp
```

Nexa recreates them.

## Library source files

External library files are not part of the Nexa data directory backup unless explicitly imported into managed blob storage.

## Permissions

Data directory defaults must avoid broad OS-user access to:

- database;
- secrets;
- provider credentials;
- extension private state.

## Temporary files

Use atomic write/rename patterns for derivative generation where filesystem semantics permit.

Never expose partially written derivative files as cache hits.

## Disk full

Canonical writes must fail safely and surface a prominent admin error.

Cache pressure should trigger eviction before canonical data is endangered where practical.

## Path portability

Persist logical storage/source identifiers rather than assuming every source can be represented forever by the same absolute OS path.

<!-- END specs/data-directory.md -->

---

<!-- BEGIN adr/README.md -->

# Architecture Decision Records

ADRs capture decisions that should not be casually reversed during implementation.

| ADR | Decision |
|---|---|
| [0001](adr/0001-go-server.md) | Implement Nexa core server in Go |
| [0002](adr/0002-sqlite-first.md) | SQLite is the reference/default database |
| [0003](adr/0003-single-process-baseline.md) | Baseline deployment is a single Nexa process |
| [0004](adr/0004-capability-broker.md) | Extensions use capability-brokered versioned contracts |
| [0005](adr/0005-agent-model.md) | Generalize people/organizations into Agents |
| [0006](adr/0006-typed-runtime-fields.md) | Store custom fields using a portable typed runtime model |
| [0007](adr/0007-core-owned-media-lifecycle.md) | Core owns media lifecycle; providers own transforms |
| [0008](adr/0008-server-authoritative-playback.md) | Playback sequencing is server authoritative |
| [0009](adr/0009-out-of-process-native-providers.md) | Native media providers run out of process |
| [0010](adr/0010-mandatory-setup-wizard.md) | First run requires a resumable setup wizard |
| [0011](adr/0011-contract-first-http-api.md) | Public HTTP API is contract-first OpenAPI |
| [0012](adr/0012-no-required-external-infrastructure.md) | Redis/Postgres/object storage are never baseline requirements |

New ADRs should use the next sequential number and include Context, Decision, Consequences, and Status.

<!-- END adr/README.md -->

---

<!-- BEGIN adr/0001-go-server.md -->

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

<!-- END adr/0001-go-server.md -->

---

<!-- BEGIN adr/0002-sqlite-first.md -->

# ADR 0002: SQLite Is the Reference Database

Status: Accepted

## Context

Nexa must be deployable like a home-server appliance without requiring infrastructure services.

## Decision

SQLite is the default and reference database. PostgreSQL is an optional scaling backend.

Features are designed with SQLite semantics first and must remain genuinely useful there.

## Consequences

- One-file database baseline.
- Write transactions must remain short and contention must be engineered deliberately.
- PostgreSQL-only data semantics cannot define the canonical domain.
- Cross-backend contract tests are required once PostgreSQL is implemented.

<!-- END adr/0002-sqlite-first.md -->

---

<!-- BEGIN adr/0003-single-process-baseline.md -->

# ADR 0003: Single-Process Baseline

Status: Accepted

## Context

Simple deployment is a product goal.

## Decision

A normal Nexa installation consists of one Nexa server process plus its data directory. In-process workers handle background jobs. Native media/privileged providers may be supervised child processes.

## Consequences

- No required service mesh, worker deployment, Redis, or separate frontend process.
- Advanced process separation may be added later behind stable service boundaries.

<!-- END adr/0003-single-process-baseline.md -->

---

<!-- BEGIN adr/0004-capability-broker.md -->

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

<!-- END adr/0004-capability-broker.md -->

---

<!-- BEGIN adr/0005-agent-model.md -->

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

<!-- END adr/0005-agent-model.md -->

---

<!-- BEGIN adr/0006-typed-runtime-fields.md -->

# ADR 0006: Portable Typed Runtime Fields

Status: Accepted

## Context

Libraries and extensions need custom fields without schema migration, but queries must remain typed and efficient on SQLite and PostgreSQL.

## Decision

Use a constrained typed runtime-value model (typed EAV semantics), indexed by immutable field IDs. JSON is reserved for unstructured configuration/escape-hatch values.

## Consequences

- Field add/rename/delete does not require SQL schema migration.
- Query compiler can use typed indexes.
- Exact physical table layout remains benchmark-driven.

<!-- END adr/0006-typed-runtime-fields.md -->

---

<!-- BEGIN adr/0007-core-owned-media-lifecycle.md -->

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

<!-- END adr/0007-core-owned-media-lifecycle.md -->

---

<!-- BEGIN adr/0008-server-authoritative-playback.md -->

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

<!-- END adr/0008-server-authoritative-playback.md -->

---

<!-- BEGIN adr/0009-out-of-process-native-providers.md -->

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

<!-- END adr/0009-out-of-process-native-providers.md -->

---

<!-- BEGIN adr/0010-mandatory-setup-wizard.md -->

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

<!-- END adr/0010-mandatory-setup-wizard.md -->

---

<!-- BEGIN adr/0011-contract-first-http-api.md -->

# ADR 0011: Contract-First Public HTTP API

Status: Accepted

## Context

Nexa will have multiple clients and extensions and cannot let internal Go structs become de facto API.

## Decision

Define the public HTTP API using OpenAPI 3.1. Generate Go/TypeScript bindings where useful. Treat the specification as source code.

## Consequences

- API changes are reviewable and testable.
- Generated code is reproducible and not hand-edited.
- Extension/internal RPC contracts remain separate from public HTTP API.

<!-- END adr/0011-contract-first-http-api.md -->

---

<!-- BEGIN adr/0012-no-required-external-infrastructure.md -->

# ADR 0012: No Required External Infrastructure

Status: Accepted

## Context

Nexa must remain easy to deploy.

## Decision

Baseline Nexa requires only its server process, SQLite database, and local data directory. PostgreSQL, Redis-like systems, S3, external search, external workers, and reverse proxies may be optional integrations only.

## Consequences

- Every core feature must have a baseline implementation.
- Advanced integrations cannot become hidden mandatory dependencies.

<!-- END adr/0012-no-required-external-infrastructure.md -->
