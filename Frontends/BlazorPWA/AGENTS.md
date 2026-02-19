# Agent Guidelines

## Core Workflow
1. **Use `docs/` when needed.**
2. **Apply TDD:** design abstractions, write failing tests, then implement.
3. **Commit all task changes.**

## Document Types
1. **Product (`docs/Product.md`)**
   Why: define scope and priorities.
   Rules: business/user value only; no UI implementation or technical internals.
2. **Use Cases (`docs/use-cases/`)**
   Why: define what user-visible behavior must happen.
   Rules: actor goals/triggers/outcomes/failures only; no implementation details.
3. **UI Specs (`docs/ui-specs/`)**
   Why: define exact UI behavior needed for HTML/CSS generation (states, copy, layout, responsive, accessibility expectations).
   Rules: user-visible UI contracts only; no framework internals.

## Use Cases
1. **Use cases first for user-visible changes.** Before implementing any user-visible behavior, update/create the relevant file in `docs/use-cases/`.
2. **Write only user value and user-observable behavior.** Describe actor goals, triggers, outcomes, and user-visible failures.
3. **No implementation details in use cases.** Exclude logs, events, APIs, services, reducers, storage, and technical workflows.
4. **Keep use cases behavior-focused; put detailed UI rules in `docs/ui-specs/`.** Link the related UI spec from the use case when exact copy/visual details matter.

## Test Policy (Strict)
1. **Test user-observable behavior, not internals.**
2. **Test public contracts per layer.** In frontend, prioritize user-facing UI contracts (actions, visibility, enabled/disabled state, accessibility semantics) and boundary contracts (service/API calls), not component-private internals.
3. **Prefer stable selectors:** `data-testid` -> role/label -> text (last resort).
4. **Do not test styling/DOM internals** (CSS classes, exact markup structure) unless explicitly required behavior.
5. **Text assertions are opt-in only:** use them only when copy itself is a requirement (for example error/legal/contract text).
6. **Tests must be deterministic** (no real time/network/randomness).
7. **Use Given-When-Then test names** (`Given_<context>_When_<action>_Then_<outcome>`).
