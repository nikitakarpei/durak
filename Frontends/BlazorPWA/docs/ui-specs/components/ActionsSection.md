# Component Contract: ActionsSection

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-004-play-attack-card.md`, `docs/use-cases/UC-005-beat-attack-card.md`, `docs/use-cases/UC-006-give-up-defence.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps action-button rendering and interaction semantics modular while page orchestration owns move availability and selection state.

## Public UI Contract
1. Inputs:
   - `CanPlayAttack` (whether Play Attack action is visible)
   - `CanGiveUpDefence` (whether Give Up Defence action is visible)
   - `IsPlayAttackEnabled` (whether Play Attack action is enabled)
2. Outputs:
   - `OnPlayAttack()`
   - `OnGiveUpDefence()`
3. Required accessibility semantics:
   - Action buttons expose disabled/enabled state semantically.

## Required States
1. No available moves (no buttons shown).
2. Attack-only move available (Play Attack visible; enabled depends on selection).
3. Give-up move available (Give Up Defence visible).

## Test Hooks
- `data-testid='actions-section'`
- `data-testid='play-attack-button'`
- `data-testid='give-up-defence-button'`

## Out of Scope
- Move availability calculation and selected-card state ownership.
