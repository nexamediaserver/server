# Nexa Roadmap

This roadmap turns the architecture and product review into a milestone checklist that can be executed in release-sized increments. It reflects the current repository state and the architectural invariants in the design documents.

## Release strategy

The project should advance in ordered milestones that preserve these constraints:

- Setup is mandatory and server-side.
- The app stays unusable until setup completes.
- Core owns policy and state; clients request intent.
- Providers remain out-of-process and isolated from core.
- Extension contracts are versioned, deterministic, and conflict-safe.
- The project proves a working user flow before broad platform generalization.

---

## Milestone 0 — Delivery baseline

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] CI is enforced for Go and frontend builds
- [ ] Formatting and lint checks are automated
- [ ] Test workflow is required for all changes
- [ ] ADR and design review process is documented
- [ ] Open questions are tracked with owners and due dates
- [ ] Error, log, and request-correlation conventions are defined
- [ ] Stable ID and revision conventions are published

Exit criteria:

- The repository has a repeatable delivery workflow.
- Public contract changes require a reviewed decision record.
- New work starts from a stable engineering baseline.

---

## Milestone 1 — Secure bootstrap and setup gate

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Data directory bootstrap is validated
- [ ] SQLite bootstrap and migration are stable
- [ ] Setup state machine is resumable across restarts
- [ ] First-run admin creation is implemented
- [ ] Local admin auth/session baseline exists
- [ ] Normal app access is blocked before setup completion
- [ ] Startup and shutdown are graceful and tested
- [ ] Setup state and admin state are persisted and recoverable

Exit criteria:

- A clean install reaches a real setup flow.
- Setup can pause, resume, and recover without corruption.
- The app is not usable before setup completion.

---

## Milestone 2 — Minimal domain and first working library workflow

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Minimal library/object model exists
- [ ] Typed runtime fields are supported
- [ ] Stable IDs and revisions are implemented
- [ ] Basic browse/detail views work
- [ ] A single library workflow is implemented end-to-end
- [ ] Local import or ingestion works for one reference content type
- [ ] Pagination and auth-aware listing are bounded and tested
- [ ] Restarting the app preserves the core workflow state

Exit criteria:

- A user can create a library and view content without a speculative abstraction layer.
- The system proves the persistence and query model against a real workflow.
- The architecture is validated by actual usage before more generalization.

---

## Milestone 3 — Extension boundary and capability broker

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Extension manifest model is defined
- [ ] Capability registry and discovery are implemented
- [ ] Version negotiation is supported
- [ ] Dependency ordering is enforced
- [ ] Conflict detection is deterministic
- [ ] Lifecycle and permissions are defined
- [ ] An initial test extension is implemented and activated
- [ ] Extension activation is validated with contract tests

Exit criteria:

- A test extension can contribute capabilities without importing core internals.
- Conflicts are resolved predictably.
- Extension activation is safe, observable, and reproducible.

---

## Milestone 4 — Extension-aware setup and reference templates

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Setup can install or select extensions
- [ ] Dependency-aware ordering is enforced during setup
- [ ] Capability providers can be recommended by the installer
- [ ] Extension-contributed library templates exist
- [ ] Setup can reach a valid runtime state from installed capabilities

Exit criteria:

- Setup is capable of producing a valid runtime based on installed extensions.
- A content extension can contribute a useful structure without core knowing the media type ahead of time.

---

## Milestone 5 — Query engine and authorization baseline

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Query AST and validation are implemented
- [ ] SQLite compiler is stable
- [ ] Field selection and pagination are enforced
- [ ] Authorization-aware queries are tested
- [ ] Representative compatibility tests exist
- [ ] Unbounded scans and unsafe query patterns are blocked

Exit criteria:

- Real workloads can query the system without unsafe or unbounded behavior.
- Query semantics are stable enough to serve as the contract baseline.

---

## Milestone 6 — Media asset scaffolding

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Stable asset identity is implemented
- [ ] Source resolver abstraction exists
- [ ] Managed blob storage is in place
- [ ] Cache layout and GC are defined
- [ ] Asset authorization is enforced
- [ ] Stable media routes are available
- [ ] Single-flight and priority-aware jobs are implemented
- [ ] Artwork or other derived assets can be generated and served

Exit criteria:

- Assets are addressed by identity, not by client-visible cache paths.
- Media generation remains server-owned and authorized.
- The public HTTP contract stays stable as asset internals evolve.

---

## Milestone 7 — Provider protocol and test providers

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Provider lifecycle management exists
- [ ] Provider handshake and versioning are defined
- [ ] Capability negotiation is implemented
- [ ] Transform and probe operations are supported
- [ ] Cancellation, progress, and timeout behavior is implemented
- [ ] File and pipe transfer semantics are tested
- [ ] Crash recovery and retry behavior are covered
- [ ] A fake/test provider proves the protocol before native providers are used

