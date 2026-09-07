# Playback Session Contract — Draft Specification

## Session authority

The server is authoritative.

A client may cache the latest received view for display, but it is never the source of truth.

## Session state

Conceptual state:

```text
session_id
user_id
controlling client/group
created_at
last_activity
context
current logical playable
position
sequence generator descriptor
generator state
random seed
history
future materialized window
play-next overlay
explicit tail
repeat policy
selected tracks/version preferences
delivery session summary
```

## Playable

A `Playable` is a logical playback unit resolved by content extensions.

Minimum identity:

```text
object/item ID
optional subresource/chapter/part identity
semantic metadata needed for display
```

Media source selection is not necessarily fixed until delivery planning.

## Start request

Input:

```text
target
intent
sequence flag/context hint
optional shuffle/repeat
optional start position
optional user-selected representation/tracks
client capability profile reference
```

Output:

```text
session ID
current entry
queue window
delivery information
```

## Queue window

The queue response identifies entries with stable session-entry IDs so client operations can refer to them even if the same object occurs more than once.

```text
history entries
current entry
up-next overlay
generated entries
explicit tail entries
continuation available/unknown/exhausted
```

## Mutations

Mutations are addressed to the session and include an expected revision where race protection matters.

Examples:

```text
skip
play-next
append
remove queued entry
move explicit entry
set shuffle policy
set repeat
seek
select audio/subtitle/representation
stop
```

The server returns the resulting revision/window or pushes an update.

## Revision

Playback session logical queue state carries a monotonically changing revision.

Concurrent controllers can detect stale mutations.

## Generator state

A generator provides:

```text
type/version
opaque serialized state
deterministic seed where relevant
context reference
```

Extension generator state must have a compatibility/version story.

Core does not inspect extension-private generator bytes beyond resource limits, but does own lifecycle and persistence.

## Window extension

When generated future entries fall below a threshold, core requests a bounded additional chunk.

Generation must avoid duplicate chunk execution under concurrency.

## Exhaustion

Generator explicitly returns:

```text
more
temporarily unavailable
exhausted
error
```

Do not infer exhaustion merely from an empty transient response without a contract.

## Queue overlays

Generator state is not rewritten for ordinary Play Next/Add to Queue actions.

Core owns overlay structures.

When overlays are consumed, generated continuation resumes.

## Delivery linkage

Logical session entry identity remains stable even if delivery plan changes or provider restarts.

A transcode failure must not accidentally advance the logical queue.

## Persistence

Durable state should include enough information for ordinary reconnect/restart continuity.

Ephemeral transcode segment paths/provider PIDs are not durable session state.

## Events

Session may emit:

```text
session.started
entry.started
entry.progress
entry.completed
queue.changed
delivery.changed
session.ended
session.error
```

Exact durable/transient event distinction is implementation-defined.
