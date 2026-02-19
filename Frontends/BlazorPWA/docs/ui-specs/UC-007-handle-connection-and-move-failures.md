# UI Spec: UC-007 Handle Connection and Move Failures

- Covers Use Case: `docs/use-cases/UC-007-handle-connection-and-move-failures.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines consistent error presentation across join and match contexts so users receive clear, immediate failure feedback.

## Screen States
1. During join flow failure, join screen remains visible and shows inline error feedback.
2. During match flow failure, match screen remains visible and shows error toast feedback.

## Interaction Rules
1. When connection/move failure state is entered with an error message, corresponding error feedback appears.
2. Error feedback content is sourced from current error state.
3. Failure prevents in-progress action flow from continuing.

## Layout and Responsive Rules
1. Error feedback is visible without leaving current context (join or match) in mobile-first layout.

## Copy Rules
- Exact error wording may change based on backend/source; behavior is to show available error feedback.

## Accessibility Expectations
1. Error feedback is visually distinguishable from normal state.
2. Error feedback remains readable in current viewport.

## Out of Scope
- Automatic recovery or retry flows.
- Guided troubleshooting wizards.
