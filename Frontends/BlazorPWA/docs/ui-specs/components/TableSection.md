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
2. Outputs:
   - `OnAttackCardSelected(string)` when user selects an open attack card.
3. Required accessibility semantics:
   - Open attack cards are actionable controls.
   - Defended attack cards are non-actionable content.

## Required States
1. Empty table (no pairs).
2. Open attack pair (attack selectable, defence empty slot shown as dashed placeholder).
3. Defended pair (attack non-selectable; defence card rendered overlaid on the attack card, offset 10px down-right and rotated 20° clockwise, so both cards remain visible).
4. Selected/beatable highlighting states for open attack cards.

## Test Hooks
- `data-testid='table-section'`
- `data-testid='table-pair'`
- `data-testid='table-attack-<cardId>'`
- `data-testid='table-defence-<cardId>'`

## Out of Scope
- Computing legal beat targets and selection rules.
