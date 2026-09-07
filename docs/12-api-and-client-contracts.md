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
