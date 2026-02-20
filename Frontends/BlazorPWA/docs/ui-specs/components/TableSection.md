# Component Contract: TableSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-003-view-match-state.md`, `docs/use-cases/UC-005-beat-attack-card.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps table-pair rendering and attack-target selection modular while page orchestration owns move-state calculations.

## Public UI Contract
1. Inputs:
   - `Pairs` (`IReadOnlyList<TablePair>`) representing current table state.
   - `SelectedAttackCardId` (`string?`) for selected target attack card.
   - `BeatableAttackCardIds` (`IReadOnlySet<string>`) for currently beatable attack cards.
   - `HasSelectedHandCard` (`bool`) indicating whether the player has selected a hand card.
   - `CanPlayAttack` (`bool`) indicating whether an attack move is available.
2. Outputs:
   - `OnAttackCardSelected(string)` when user selects an open attack card.
   - `OnPlayAttack()` when user clicks the highlighted table area to submit the selected hand card.
3. Required accessibility semantics:
   - Open attack cards are actionable controls.
   - Defended attack cards are non-actionable content.
   - The highlighted table area is an actionable control when visible.
4. Card face layout matches hand cards with rank stacked above suit and compact spacing.

## Required States
1. Empty table (no pairs).
2. Open attack pair (attack selectable).
3. Defended pair (attack non-selectable; defence card overlaid on the attack card, offset and rotated, so both cards remain visible).
4. Selected/beatable highlighting states for open attack cards.
5. When a hand card is selected, non-beatable and non-selected attack cards are visually faded.
6. Clicking a non-beatable attack card does not select it and briefly shakes the card.
7. When `CanPlayAttack` and `HasSelectedHandCard` are true, the table area shows a subtle highlight and is clickable to submit the attack.

## Test Hooks
- `data-testid='table-section'`
- `data-testid='table-pair'`
- `data-testid='table-attack-<cardId>'`
- `data-testid='table-play-target'` on the highlighted table area when visible.
- `data-visual-state='unbeatable'` on non-beatable, non-selected attack cards when a hand card is selected.
- `data-visual-state='invalid'` on the clicked attack card while the invalid selection shake is active.

## Out of Scope
- Computing legal beat targets and selection rules.
