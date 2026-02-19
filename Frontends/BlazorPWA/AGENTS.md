# Agent Guidelines

## Core Workflow
1. **Use `docs/` when needed.**
2. **Apply strict TDD:** design abstractions, write/adjust tests first, run them to fail (red), then implement and run them to pass (green).
3. **For UI component changes, use spec-driven TDD order:** component contract spec -> tests (red) -> implementation (green).
4. **Commit all task changes.**
5. **When fixing problems, avoid immediate quick patches.** First identify the root cause, then propose a long-term solution that prevents similar issues in the future.

## Use Cases
1. **Before implementing any user-visible behavior, update/create the relevant use case in `docs/use-cases/` and related UI spec in `docs/ui-specs/`.**
2. **Use cases must contain only user-observable behavior and user value.**
3. **UI specs must contain exact UI contracts needed for implementation and testing.**
4. **Do not include implementation details in use cases or UI specs.** Exclude logs, events, APIs, services, reducers, storage, and technical workflows.
5. **Every user-visible code change must update use cases/UI specs in the same task.**
6. **Every user-visible UI change must satisfy `docs/ui-specs/HTML-CSS-GENERATION-CHECKLIST.md` and update the relevant UI spec accordingly.**

## UI Modularity Enforcement
1. **User-visible UI changes must be implemented in section components, not by expanding monolithic page files.**
2. **Page files act as orchestration only** (state wiring and intent routing), while rendering/interaction details belong to section components.
3. **Each new or changed section component must have a component contract spec** in `docs/ui-specs/components/` (or update an existing one).
4. **Each section component must define explicit inputs, outputs, and stable test hooks** (`data-testid`).
5. **Do not add hidden cross-component dependencies.** Components may only depend on declared parameters/contracts.

## Test Policy (Strict)
1. **Test user-observable behavior, not internals.**
2. **Test public contracts per layer.** In frontend, prioritize user-facing UI contracts (actions, visibility, enabled/disabled state, accessibility semantics) and boundary contracts (service/API calls), not component-private internals.
3. **Prefer stable selectors:** `data-testid` -> role/label -> text (last resort).
4. **Do not test styling/DOM internals** (CSS classes, exact markup structure) unless explicitly required behavior.
5. **Text assertions are opt-in only:** use them only when copy itself is a requirement (for example error/legal/contract text).
6. **Tests must be deterministic** (no real time/network/randomness).
7. **Use Given-When-Then test names** (`Given_<context>_When_<action>_Then_<outcome>`).
