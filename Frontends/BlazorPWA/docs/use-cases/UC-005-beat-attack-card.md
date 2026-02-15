# UC-005: Beat Attack Card with Explicit Target Selection

- Priority: Must
- Actor: Player
- Action: Defend by choosing a specific attack card and a defender card

## Preconditions
- Player is connected to a game
- It is player's defence turn
- Player selected a target attack card on the table
- Player selected a defender card from hand

## Trigger
- Player presses Beat Attack

## Main Flow
1. Player selects an open attack card on the table.
2. Player selects one defender card from hand.
3. Player submits defence.
4. App sends the move request.
5. Match state updates and reflects result.

## Alternative / Failure Flows
- If target or defender selection is missing, the Beat Attack button is grayed out and the move is not sent.
- If request fails, app shows error state.

## Postconditions
- Defence intent is submitted for the chosen attack card.

## Observable Changes
- The Beat Attack button is visible only when it is the player's defence turn. No action buttons are shown otherwise.
- Cards the player is allowed to beat with are visually marked (green tint).
- Selected hand card is raised above the hand row with an indigo border (full card remains visible); selected attack card on the table is highlighted with an indigo border.
- Defender card appears paired with the chosen attack card after the game state updates.

## Out of Scope (MVP)
- Client-side legality checks for selected card pair
