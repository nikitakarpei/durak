# 2. MongoDB as primary datastore for service data

Date: 2026-02-07

## Status

Accepted

## Context

We need a primary datastore for service data for a home project. Early operational costs are critical, and we want an option that is free to start. We also want to avoid vendor lock-in and keep a path to scale if the project grows.

## Decision

We will use MongoDB as the primary datastore for service data. We will start with the free plan and keep the ability to self-host, while preserving a path to horizontal scaling if the project gains popularity.

## Consequences

- We can start with minimal operating costs using a free plan.
- We can self-host if needed, reducing vendor lock-in risk.
- We have a clear horizontal scaling path if usage grows.
- We accept the operational overhead of running and maintaining a document database when self-hosted.
