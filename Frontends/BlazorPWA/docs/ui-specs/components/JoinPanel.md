# Component Contract: JoinPanel

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-001-offline-app-shell.md`, `docs/use-cases/UC-002-enter-game-and-connect.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps join-entry behavior isolated from page orchestration so offline shell entry and connect flow remain maintainable.

## Public UI Contract
1. Inputs:
   - `GameId` (current game id input value)
   - `CustomId` (current optional custom id input value)
   - `IsConnecting` (connect button disabled state)
   - `ShowError` and `ErrorMessage` (inline error visibility/content)
2. Outputs:
   - `GameIdChanged(string)`
   - `CustomIdChanged(string)`
   - `OnConnect()`
3. Required accessibility semantics:
   - Labeled game/custom id input fields
   - Disabled connect action when connecting

## Required States
1. Default join-entry state.
2. Connecting state (connect disabled).
3. Error state (inline error shown).

## Test Hooks
- `data-testid='join-panel'`
- `data-testid='join-title'`
- `data-testid='game-id-input'`
- `data-testid='custom-id-input'`
- `data-testid='connect-button'`
- `data-testid='join-error'`

## Out of Scope
- Connection orchestration logic and network calls.
- Match-screen rendering.
