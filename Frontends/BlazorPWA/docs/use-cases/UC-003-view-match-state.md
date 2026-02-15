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
2. App shows opponents with seats and card counts.
3. App shows table cards.
4. App shows a scrollable event log of past game actions.
5. App shows player hand and action controls.

## Alternative / Failure Flows
- If some data is not yet available, placeholders are shown.

## Postconditions
- Player can see the current match state.

## Observable Changes
- Turn, table, hand, trump, and talon information update over time.
- The talon count decreases each time cards are dealt to a player.
- The event log grows as the game progresses: each attack, beaten card, defence given up, round outcome, and fool declaration produces a new entry visible to the player.
- The player is labelled "You" in their own log entries; other players are labelled by their player ID.
- The log automatically scrolls to the latest entry.

## Out of Scope (MVP)
- Rich card art
- Desktop-first layout optimization
