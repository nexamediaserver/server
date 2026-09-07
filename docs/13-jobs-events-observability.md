# Jobs, Events, and Observability

## Baseline requirement

Nexa must run background work without Redis or an external queue.

The default job scheduler and workers run inside the Nexa server process and persist canonical job state in the active Nexa database.

## Job model

Conceptual job fields:

```text
id
kind
owner/core-or-extension
payload/reference
status
priority
created_at
run_at
attempts
max_attempts
locked/claimed state
progress
last_error
cancellation state
```

Do not put unbounded binary payloads into job rows.

## Example job types

- library scans;
- metadata refresh;
- artwork retrieval;
- thumbnail pre-generation;
- media probe;
- optimized representation generation;
- exports;
- extension work;
- cleanup/garbage collection;
- index rebuild;
- remote-provider synchronization.

## SQLite behavior

The SQLite scheduler must account for a single-writer model.

Claim/update transactions should be short.

Workers may run concurrently after work is claimed.

Do not hold a write transaction throughout job execution.

## PostgreSQL behavior

PostgreSQL may use backend-native efficient claiming/locking strategies, while preserving the same job semantics.

## Priority

Core priority classes must integrate media work and general background work.

Interactive playback/media has precedence over background maintenance.

An extension must not be able to declare every task highest priority.

## Retry

Retry policy includes:

- retryable vs terminal error classification;
- bounded exponential backoff;
- maximum attempts;
- optional dead-letter/failed state;
- administrator retry action.

Jobs involving external side effects need idempotency safeguards.

## Cancellation

Long-running jobs must receive cancellation signals where feasible.

Providers/extensions must cooperate with cancellation.

## Scheduling

Support:

- immediate;
- delayed (`run_at`);
- recurring/scheduled jobs where needed.

Avoid implementing a second unrelated scheduler inside extensions.

## Events

The core event system announces domain/runtime facts.

Examples:

```text
object.created
object.updated
agent.updated
library.scan.started
library.scan.completed
playback.started
playback.completed
user.created
extension.activated
```

Event IDs are namespaced and versioned when payload compatibility requires it.

## Events are not commands

Events announce facts. They are not a mechanism for obtaining an authoritative answer.

If Nexa needs a result from an extension, use a provider/service contract.

## Delivery semantics

The baseline event system may provide at-least-once delivery for durable extension subscriptions if durability is required.

Event consumers must be designed for duplicate delivery where applicable.

Not every high-frequency internal signal needs durable persistence.

Distinguish:

- durable domain events;
- transient runtime notifications;
- UI progress messages.

## Extension jobs/events

Extensions use core services to schedule work and subscribe/publish within their permissions.

Extension shutdown must stop new job dispatch to the extension and handle in-flight tasks according to lifecycle policy.

## Structured logging

Use `log/slog`.

Every subsystem should log machine-readable fields rather than parsing prose.

Common fields:

```text
request_id
extension_id
provider_id
job_id
playback_session_id
library_id
user_id where appropriate
operation
duration
error_code
```

Never log secrets, credential material, access tokens, or raw passwords.

## Metrics

Expose optional internal metrics for:

- HTTP request latency/status;
- active playback sessions;
- direct/remux/transcode counts;
- provider utilization;
- transcode/derivative queue depth;
- job queue depth/latency;
- SQLite busy/transaction latency;
- extension failures;
- cache hits/misses/size;
- library scan throughput.

The baseline UI may consume an internal admin diagnostics API. Prometheus/OpenTelemetry export can be optional.

## Tracing

Distributed tracing is not a baseline deployment dependency.

Internal trace/correlation IDs should still connect:

```text
HTTP request
-> playback session
-> media plan
-> provider job
```

Optional OpenTelemetry export may be added behind configuration/extension.

## Health

Health is layered:

### Process health

Can Nexa serve?

### Readiness

Are canonical storage, migrations, and mandatory core services usable?

### Extension/provider health

Which optional capabilities are degraded?

A failed optional provider should not mark the entire server unavailable unless a configured critical dependency requires it.

## Admin diagnostics

Provide a diagnostics surface summarizing:

- DB backend/status;
- storage roots;
- active extensions;
- extension conflicts/failures;
- network listeners;
- provider capabilities;
- media work/resource usage;
- job failures;
- playback delivery reasons;
- cache status.

Diagnostics should prefer actionable explanations over raw stack traces.
