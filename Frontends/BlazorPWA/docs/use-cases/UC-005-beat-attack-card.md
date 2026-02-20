# UC-005: Beat Attack Card with Explicit Target Selection

- Priority: Must
- Actor: Player
- Action: Defend by choosing a specific attack card and a defender card
- Related UI Spec: `docs/ui-specs/UC-005-beat-attack-card.md`

## Why This Use Case Is Needed
- Player needs to defend a specific attack card to continue the round.

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
- Player can explicitly select a target attack card and a defender card before submitting.
- After selecting a target attack card on the table, hand cards that cannot beat it are faded to emphasize valid defenders.
- After selecting a hand card, table attack cards that cannot be beaten by it are faded to emphasize valid targets, even when none are beatable.
- Clicking a non-beatable table attack card does not select it and triggers a brief shake animation to indicate it is invalid.
- Clicking a non-beatable hand card does not select it and triggers a brief shake animation to indicate it is invalid.
- Defender card appears overlaid on the chosen attack card after the game state updates: the defender card is placed on top, offset and rotated, while the attack card remains visible beneath it.

## Out of Scope (MVP)
- Client-side legality checks for selected card pair
