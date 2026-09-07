# Glossary

## Agent

A generalized entity that can participate in credits or relationships. Examples include an individual person, group, company, studio, label, publisher, band, orchestra, or extension-defined kind.

## Agent Kind

An extensible semantic classification for an Agent, such as `core.person`, `core.organization`, `music.band`, or `games.publisher`.

## Asset / Media Asset

A stable Nexa identity referring to a source of media bytes. An asset may point to managed blob storage, a library file, a remote source, or another storage provider.

## Capability

A named, versioned function or service an extension provides or consumes.

## Capability Broker

The core subsystem that discovers capabilities, validates versions and permissions, selects providers, resolves ordering, detects conflicts, and manages extension activation.

## Credit

A relationship between an Item/Object and an Agent with a role, optional credited-as name, and ordering metadata.

## Derivative

A reproducible transformed representation of an asset, normally stored in disposable cache storage. Examples: a 240x360 AVIF poster or a resized avatar.

## Extension

A package that adds semantics, capabilities, UI, integrations, protocols, or providers to Nexa through supported extension contracts.

## Hub / Library

A configured collection/domain in Nexa. "Library" is the common end-user term; "Hub" may be used for the more general programmable concept where appropriate. The implementation should converge on one canonical internal term before the public API stabilizes.

## Item / Object

A content record managed by a Hub/Library. The architecture may generalize Items into a wider Object model, but the exact hierarchy remains an open modeling decision.

## Media Delivery Planner

The component that decides HOW a specific playable should be delivered: direct play, remux, transcode, representation selection, subtitles, bitrate/resolution, etc.

## Media Provider

An extension/provider capable of probing or transforming media. Examples: FFmpeg, libvips, ImageMagick.

## Playback Orchestrator

The server component that decides WHAT should play, maintains authoritative session/queue state, invokes sequence generators, and applies queue mutations.

## Playback Session

A server-owned runtime/persistable state object representing an active playback context for a user/client.

## Representation

A persistent, intentional alternate media form associated with an asset, as opposed to disposable cache. Example: a pre-optimized mobile H.264 copy.

## Rendition Profile

A named semantic image/media transformation profile such as `poster-grid`, `avatar-sm`, or `backdrop-large`.

## Sequence Generator

A server-side stateful or deterministic generator that supplies additional playables for a playback session, such as ordered episodes, album order, shuffle, radio, or extension-defined logic.

## Setup Provider / Setup Step

Core or extension-defined work exposed in the mandatory first-run setup flow.

## Source Revision

A stable value that changes when the bytes or material identity of an Asset's source changes. Used in derivative cache keys and invalidation.

## Typed EAV

A constrained entity-attribute-value model in which custom field values are stored in typed columns/tables, preserving efficient typed indexing while allowing runtime-defined fields.
