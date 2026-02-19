# UI Spec: UC-006 Give Up Defence

- Covers Use Case: `docs/use-cases/UC-006-give-up-defence.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines exact give-up interaction and resulting state presentation so implementation and tests remain aligned.

## Screen States
1. Give Up Defence action is visible only when give-up move is available.
2. When no moves are available, no action buttons are visible.

## Interaction Rules
1. Selecting Give Up Defence sends a give-up request.
2. If access token is unavailable, selecting Give Up Defence does not send a request.
3. After successful state update, table cards are moved into defender hand and table is empty.

## Layout and Responsive Rules
1. Action button remains reachable in mobile-first layout.
2. After state update, hand reflects added cards and table reflects cleared state.

## Copy Rules
- Current label text for give-up action may change; behavior remains the contract.

## Accessibility Expectations
1. Give Up Defence action is keyboard-focusable and actionable.

## Out of Scope
- Confirmation modal before give-up submission.
