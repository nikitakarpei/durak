# UC-003: View Match State on Mobile-First Screen

- Priority: Must
- Actor: Player
- Action: Observe current match status and cards

## Preconditions
- Player is connected to a game

## Trigger
- Match screen is open and game state changes

## Main Flow
1. App shows game header information.
2. App shows all opponent seats configured for the game, including seats not yet occupied.
3. App shows table cards.
4. App shows player hand and action controls.

## Alternative / Failure Flows
- If some data is not yet available, placeholders are shown.

## Postconditions
- Player can see the current match state.

## Observable Changes
- Turn, table, hand, trump, and talon information update over time.
- The talon count decreases each time cards are dealt to a player.
- Any unoccupied opponent seat within the configured seat count is shown as "Not joined" until a player joins it.

## Out of Scope (MVP)
- Rich card art
- Desktop-first layout optimization
