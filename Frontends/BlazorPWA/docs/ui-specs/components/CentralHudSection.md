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
   - `IsTurnActive` (`bool`) indicating whether the local player is the active turn holder.
   - `TurnTimerDuration` (`TimeSpan?`) for the active turn countdown duration.
   - `TurnTimerKey` (`string`) to restart the countdown animation when the turn changes.
2. Outputs:
   - None.
3. Required accessibility semantics:
   - HUD is presentational; card/deck visuals are non-interactive.
   - Role indicator is presentational only.

## Required States
1. Trump known: shows mini outlined card with centered rank and suit.
2. Trump unknown: shows placeholder content in the trump slot.
3. Deck count present: shows count value and mini outlined card-back icon with diagonal hatching.
4. Neutral role: no role icon is shown between trump and deck slots.
5. Defender role: shield emoji appears in the center slot.
6. Attacker role: crossed swords emoji appears in the center slot.
7. Sizing: the trump and deck sections use equal widths, while the role section may be narrower; the HUD height is compact and not stretched by surrounding layout (e.g., when the actions section is empty).
8. When `IsTurnActive` and `TurnTimerDuration` are set, a rounded countdown border appears and depletes clockwise around the HUD.
9. Countdown color phases: neutral (100–50%) `#2563eb`, warning (50–20%) `#f59e0b`, urgent (20–0%) `#dc2626` with a subtle pulse in urgent range, then disappears at zero.

## Test Hooks
- `data-testid='central-hud'`
- `data-testid='central-trump'`
- `data-testid='central-deck'`
- `data-testid='central-count'`
- `data-testid='central-role'` on the role icon slot when visible.
- `data-testid='central-hud-timer'` on the active countdown border.

## Out of Scope
- Move logic and deck ownership rules.
