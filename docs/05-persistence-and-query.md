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
