# UC-007: Show Errors for Connection and Move Failures

- Priority: Must
- Actor: Player
- Action: Receive clear error feedback when connection or move submission fails
- Related UI Spec: `docs/ui-specs/UC-007-handle-connection-and-move-failures.md`

## Why This Use Case Is Needed
- Player needs clear feedback when actions fail so they understand current state and can decide next steps.

## Preconditions
- Player is connecting, connected, or submitting a move

## Trigger
- A connection step fails, live updates stop unexpectedly, or a move submission fails

## Main Flow
1. App detects failure.
2. App enters error state.
3. App shows generic error feedback to player.

## Alternative / Failure Flows
- If failure happens during active match, session remains unusable until reconnect.

## Postconditions
- Player is informed that the operation failed.

## Observable Changes
- Error feedback appears on screen.
- Connection flow or move flow does not continue.

## Out of Scope (MVP)
- Automatic retry/recovery
- Error-specific recovery wizards
