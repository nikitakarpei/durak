# UI Spec: UC-002 Enter Existing Game and Connect

- Covers Use Case: `docs/use-cases/UC-002-enter-game-and-connect.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines exact join-screen behavior and states so implementation and tests stay aligned when copy/layout evolves.

## Screen States
1. Join screen shows game ID input, optional custom ID input, and Connect action.
2. While connecting, Connect action is visible but disabled.
3. After successful connection, match screen replaces join screen.
4. On connection failure, an inline error is shown on join screen.

## Interaction Rules
1. Connect action starts connection only when game ID is non-empty.
2. Game ID and custom ID values are trimmed before connection request.
3. Empty game ID does not trigger a connection request.

## Layout and Responsive Rules
1. Join screen uses a compact single-column card-style panel suitable for mobile-first entry.

## Copy Rules
- Current UI labels/placeholders may change over time; tests should not depend on exact wording unless copy is explicitly required.

## Accessibility Expectations
1. Inputs are presented with visible labels.
2. Disabled connect state is perceivable and not actionable.

## Out of Scope
- Game creation flow.
- Deep-link based automatic game entry.
