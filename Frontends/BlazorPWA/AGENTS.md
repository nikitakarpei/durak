# Agent Guidelines

## Core Workflow
1. **Use `docs/` when needed.**
2. **Log decisions in `docs/Decisions.md`** with what/why/alternatives.
3. **Apply TDD:** design abstractions, write failing tests, then implement.
4. **Commit all task changes.**

## Use Cases
1. **Update `docs/use-cases/` whenever UI behavior changes.**
2. **Keep use cases user-facing only** (no implementation details).

## Test Policy (Strict)
1. **Test user-observable behavior, not internals.**
2. **Prefer stable selectors:** `data-testid` -> role/label -> text (last resort).
3. **Do not test styling/DOM internals** (CSS classes, exact markup structure) unless explicitly required behavior.
4. **Text assertions are opt-in only:** use them only when copy itself is a requirement (for example error/legal/contract text).
5. **Tests must be deterministic** (no real time/network/randomness).
