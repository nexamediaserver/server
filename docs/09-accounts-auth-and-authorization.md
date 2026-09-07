# Accounts, Authentication, and Authorization

## Core ownership

Nexa core owns the user/account model.

External authentication systems authenticate or provision identities; they do not replace Nexa's internal user object.

A Nexa user may contain:

```text
id
display/profile data
preferences
roles/permissions
playback history
ratings/user data
sessions
linked identities
created/updated state
```

## Separate concepts

Keep these concerns distinct:

### Authentication

Who is the caller?

### Identity linkage/provisioning

Which external identity maps to which Nexa user?

### Authorization

What may the Nexa user do?

Do not let an OIDC/LDAP provider become the authorization engine by accident.

## Native/local authentication

The baseline server must support a local administrator account without requiring any extension or external identity service.

Additional local-user features may remain core or be moved behind providers only if first-run reliability is preserved.

## External identity providers

Extensions may add:

- OpenID Connect;
- LDAP;
- SAML;
- Kerberos;
- passkey/WebAuthn-related integrations;
- other providers.

Provider capabilities may include:

```text
authenticate
identity discovery
directory lookup
group lookup
user provisioning
group-to-role mapping
logout/federation hooks
```

## Linked identities

Conceptually:

```text
user
  └── linked_identity
        provider_id
        provider_subject
        claims/metadata
        linkage timestamps
```

Provider subjects and issuer/provider identity together must be stable and unique.

## Role/group mapping

An extension may map external groups to Nexa roles, e.g.:

```text
LDAP group "media-admins"
  -> Nexa role "Library Administrator"
```

Core authorization still evaluates the final Nexa permissions.

## Authorization model

The model should support at least:

- server administration;
- extension administration;
- library-specific administration;
- library/content read access;
- content modification;
- playback permission;
- user/profile management;
- provider/network configuration.

Permission checks must be performed server-side even if the UI hides inaccessible controls.

## Extension access to account services

Extensions consume a versioned `nexa.accounts`/authorization service rather than database tables.

Permissions constrain what extensions can access.

Examples:

```text
accounts.lookup.basic
accounts.read.groups
accounts.provision
auth.register-provider
authorization.map-external-groups
```

Avoid exposing password hashes, secrets, or session internals.

## Setup administrator

The administrator is created early in the setup wizard.

Before the first admin exists, setup access must be protected:

- localhost-only by default where feasible; and/or
- a one-time setup token printed to the local console/log.

After admin creation, the one-time bootstrap credential is invalidated permanently.

## Sessions

Core owns browser/API sessions.

Session persistence must work with SQLite alone.

External providers may influence authentication, but successful authentication yields a Nexa-owned session/token.

## Secrets

Extension credentials such as OIDC client secrets or API tokens must use a core secrets service, not plaintext extension configuration where avoidable.

The secrets API must enforce namespace and permission boundaries.

Secret values should not be returned to frontend clients after initial submission unless explicitly required.

## Account extension conflicts

Multiple identity providers may coexist.

Login UI/provider selection should be explicit and deterministic.

If a configuration requires one primary provider, this is a SELECT_ONE capability scoped appropriately rather than a load-order convention.

## Remote access identity

A Plex-like Nexa remote-access/DDNS/account extension may introduce an external Nexa service identity while still mapping access to local Nexa users and permissions.

The remote-access extension must not bypass local authorization.

## Auditability

Security-sensitive account operations should produce audit-capable events/logs:

- admin creation;
- identity linking/unlinking;
- role changes;
- provider changes;
- authentication failures/rate limits;
- session revocation;
- extension permission grants.
