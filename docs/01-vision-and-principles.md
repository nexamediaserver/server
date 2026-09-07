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
