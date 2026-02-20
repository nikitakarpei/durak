# Component Contract: HandSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-003-view-match-state.md`, `docs/use-cases/UC-004-play-attack-card.md`, `docs/use-cases/UC-005-beat-attack-card.md`, `docs/use-cases/UC-006-give-up-defence.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps hand-card rendering modular while page orchestration owns move calculations and selected-card state.

## Public UI Contract
1. Inputs:
   - `Cards` (`IReadOnlyList<Card>`) for currently visible hand cards.
   - `SelectedCardId` (`string?`) for selected hand card.
   - `PlayableCardIds` (`IReadOnlySet<string>`) for currently playable cards.
   - `BeatableHandCardIds` (`IReadOnlySet<string>`) for currently beatable hand cards.
   - `HasSelectedAttackCard` (`bool`) indicating whether the player has selected a table attack card.
2. Outputs:
   - `OnCardSelected(string)` when user selects a hand card.
3. Required accessibility semantics:
   - Hand cards are actionable controls.

## Required States
1. Empty hand.
2. Populated hand.
3. Selected card state.
4. Playable/beatable highlight states.
5. Hand card face layout matches table cards with rank stacked above suit and compact spacing.
6. When a table attack card is selected, non-beatable and non-selected hand cards are visually faded.

## Test Hooks
- `data-testid='hand-section'`
- `data-testid='hand-scroll'`
- `data-testid='hand-card-<cardId>'`
- `data-visual-state='unbeatable'` on non-beatable, non-selected hand cards when a table attack card is selected.
- `data-visual-state='invalid'` on the clicked hand card while the invalid selection shake is active.

## Out of Scope
- Move legality computation and selected-card toggle logic ownership.