Exit criteria:

- Core does not depend on any specific media engine.
- Provider behavior is isolated, observable, and recoverable.
- Native media implementations can replace each other without breaking the public contract.

---

## Milestone 8 — Playback foundation and direct play

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] `PlaybackSession` model exists
- [ ] Playback intent and capability profile are defined
- [ ] Queue windows and deterministic ordering are implemented
- [ ] Repeat/shuffle mechanics are supported
- [ ] Reconnect and session rehydration work
- [ ] Direct-play test media is used for validation
- [ ] Server owns playback state and sequencing decisions

Exit criteria:

- Playback state remains server-authoritative across reconnects and restarts.
- Direct play works before media-engine complexity is introduced.
- The playback model is stable before FFmpeg and richer delivery layers are added.

---

## Milestone 9 — Media delivery, FFmpeg, and rich playback

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Probe descriptors and representation model exist
- [ ] Direct play, remux, and transcode planning work
- [ ] Subtitle planning and delivery are supported
- [ ] FFmpeg provider integration is implemented
- [ ] Segmented delivery and seek behavior are stable
- [ ] Fallback and provider diagnostics exist
- [ ] Client contracts remain stable even with provider changes

Exit criteria:

- Media is delivered through the server without exposing provider-specific behavior.
- Playback remains stable and diagnosable under realistic media conditions.

---

## Milestone 10 — Auth extension contracts

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Account/auth service contract is defined
- [ ] Identity linking exists
- [ ] Directory and group mapping is modeled
- [ ] Provider login contributions are supported
- [ ] Secrets service is integrated
- [ ] OIDC reference provider is implemented
- [ ] LDAP validation path is considered or implemented

Exit criteria:

- Authentication is not hardcoded into the core app shell.
- A stable account model supports multiple auth provider types.
- External identities map cleanly to server-owned auth semantics.

---

## Milestone 11 — Network service broker and external exposure

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Route and service claims are modeled
- [ ] TCP/UDP discovery claims are supported
- [ ] Diagnostics and lifecycle behavior are stable
- [ ] Conflict detection for service exposure is enforced
- [ ] An HTTP-first reference extension is implemented
- [ ] A more demanding protocol scenario is tested

Exit criteria:

- Protocol-based services remain isolated from core internals.
- Network exposure is validated before activation.

---

## Milestone 12 — PostgreSQL backend and compatibility hardening

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] PostgreSQL repository implementations exist
- [ ] Query compiler parity is validated
- [ ] Job claiming and migration semantics are tested
- [ ] Installation and switching strategy is stable
- [ ] SQLite remains the default reference backend
- [ ] Parity test suite is complete

Exit criteria:

- PostgreSQL can be used without changing public semantics or extension contracts.
- SQLite remains the default and compatibility baseline.

---

## Milestone 13 — Advanced deployment and scale optimization

Status: [ ] Not started / [ ] In progress / [ ] Complete

Checklist:

- [ ] Optional remote storage is considered and tested
- [ ] Remote access / DDNS integration is optional and extension-based
- [ ] Observability integrations are optional and not required by default
- [ ] Multi-process or worker scaling is justified by real workload
- [ ] Advanced media-generation optimization is measured and justified

Exit criteria:

- Additional infrastructure choices are introduced only when real usage requires them.
- The default deployment remains simple and self-hosted.

---

## Hard gates

The following are mandatory before the next milestone is considered safe:

- [ ] Setup gate: no normal app behavior before setup completes
- [ ] Core-vs-extension gate: no content-specific semantics are hardcoded into core
- [ ] Provider gate: media engines are isolated behind a versioned protocol
- [ ] Playback gate: the server owns session state and ordering
- [ ] Scalability gate: pagination, auth filtering, and bounded queries are proven

---

## Release wedge for a small team

The best early beta definition is:

- [ ] secure setup
- [ ] one useful library workflow
- [ ] durable local import/indexing
- [ ] artwork or media route delivery
- [ ] basic server-owned playback

This is the practical minimum viable product for the project while preserving the architecture.

---

## Not for early implementation

These should remain explicitly deferred unless proven necessary:

- [ ] Redis as a required dependency
- [ ] multi-node playback or distributed locking
- [ ] arbitrary in-process native plugins
- [ ] complex workflow engines before the domain model is stable
- [ ] broad network service expansion before the local baseline is stable
- [ ] PostgreSQL scaling work before the SQLite contract is proven

---

## Status summary

Use this checklist as the execution ledger. When a milestone is complete, mark it as done and attach the proof:

- code change and tests
- validation output
- ADR or contract update if public behavior changed
- release notes or updated docs if user-visible behavior changed
