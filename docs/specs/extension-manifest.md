# Extension Manifest Contract — Draft Specification

Status: Draft; intended to guide implementation.

## Goals

The manifest must allow Nexa to reason about an extension before executing its code.

The manifest therefore describes:

- identity/version;
- Nexa compatibility;
- execution artifacts;
- provided capabilities;
- required/recommended capabilities;
- permissions;
- resource claims;
- setup contributions;
- configuration schemas;
- conflicts/replacements.

## Example shape

Illustrative TOML:

```toml
id = "org.example.movies"
version = "1.0.0"
name = "Movies"
nexa = ">=1.0 <2.0"

[execution]
kind = "wasm"
entrypoint = "wasm/extension.wasm"

[[provides]]
capability = "library.template"
id = "org.example.movies.library"
version = 1
cardinality = "multi"

[[requires]]
capability = "media.image.transform"
version = ">=1"
optional = true
recommended = true

[[permissions]]
id = "objects.read"

[[permissions]]
id = "objects.write"

[[setup.steps]]
id = "metadata"
after = ["core.extensions"]
before = ["core.libraries"]
required = false
schema = "schemas/setup-metadata.json"

[[claims.http]]
path = "/example-protocol"
mode = "exclusive"
```

The final syntax may change; semantics should not.

## Identity

`id` MUST be globally namespaced and stable.

`version` MUST use a deterministic comparable version scheme, expected to be semantic versioning unless an ADR says otherwise.

Display name is not identity.

## Compatibility

Manifest declares a Nexa compatibility range and extension-service capability versions.

Nexa MUST reject activation when mandatory compatibility is unsatisfied.

## Artifacts

Platform-specific native artifacts identify:

```text
os
architecture
optional libc/runtime constraints
path
hash
signature/provenance metadata
```

Never select binaries by untrusted filename convention alone.

## Provided capabilities

Each provided capability includes:

```text
capability ID
provider instance ID if needed
contract version
cardinality
scope
priority/default hints
configuration schema
```

Cardinality values:

```text
MULTI
ORDERED
SELECT_ONE
EXCLUSIVE
```

## Requirements

Requirements distinguish:

- required;
- optional;
- recommended.

A recommended missing capability may be surfaced in setup without blocking activation.

## Permissions

Permissions are explicit and install/enable UI should be able to explain them.

Permissions do not become granted simply because an extension asks for them.

## Resource claims

Claims are resources Nexa must reserve before activation, e.g.:

- HTTP routes;
- TCP ports;
- UDP ports;
- multicast groups;
- singleton semantic resources.

Claims are validated globally/scoped before executing extension startup.

## Setup steps

Steps have:

```text
stable step ID
ordering constraints
required flag
schema/custom UI contribution
completion validation
```

Cyclic setup ordering is an activation/configuration error.

## Configuration

Settings are schema described and namespaced.

Secrets are referenced through the core secrets service rather than represented directly in ordinary readable config after storage.

## Manifest parsing

Manifest parsing is untrusted-input parsing.

Requirements:

- strict schema;
- bounded sizes;
- reject duplicate keys/IDs;
- reject unknown security-sensitive enum values;
- normalize before conflict planning;
- do not execute extension code merely to discover static claims.

Dynamic claims may be supported only after static sandboxed preflight and must still pass the same planner.

## Manifest evolution

The package/manifest format itself is versioned separately from Nexa server version.

Unknown future mandatory manifest features cause a clear incompatibility error rather than partial activation.
