# Agent Guidelines

## Before Starting Any Task

1. **Use `docs/` when needed.** Before or during a task, the agent can read documentation in the `docs/` folder when it is useful for understanding scope, conventions, architecture, or product behavior.

2. **Log every decision.** Document the reasoning behind each decision you make — what you decided, why, and what alternatives were considered.

## Use Case Rules

1. **Update use cases when UI behavior changes.** If UI behavior is changed, added, or removed, the corresponding files in `docs/use-cases/` must be updated in the same task.

2. **No implementation details in use cases.** Use cases must not include technical implementation details (for example APIs, RPC names, event names, classes, services, reducers, or internal architecture choices).

3. **User perspective first.** Use cases should always describe behavior from the user perspective (actor goals, triggers, observable outcomes, and failure behavior visible to the user).

## Delivery Approach

1. **Apply TDD before implementation.** Before implementing behavior, first think through and define the required abstractions, then write tests for the expected behavior, and only after that implement code to satisfy those tests.

2. **Focus frontend tests on behavior.** Good frontend tests validate user-observable behavior, not implementation internals.

3. **Avoid styling/DOM-internal assertions.** Do not test CSS classes, exact DOM structure, or implementation-only markup unless it is user-observable behavior (for example visibility, enabled/disabled state, role/name, text, and actionable controls).

4. **Commit all changes.** Every completed task must end with a git commit that includes all modified and new files related to that task.
