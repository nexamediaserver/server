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
