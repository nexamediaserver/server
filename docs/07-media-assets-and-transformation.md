# Media Assets and Transformation

## Architectural rule

> **Nexa owns the media lifecycle; extensions provide media capabilities.**

Nexa core owns:

- asset identity;
- source resolution;
- media transformation requests;
- provider selection;
- derivative cache keys;
- HTTP delivery;
- resource scheduling;
- persistent representations;
- playback-session integration.

Media providers own mechanics such as decoding, resizing, encoding, remuxing, transcoding, subtitle conversion, and segmentation.

## Media assets

An Asset is a stable Nexa identity for media bytes.

Conceptual properties:

```text
id
kind/media class
source descriptor
source revision
declared/detected MIME/type
size
content hash where available
probe metadata
created/updated timestamps
```

Fields and domain records reference `AssetId`, not cache paths or public URLs.

## Asset sources

An asset may resolve to:

- Nexa-managed blob storage;
- a file in a library/source location;
- extension-owned storage;
- remote content;
- a persistent generated representation.

Asset identity does not imply Nexa copied the entire source into managed storage.

This is essential for large video files.

## Managed blob storage

Default implementation:

```text
/data/blobs/<content-derived-or-managed-layout>
```

Optional implementation:

- S3-compatible object storage.

Expose a `BlobStore`-like internal abstraction.

Where practical, managed blobs should support content hashing and deduplication.

## Media provider capabilities

Providers advertise semantic capabilities, not tool command lines.

Examples:

```text
media.image.decode.jpeg
media.image.decode.heic
media.image.resize
media.image.crop
media.image.encode.webp

media.probe.audio-video
media.remux
media.video.transcode
media.audio.transcode
media.subtitle.convert
media.subtitle.burn
media.segment.hls

codec.decode.hevc
codec.encode.h264.nvenc
codec.encode.h264.software
```

Provider capability descriptions should include constraints such as:

- accepted formats/codecs;
- dimensions;
- alpha/animation support;
- HDR handling;
- hardware device;
- concurrency/cost hints.

## Semantic transformations

Core asks for outcomes.

Example image transformation:

```text
width: 300
height: 450
fit: cover
gravity: center
output: auto
quality: balanced
preserve_alpha: true
preserve_animation: false
```

Example video target:

```text
container: HLS
video codec: H.264
max resolution: 1920x1080
max bitrate: 12 Mbps
HDR policy: tone-map to SDR
audio codec: AAC
channels: 2
subtitle mode: external
```

Providers translate these semantics into libvips/ImageMagick/FFmpeg-specific operations.

Provider-specific knobs may exist in advanced settings but are not part of the baseline semantic contract.

## Stable delivery scaffolding

Core owns stable media routes even if no transformer is installed.

Conceptual routes:

```text
/media/assets/{asset-id}/original
/media/assets/{asset-id}/image/{rendition}
/playback/sessions/{session-id}/...
```

An extension never creates its own ad-hoc thumbnail route for core media.

Removing a provider changes provider availability, not media URL architecture.

## Rendition profiles

Images should normally be requested via named profiles rather than arbitrary dimensions.

Examples:

```text
avatar-xs
avatar-sm
poster-grid
poster-detail
backdrop-small
backdrop-large
logo
```

Profiles define normalized semantic transforms.

Extensions may register additional profiles.

Responsive variants may use constrained buckets such as `@1x`, `@2x`.

Arbitrary transforms, if exposed, require strict authorization/resource limits and must not become an unbounded cache-key generator.

## Format negotiation

Core decides the preferred output based on:

- client `Accept`/capabilities;
- rendition semantics;
- installed provider capabilities;
- administrator policy.

Example preference:

```text
AVIF -> WebP -> JPEG
```

The transformer does not decide browser policy.

## Derivative cache

A derivative is reproducible cache content.

A cache key should incorporate at least:

```text
source revision
normalized transformation recipe
output format
Nexa transform contract revision
provider cache/behavior revision where required
```

Conceptually:

```text
DerivativeKey = hash(...)
```

Cache path example:

```text
/data/cache/media/9f/24/<hash>.avif
```

Changing the source or recipe naturally produces a new key. Old derivatives become garbage-collectable rather than requiring complex invalidation updates.

## Cache separation

Do not store high-frequency cache access bookkeeping in the canonical database.

Suggested data directory:

```text
/data/
├── nexa.db
├── blobs/
└── cache/
    ├── media/
    ├── transcode/
    ├── tmp/
    └── cache.db
```

`cache.db` is disposable.

Hot access metadata may be kept in memory and flushed in batches.

## Single-flight generation

Concurrent requests for the same missing derivative MUST coalesce.

Only one transformation should execute per derivative key per process; other callers await its result.

Future multi-node deployments may extend the same abstraction with distributed leases.

## Work scheduling and priority

Core owns media work priorities.

Suggested classes:

```text
1 interactive playback
2 interactive image derivative
3 playback look-ahead
4 UI prefetch
5 background artwork generation
6 library analysis
7 maintenance
```

Providers advertise capacity/limits.

Core must prevent background thumbnail work from starving active playback.

## Resource limits

Transformation requests must have core-level budgets independent of provider settings:

- maximum source/output dimensions;
- maximum decoded pixels;
- memory budget;
- execution timeout;
- temporary disk budget;
- concurrency;
- maximum generated output size.

ImageMagick-like providers require restrictive policies for untrusted input.

## Native media provider process model

Media providers are supervised child processes.

Control protocol:

```text
Nexa <---- framed versioned messages ----> Provider
```

Expected operations include:

```text
Hello/Handshake
Capabilities
Probe
Transform
StartSession
Seek/Reposition where applicable
Cancel
Progress
Complete
Error
Health
```

Use Protobuf unless superseded by an ADR.

Bulk data travels through controlled files/pipes/streams, not giant Protobuf payloads.

## Cancellation

Every expensive provider operation MUST support cancellation.

Cancellation must terminate or reclaim abandoned FFmpeg/libvips/ImageMagick work within bounded time.

Orphaned media worker processes are unacceptable.

## Persistent representations

Distinguish canonical sources, persistent optimized representations, and ephemeral playback cache.

```text
Original Asset
├── persistent representation: mobile H.264
├── persistent representation: AV1
└── ...

Playback Session
└── ephemeral HLS/transcode segments
```

Persistent representations are first-class metadata and may be selected for later playback.

Ephemeral session outputs are garbage collected.

## Probe metadata

Probe results should be cached against source revision.

Probe information may include:

- container;
- streams;
- codecs/profiles/levels;
- dimensions;
- frame rate;
- sample rate/channels;
- bitrate;
- HDR/color data;
- subtitle formats;
- duration;
- keyframe/seek-relevant metadata.

The public model should not be tied to FFprobe JSON structure.
