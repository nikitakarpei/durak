# UI Spec: UC-005 Beat Attack Card

- Covers Use Case: `docs/use-cases/UC-005-beat-attack-card.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines exact defend interaction rules so explicit target selection behaves consistently in UI, HTML/CSS, and tests.

## Screen States
1. Beat Attack action is visible only when beat move is available.
2. Beat Attack action is disabled until both selections are made (target attack + defender card).
3. Beat Attack action is enabled only when both required selections exist.

## Interaction Rules
1. Player selects a specific open attack card as defence target.
2. Player selects one defender card from hand.
3. Submitting defence sends both selected identifiers.
4. If either selection is missing, submitting defence does not send a move.
5. If access token is unavailable, submitting defence does not send a move.
6. After a table attack card is selected, hand cards that cannot beat it are visually faded.
7. After a hand card is selected, table attack cards that cannot be beaten by it are visually faded, even when none are beatable.
8. Clicking a non-beatable table attack card does not select it and triggers a short shake animation on that card.

## Layout and Responsive Rules
1. Table attack cards and hand cards remain independently selectable in mobile-first layout.
2. After state update confirming defence, selected defender appears in the target table pair and is removed from hand.

## Copy Rules
- Current label text for defence action may change; behavior remains the contract.

## Accessibility Expectations
1. Beat Attack action exposes disabled/enabled state semantically.
2. Table and hand selectable cards are keyboard-focusable and actionable.

## Out of Scope
- Client-side legality engine for validating every defend pair.
