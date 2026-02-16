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
- Cards in the player's hand are displayed as recognisable playing cards: white background, rounded corners, drop shadow, and the rank and suit shown in the top-left corner.
- Red suits (Hearts, Diamonds) appear in red; black suits (Spades, Clubs) appear in dark.
- Cards in hand overlap slightly in a fan layout to conserve horizontal space; the selected card lifts above the others.

## Out of Scope (MVP)
- Desktop-first layout optimization
