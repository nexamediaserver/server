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
