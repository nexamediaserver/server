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
