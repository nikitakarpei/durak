# UC-004: Attacker Plays One of Their Hand Cards

- Priority: Must
- Actor: Player
- Action: As attacker, play one selected card from hand

## Preconditions
- Player is connected to a game
- It is player's attack turn
- Player has at least one card in hand
- Player selected one hand card

## Trigger
- Player presses Play Attack

## Main Flow
1. Player selects one card from hand.
2. Player submits attack.
3. App sends the move request.
4. Match state updates and reflects result.

## Alternative / Failure Flows
- If no hand card is selected, the Play Attack button is grayed out and the move is not sent.
- If request fails, app shows error state.

## Postconditions
- Attack intent is submitted.

## Observable Changes
- The Play Attack button is visible only when it is the player's attack turn. No action buttons are shown otherwise.
- Cards the player is allowed to play are visually marked (green tint) only during the player's own turn.
- The green tint is removed from all hand cards when it becomes another player's turn.
- Selected card is highlighted (raised above the hand row with an indigo border) while it is pending submission. The full card remains visible when raised.
- Selected card appears on the table and is removed from hand after the game state updates.

## Out of Scope (MVP)
- Client-side move validity enforcement
