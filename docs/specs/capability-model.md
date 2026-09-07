# Capability Model — Draft Specification

## Capability identity

A capability has:

```text
namespaced ID
contract major version
provider
cardinality
scope
health/state
```

Example:

```text
nexa.media.image.transform@1
org.example.television.sequence@1
```

## Provider selection

Selection order is explicit:

1. hard per-scope administrator/user choice;
2. capability-specific policy;
3. compatible preferred priority;
4. deterministic fallback.

Ties that cannot be resolved safely produce a configuration conflict.

Extension load order MUST NOT participate.

## Scope resolution

A provider may be selected at:

```text
global
server
library
user
session
request
```

More-specific explicit configuration wins over broader defaults if the capability permits it.

## MULTI

All compatible providers remain available. Callers may choose based on request constraints.

Example: image transforms.

## ORDERED

Nexa stores a deterministic order. Every element receives the operation only where the contract explicitly defines chaining.

Example: post-processing pipeline.

Failure semantics must be specified by each ORDERED capability; do not assume "continue on error".

## SELECT_ONE

Several providers can be installed, but exactly one is chosen in a scope when capability is required.

Example: default sequence semantics for a content type.

## EXCLUSIVE

A resource may have at most one owner.

Example: exact socket binding.

## Version compatibility

Major capability contract versions are not assumed compatible.

A provider may expose multiple contract versions simultaneously during migrations.

The broker selects a mutually supported version.

## Health

Provider state includes:

```text
available
degraded
unavailable
starting
stopping
```

Selection avoids unavailable providers.

Fallback behavior is capability specific and must not violate logical semantics.

## Permission check

Provider registration does not bypass permissions.

Both:

- extension's permission to register/provide;
- caller's permission to invoke/consume

must be enforceable where relevant.

## Diagnostics

Broker exposes:

```text
capability
providers
selected provider by scope
selection reason
version
health
conflicts
missing requirements
```

to administrators.
