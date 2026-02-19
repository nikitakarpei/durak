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

## Test Hooks
- `data-testid='opponents-section'`
- `data-testid='opponent-seat'`
- `data-testid='opponent-hand'`
- `data-testid='opponent-card-back'`

## Out of Scope
- Seat allocation/orchestration logic and player identity mapping rules.
