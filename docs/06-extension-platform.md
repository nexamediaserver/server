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
