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
4. Seats arranged in up to two horizontal tier rows. Tier assignment by seat count:
   - 1 seat  → tier 1: center
   - 2 seats → tier 1: left + right
   - 3 seats → tier 1: left + center + right
   - 4 seats → tier 1: left + right / tier 2: outer-left + outer-right
   - 5 seats → tier 1: left + center + right / tier 2: outer-left + outer-right

## Test Hooks
- `data-testid='opponents-section'`
- `data-testid='opponents-tier'` + `data-tier='1'` or `data-tier='2'` on each tier row.
- `data-testid='opponent-seat'` + `data-tier='1'` or `data-tier='2'` on each seat tile.
- `data-testid='opponent-hand'`
- `data-testid='opponent-card-back'`

## Out of Scope
- Seat allocation/orchestration logic and player identity mapping rules.
