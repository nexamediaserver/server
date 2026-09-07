# Network Services and Protocol Extensions

## Purpose

Some extensions need to expose externally visible protocols rather than merely call Nexa core APIs.

Examples:

- DLNA/UPnP;
- OPDS;
- remote-access/DDNS;
- discovery protocols;
- specialized HTTP APIs;
- future casting/control protocols.

Nexa therefore needs a network resource broker, not merely an extension HTTP callback.

## Core owns network resources

Extensions MUST NOT blindly bind arbitrary sockets or mutate the core router.

They request resources from `nexa.network`/`nexa.api`.

Supported resource types may include:

- HTTP route mounts;
- TCP listeners;
- UDP listeners;
- multicast memberships;
- mDNS/DNS-SD advertisements;
- SSDP/discovery integration;
- WebSocket endpoints;
- protocol-specific advertisements.

Core tracks ownership and lifecycle.

## HTTP route model

Extensions receive an automatically safe namespaced route space such as:

```text
/api/extensions/{extension-id}/...
```

for ordinary APIs.

Protocol compatibility may require canonical routes such as:

```text
/opds
```

Those require explicit route claims.

Core route spaces such as these should be reserved:

```text
/api/core/*
/setup/*
/media/*
/playback/*
/admin/*
```

Exact reserved paths are defined by the public route registry.

## Route claim conflicts

Claims must specify enough information to detect overlap before activation.

Examples:

```text
extension A claims /opds/*
extension B claims /opds/admin/*
```

The router/resource planner decides whether the second is nested-compatible or conflicting based on explicit claim rules.

Never allow activation order to decide ownership.

## TCP/UDP claims

A socket resource is identified by properties such as:

```text
address scope
protocol
port
IP family
reuse semantics
multicast group if applicable
```

Two extensions claiming incompatible ownership of the same binding must fail planning before either is activated.

## DLNA/UPnP

A DLNA extension may need:

- SSDP on UDP multicast;
- device/service descriptions;
- event subscriptions;
- HTTP content/control endpoints;
- media delivery URLs.

The extension should reuse Nexa's asset/media delivery services where possible rather than implementing separate transcoding or authorization logic.

Nexa core still owns playback/media policy where requests correspond to Nexa playback semantics.

## OPDS

An OPDS extension primarily projects Nexa objects/libraries into the OPDS protocol and claims appropriate HTTP routes.

It consumes:

- query/object services;
- account/authorization services;
- media assets;
- optional search.

It must not read database tables directly.

## Remote access / DDNS

A remote-access provider may provide:

- server registration;
- account linking;
- DDNS/subdomain assignment;
- TLS/tunnel integration;
- reachability testing;
- discovery.

The setup wizard can add extension-provided configuration steps only when such a provider is installed/enabled.

## TLS and reverse proxies

Baseline Nexa should work directly on a local HTTP endpoint.

Advanced deployment may place a reverse proxy in front of Nexa.

Extension network APIs must not assume that public URLs are equivalent to local listener addresses.

Core should provide canonical external/base URL resolution and trusted-proxy configuration.

## Discovery

Discovery resources should be brokered to avoid duplicate announcements, port collisions, or inconsistent server identity.

A stable Nexa instance ID should be used where protocols require durable identity.

## Network extension permissions

Network claims are privileged.

Permissions may include:

```text
network.http-route
network.tcp
network.udp
network.multicast
network.discovery
network.public-advertisement
```

The administrator must be able to inspect active listeners and which extension owns each one.

## Diagnostics

Expose an administrator-facing network diagnostics view:

```text
listener
protocol
address/port
owning extension
status
last error
advertisement state
```

This is especially important on home networks where firewall and multicast behavior are difficult to debug.

## Lifecycle

On extension disable/upgrade/failure:

- stop accepting new connections;
- release listeners/multicast memberships;
- unregister advertisements;
- drain/cancel in-flight work according to the protocol contract;
- update diagnostics.

A failed network extension must not leave hidden background listeners running.
