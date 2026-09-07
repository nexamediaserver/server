# Native Media Provider Protocol — Draft Specification

## Purpose

Defines the control-plane expectations between Nexa core and native media provider child processes.

This protocol is independent of FFmpeg/libvips/ImageMagick command syntax.

## Transport

Initial transport preference:

- child-process stdin/stdout or local IPC;
- framed messages;
- Protocol Buffers;
- separate stderr/log channel or structured log messages.

No public TCP port is required for ordinary provider operation.

## Handshake

Provider starts and receives/sends handshake including:

```text
protocol versions supported
provider ID/version
implementation build
capabilities
platform/hardware inventory
concurrency/resource limits
```

Nexa selects a mutually supported protocol version before work begins.

## Operations

### Probe

Input:

```text
source handle/descriptor
requested probe depth
limits
```

Output:

```text
normalized MediaDescriptor
warnings
```

### Transform

For finite transforms such as images/subtitles.

Input:

```text
source
normalized semantic recipe
target output handle/path/stream
resource budget
```

Events:

```text
accepted
progress
completed
failed
```

### StartPlaybackTransform

For long-lived/segment-producing playback work.

Input includes:

```text
source
delivery plan
selected streams
start position
output mode
resource budget
```

Returns a provider session ID.

### Seek/Reposition

If provider execution model supports persistent session repositioning.

Otherwise Nexa may cancel and create a replacement provider session.

### Cancel

Must be idempotent.

Provider must release work within bounded timeout.

### Health

Allows core to distinguish a failed job from failed provider process.

## Source/output handles

Do not serialize entire media payloads into Protobuf.

Use controlled mechanisms such as:

- file path within an explicitly granted root;
- inherited file descriptor/handle;
- named/anonymous pipe;
- local temporary output directory;
- stream descriptor negotiated by the protocol.

Provider must not infer arbitrary filesystem access from receiving one source.

## Normalized media descriptor

The descriptor should model semantics such as:

```text
container
duration
overall bitrate
video streams
audio streams
subtitle streams
attachments/chapters where supported
codec/profile/level
dimensions
frame rate
sample rate/channels/layout
HDR/color metadata
language/title/disposition
```

Do not expose FFprobe JSON as the public contract.

## Error model

Errors carry:

```text
stable provider error code
operation
retryability
human-readable diagnostic
provider-specific detail for admin logs
```

Provider stderr is diagnostic, not the machine API.

## Timeouts

Core owns operation deadlines.

Provider receives budget/deadline information and is killed if it ignores cancellation beyond configured grace.

## Process crash

On provider process exit:

- all outstanding operations fail;
- core records diagnostic;
- supervisor may restart provider under backoff;
- planner may retry with fallback provider if semantics allow.

## Security

Provider invocation never goes through a shell command string.

Arguments are structured.

Temporary paths are created by core/provider runtime using secure APIs.

Provider environment is minimized.

## Cache behavior revision

A provider advertises a cache-behavior revision when its output semantics may materially change for identical normalized requests. Core may incorporate this into derivative cache keys.

This value is not the provider package version by default; routine bugfix upgrades should not necessarily invalidate all derivatives.
