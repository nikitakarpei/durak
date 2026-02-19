# UI Spec: UC-004 Play Attack Card

- Covers Use Case: `docs/use-cases/UC-004-play-attack-card.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines exact attack-interaction presentation so HTML/CSS and tests remain consistent as visual details evolve.

## Screen States
1. Attack action is visible only when attack move is available.
2. Attack action is disabled when no hand card is selected.
3. Attack action is enabled when a valid hand card is selected.

## Interaction Rules
1. Selecting a hand card marks it as selected.
2. Selecting the same hand card again removes selection.
3. Submitting attack sends selected card as the attack move.
4. If no selection exists, submitting attack does not send a move.
5. If access token is unavailable, submitting attack does not send a move.

## Layout and Responsive Rules
1. Hand cards remain visible and actionable in a mobile-first layout.
2. After state update confirming played attack, the played card is removed from hand and shown on table.

## Copy Rules
- Current button label for attack action may change; behavior remains the contract.

## Accessibility Expectations
1. Attack action exposes disabled/enabled state semantically.
2. Hand card controls are keyboard-focusable and actionable.

## Out of Scope
- Client-side rules engine for validating legal attack cards beyond server-provided moves.
