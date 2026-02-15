# UC-006: Give Up Defence

- Priority: Must
- Actor: Player
- Action: Give up current defence

## Preconditions
- Player is connected to a game

## Trigger
- Player presses Give Up Defence

## Main Flow
1. Player submits give-up action.
2. App sends the move request.
3. Match state updates and reflects result.

## Alternative / Failure Flows
- If request fails, app shows error state.

## Postconditions
- Give-up intent is submitted.
- All cards from the table are added to the defender's hand.
- The table is cleared.

## Observable Changes
- The Give Up Defence button is visible only when the player is defending and give-up is an available move. No action buttons are shown otherwise.
- The table is emptied.
- All attack cards (and any partially beaten cards) that were on the table appear in the defender's hand.

## Out of Scope (MVP)
- Confirmation dialog before giving up
