# Architecture Decision Records

ADRs capture decisions that should not be casually reversed during implementation.

| ADR | Decision |
|---|---|
| [0001](0001-go-server.md) | Implement Nexa core server in Go |
| [0002](0002-sqlite-first.md) | SQLite is the reference/default database |
| [0003](0003-single-process-baseline.md) | Baseline deployment is a single Nexa process |
| [0004](0004-capability-broker.md) | Extensions use capability-brokered versioned contracts |
| [0005](0005-agent-model.md) | Generalize people/organizations into Agents |
| [0006](0006-typed-runtime-fields.md) | Store custom fields using a portable typed runtime model |
| [0007](0007-core-owned-media-lifecycle.md) | Core owns media lifecycle; providers own transforms |
| [0008](0008-server-authoritative-playback.md) | Playback sequencing is server authoritative |
| [0009](0009-out-of-process-native-providers.md) | Native media providers run out of process |
| [0010](0010-mandatory-setup-wizard.md) | First run requires a resumable setup wizard |
| [0011](0011-contract-first-http-api.md) | Public HTTP API is contract-first OpenAPI |
| [0012](0012-no-required-external-infrastructure.md) | Redis/Postgres/object storage are never baseline requirements |
| [0013](0013-sqlc-for-static-repositories.md) | sqlc for static repositories; query AST compiler stays hand-written |

New ADRs should use the next sequential number and include Context, Decision, Consequences, and Status.
