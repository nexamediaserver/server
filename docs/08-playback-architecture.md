# Playback Architecture

## Architectural rule

> **The client submits playback intent. Nexa owns the authoritative playback session, sequence, queue, and delivery decisions.**

The client must never be required to construct or maintain the canonical queue.

## Two planners, two questions

Playback is split into:

### Playback Orchestrator

Answers:

> **WHAT should play?**

Responsibilities:

- interpret the requested target and playback intent;
- determine playback context;
- create/manage session state;
- select sequence resolver/generator;
- materialize upcoming entries;
- maintain history;
- apply shuffle/repeat/radio policy;
- apply user queue mutations;
- extend dynamic sequences.

### Media Delivery Planner

Answers:

> **HOW should this playable reach this client?**

Responsibilities:

- choose source/representation;
- direct play vs remux vs transcode;
- choose codecs/container/bitrate/resolution;
- subtitle handling;
- provider selection.

These systems are related but must not be collapsed into one extension contract.

## Playback intent API

A client should send a high-level request such as:

```json
{
  "target": {
    "type": "item",
    "id": "..."
  },
  "mode": "play",
  "inSequence": true
}
```

Optional user choices may include:

```text
shuffle
repeat mode
explicit start position
selected media version
selected audio/subtitle track
```

The exact HTTP schema is defined by OpenAPI.

The client does not send the complete sequence for ordinary contextual playback.

## Playback session

A server-owned session contains conceptually:

```text
session_id
user_id
client_id
playback context
sequence generator type
serialized/durable generator state
history
current entry
materialized future window
explicit queue overlays
shuffle/repeat policy
delivery state
last activity
```

Session persistence requirements may vary by session type, but enough state must survive client reconnect and should survive ordinary server restart where practical.

## Playback context

The same item can have different sequence meaning based on how playback starts.

Examples:

- playing Episode 6 from within a season;
- playing the same episode from a manually ordered playlist;
- playing a track from an album;
- playing the track from "radio from this track";
- playing an item from a saved query.

The context must be explicit in server state.

## Sequence resolvers

Content extensions provide sequence semantics.

A resolver takes:

```text
target
intent
user
library/content context
```

and returns a sequence specification.

Example:

```text
resolver: television
target: episode 6
intent: play in sequence
->
generator: ordered-episodes
context: season/series
start: episode 6
```

Core does not hardcode what "next episode" means.

## Sequence generators

Generators produce chunks of future playables.

Conceptual contract:

```text
next_chunk(context, count) -> [Playable...]
```

Generators may be:

- static;
- ordered children;
- query-backed;
- deterministic shuffle;
- radio/recommendation;
- extension defined.

The entire future sequence need not exist in memory or persistent storage.

## Dynamic queue window

The client receives a bounded view:

```text
previous: 2-5 entries
current: 1 entry
upcoming: N entries
```

As upcoming content is consumed, the server asks the generator for another chunk.

Clients may request more history/upcoming entries for UI, but the server remains authoritative.

## Deterministic randomization

Shuffle/radio generators should use a server-created seed plus serializable generator state.

Benefits:

- reconnect does not reshuffle;
- playback handoff keeps order;
- restart can restore order;
- behavior is testable;
- "why did this play?" diagnostics are possible.

Uniform random shuffle is not the only valid algorithm. Extensions may implement smart shuffle subject to deterministic/stateful contracts.

## Queue overlays

Explicit user queue actions should overlay generated continuation instead of corrupting generator state.

Conceptual effective order:

```text
history
current
play-next overlay
generated continuation
explicit tail additions
```

Operations include:

- Play Next;
- Add to Queue;
- Remove;
- Reorder explicitly queued items;
- Skip;
- Clear explicit queue;
- regenerate/refresh continuation where policy permits.

The semantics of each mutation must be server defined.

## Repeat

Repeat modes should be represented as server policy:

```text
none
current
sequence/context
explicit queue
extension-defined where justified
```

Do not require the client to restart playback manually to implement repeat.

## Client capability report

Clients report facts such as:

- supported containers;
- video/audio codecs;
- codec profiles/levels;
- subtitle formats;
- maximum dimensions/bitrate;
- HDR capabilities;
- direct-streaming constraints.

The capability report is input to the Media Delivery Planner.

A client MUST NOT dictate an encoder command.

## Delivery decision

For each current playable:

```text
playable
+ persistent representations
+ client capabilities
+ network/server policy
+ provider capabilities
= delivery plan
```

Delivery plan classes include:

- direct play;
- remux/direct stream;
- audio-only transcode;
- subtitle conversion;
- video transcode;
- segmented streaming.

## Remux before transcode

If codecs are acceptable and only the container/subtitle packaging is incompatible, the planner should prefer remux/conversion over unnecessary re-encoding.

## Session output ownership

Nexa HTTP owns:

- authorization;
- manifests;
- media URLs;
- range handling;
- playback session lifetime;
- segment delivery;
- accounting;
- remote-access integration.

FFmpeg or another provider is a worker, not the public media web server.

## Seeking

Seeking must be a first-class session operation.

The orchestration/delivery system must:

- map client seek intent to the current playable;
- cancel obsolete look-ahead;
- reposition/restart provider work;
- avoid generating large unused ranges;
- resume segment production around the requested location.

Provider protocols must support cancellation and the relevant reposition semantics.

## Progress and watched state

Clients report playback progress/events.

The server decides:

- when progress is persisted;
- when an item is considered played/completed;
- resume position behavior;
- how watched state affects future generators.

Extensions may contribute content-specific completion thresholds if the platform explicitly exposes such a hook.

## Multi-client handoff

Because session and sequence state are server-owned, future playback handoff should be possible without rebuilding the queue client-side.

A receiving client supplies its capabilities; the Media Delivery Planner may choose a new delivery plan while the Playback Orchestrator preserves sequence state.

## Failure behavior

If a provider fails mid-playback, core may:

- retry with another provider;
- select a different representation;
- restart the same provider;
- terminate the delivery with a structured error.

Provider fallback must not silently change the logical current queue item.

## Observability

A playback session should expose diagnostic state for administrators:

- resolved context;
- generator/provider IDs;
- random seed/state summary where safe;
- current queue window;
- source representation;
- direct/remux/transcode reason;
- selected provider/hardware path;
- bitrate/resolution;
- errors/retries.
