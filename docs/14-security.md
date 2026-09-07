# Security and Trust Boundaries

## Security model

Nexa is a server that:

- reads potentially hostile media and metadata;
- exposes network services;
- executes third-party extensions;
- invokes complex native media tools;
- stores user credentials/tokens;
- may be exposed remotely.

Security boundaries must be explicit.

## Trust zones

```text
Untrusted clients
      │
      ▼
HTTP/API authorization boundary
      │
      ▼
Nexa core
  │       │        │
  │       │        └── secrets/account store
  │       │
  │       └── sandboxed WASM extensions
  │
  └── supervised native providers/extensions
             │
             └── untrusted media inputs
```

## Setup security

Unconfigured servers are vulnerable to administrator takeover.

Before first-admin creation:

- setup should be localhost-only by default where practical and/or require a one-time bootstrap token;
- token entropy must be sufficient;
- token expiration/reissue policy must be explicit;
- normal APIs are unavailable.

After first-admin creation, bootstrap credentials are invalid.

## Authentication

- password storage uses a modern password hashing scheme with safe parameters;
- authentication endpoints are rate-limited;
- external provider callbacks validate issuer/state/nonce as relevant;
- sessions/tokens are revocable;
- sensitive cookies use appropriate `HttpOnly`, `Secure`, and SameSite settings depending on deployment.

Exact crypto libraries/parameters require a security-focused ADR/review at implementation time.

## Authorization

Authorization is server-side and deny-by-default for protected resources.

Every resource access path must account for:

- user identity;
- library/object access;
- administrative scope;
- extension ownership where relevant.

Media URLs are not authorization bypasses.

## CSRF/CORS

Browser APIs need an explicit same-origin/CORS policy.

State-changing cookie-authenticated requests need CSRF protection appropriate to the session model.

Do not enable permissive `*` CORS as a convenience default.

## Extension sandboxing

WASM extensions receive only granted host functions/capabilities.

No implicit access to:

- server filesystem;
- arbitrary environment variables;
- database;
- network;
- secrets;
- other extensions.

Outbound HTTP is brokered and permission controlled.

## Native provider isolation

Native media providers are privileged relative to WASM and require extra controls.

Where practical:

- run as child processes with minimal environment;
- expose only controlled input/output resources;
- restrict filesystem visibility;
- apply OS sandboxing where feasible per platform;
- set time/memory/process limits;
- kill provider process trees on cancellation/timeout.

The architecture must not rely on providers being memory-safe.

## Untrusted media

All media and metadata are untrusted input.

Protect against:

- decompression bombs;
- parser bugs;
- malformed containers;
- path traversal;
- unsafe ImageMagick delegates/coders;
- malicious archive-like formats;
- huge dimension/resource requests;
- command injection into FFmpeg/ImageMagick arguments.

Provider commands MUST be constructed from structured arguments, never concatenated shell strings.

## Image transformation limits

Core enforces:

- maximum source/output dimensions;
- decoded-pixel limits;
- memory;
- temporary disk;
- runtime;
- output size.

ImageMagick providers must use a restrictive server-side security policy.

## Cache abuse

Named/bucketed rendition profiles are the default.

Do not allow unauthenticated callers to generate arbitrary unique dimension/quality combinations.

Rate/complexity-limit custom transformation endpoints.

## Query abuse

Validate and limit query AST complexity:

- maximum nesting;
- number of predicates;
- relation depth;
- sort/group limits;
- page size;
- expensive full-text/regex operators.

Extensions cannot inject raw SQL.

## Filesystem paths

Library roots are configured capabilities.

Normalize and validate all paths.

Do not serve arbitrary server filesystem paths because a client supplied them.

Symlink policy must be explicit and tested.

## SSRF

Metadata/remote-fetch extensions may cause server-side HTTP requests.

The outbound HTTP broker should support policy controls against:

- loopback/internal network access where inappropriate;
- cloud metadata endpoints;
- disallowed schemes;
- DNS rebinding risks;
- unbounded redirects/download sizes.

Some trusted integrations may require private-network access and should request that permission explicitly.

## Secrets

Use a core secret store/service.

Rules:

- secrets are namespaced;
- extensions see only granted secrets;
- secrets are redacted from logs/errors;
- frontend reads do not return stored secret values by default;
- backup/export semantics are explicit.

## Extension provenance

The distribution system should support package integrity and signature verification.

Whether third-party unsigned extensions are allowed is a product-policy decision, but the administrator must be able to distinguish trust/provenance.

## Dependency security

CI should include:

- Go vulnerability scanning;
- frontend dependency auditing;
- extension/provider artifact verification;
- reproducible dependency lock/update review.

Avoid dynamically downloading executable provider binaries without integrity verification.

## Audit events

Security-sensitive changes should be auditable:

- admin/user creation;
- role/permission changes;
- extension install/enable/permission grant;
- auth provider changes;
- remote-access changes;
- network listener claims;
- secret rotation where metadata can be logged safely.
