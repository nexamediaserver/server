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
