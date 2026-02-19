# UC-002: Enter Existing Game ID and Connect

- Priority: Must
- Actor: Player
- Action: Join an existing game by game ID
- Related UI Spec: `docs/ui-specs/UC-002-enter-game-and-connect.md`

## Why This Use Case Is Needed
- Player needs a direct way to enter an existing game and start playing.

## Preconditions
- App shell is loaded
- Player is on the join screen

## Trigger
- Player enters game ID (and optional player identifier) and presses Connect

## Main Flow
1. Player enters game ID and optional identifier.
2. App starts connection and shows connecting state.
3. App completes connection for the selected game.
4. Match screen is shown.

## Alternative / Failure Flows
- If game ID is empty, connection does not start.
- If connection fails at any step, app enters error state.

## Postconditions
- Player is connected to the selected existing game.

## Observable Changes
- Connect button is disabled while connecting.
- On success, UI switches to match screen.
- On failure, error message is shown.

## Out of Scope (MVP)
- Creating a new game
- Deep-link based game entry
