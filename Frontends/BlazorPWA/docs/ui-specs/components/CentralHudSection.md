# Component Contract: CentralHudSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-003-view-match-state.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps the compact central HUD modular and reusable while the page orchestrates game state.

## Public UI Contract
1. Inputs:
   - `TrumpCard` (`Card?`) for the current trump card (null when unknown).
   - `TalonCount` (`int`) for remaining deck count.
   - `Role` (`CentralHudRole`) for current player responsibility (`Neutral`, `Attacker`, `Defender`).
2. Outputs:
   - None.
3. Required accessibility semantics:
   - HUD is presentational; card/deck visuals are non-interactive.
   - Role indicator is presentational only.

## Required States
1. Trump known: shows mini card face with rank and suit.
2. Trump unknown: shows placeholder content in the trump slot.
3. Deck count present: shows count value and deck icon.
4. Neutral role: no role icon is shown between trump and deck slots.
5. Defender role: shield icon appears in the center slot.
6. Attacker role: forward arrow icon appears in the center slot.
7. Rigid size: the pill, trump slot, and count slot are fixed-width; content changes (rank length, digit count) must not change the physical dimensions of the HUD. The HUD height must also be stable — it must not be stretched by the surrounding grid layout (e.g., when the actions section is empty).

## Test Hooks
- `data-testid='central-hud'`
- `data-testid='central-trump'`
- `data-testid='central-deck'`
- `data-testid='central-count'`
- `data-testid='central-role'` on the role icon slot when visible.

## Out of Scope
- Move logic and deck ownership rules.
