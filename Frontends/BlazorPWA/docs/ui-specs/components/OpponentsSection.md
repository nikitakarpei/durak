# Component Contract: OpponentsSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-003-view-match-state.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps opponent-seat rendering modular and stable while page-level logic handles seat orchestration.

## Public UI Contract
1. Inputs:
   - `Seats` (`IReadOnlyList<OpponentSeatViewModel>`) with `Seat`, `IsJoined`, `CardCount`.
2. Outputs:
   - None (presentational section).
3. Required accessibility semantics:
   - Opponent section remains readable and structured across joined and unjoined states.

## Required States
1. Mixed joined/unjoined seats.
2. Joined seat with card backs (`CardCount > 0`).
3. Unjoined seat with placeholder and no card backs.
4. Seats distributed in a symmetric U-shaped layout that adapts to the number of opponent seats:
   - 1 seat → top-center; 2 seats → upper-left and upper-right; 3 seats → left-middle, top-center, right-middle;
     4 seats → left-middle, upper-left, upper-right, right-middle; 5 seats → all five positions.

## Test Hooks
- `data-testid='opponents-section'`
- `data-testid='opponent-seat'`
- `data-testid='opponent-hand'`
- `data-testid='opponent-card-back'`
- `data-seat-position='1..5'` on each opponent seat in visual order.

## Out of Scope
- Seat allocation/orchestration logic and player identity mapping rules.
