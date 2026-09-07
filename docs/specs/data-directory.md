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
