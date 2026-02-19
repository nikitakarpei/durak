# Component Contract: TalonHudSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-003-view-match-state.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps the compact talon HUD modular and reusable while the page orchestrates game state.

## Public UI Contract
1. Inputs:
   - `TrumpCard` (`Card?`) for the current trump card (null when unknown).
   - `TalonCount` (`int`) for remaining deck count.
2. Outputs:
   - None.
3. Required accessibility semantics:
   - HUD is presentational; card/deck visuals are non-interactive.

## Required States
1. Trump known: shows mini card face with rank and suit.
2. Trump unknown: shows placeholder content in the trump slot.
3. Deck count present: shows count value and deck icon.

## Test Hooks
- `data-testid='talon-hud'`
- `data-testid='talon-trump'`
- `data-testid='talon-deck'`
- `data-testid='talon-count'`

## Out of Scope
- Move logic and deck ownership rules.
