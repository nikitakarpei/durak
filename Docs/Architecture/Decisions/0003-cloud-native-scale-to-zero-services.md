# 3. Cloud-native services with scale-to-zero

Date: 2026-02-07

## Status

Accepted

## Context

We want to minimize operational costs for this project. This requires services to run only when needed and to start quickly to handle demand.

## Decision

All services will be cloud-native and must be able to scale down to zero instances and spin up quickly on demand.

## Consequences

- Idle cost is reduced because services do not run continuously.
- We must choose platforms and runtimes that support fast cold starts.
- We need to design services to be stateless or to externalize state so scale-to-zero is safe.
