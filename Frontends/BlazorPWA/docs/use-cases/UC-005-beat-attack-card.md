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
- Player selects a valid target attack card and defender card pair.

## Main Flow
1. Player selects an open attack card on the table.
2. Player selects one defender card from hand.
3. App submits defence immediately after the valid pair is selected.
4. Match state updates and reflects result.

## Alternative / Failure Flows
- If target or defender selection is missing, no move is sent.
- If request fails, app shows error state.

## Postconditions
- Defence intent is submitted for the chosen attack card.

## Observable Changes
- Player can explicitly select a target attack card and a defender card.
- Once both selected form a valid pair, the defence move is sent automatically.
- After selecting a target attack card on the table, hand cards that cannot beat it are faded to emphasize valid defenders.
- After selecting a hand card, table attack cards that cannot be beaten by it are faded to emphasize valid targets, even when none are beatable.
- Clicking a non-beatable table attack card does not select it and triggers a brief shake animation to indicate it is invalid.
- Clicking a non-beatable hand card does not select it and triggers a brief shake animation to indicate it is invalid.
- Defender card appears overlaid on the chosen attack card after the game state updates: the defender card is placed on top, offset and rotated, while the attack card remains visible beneath it.

## Out of Scope (MVP)
- Client-side legality checks for selected card pair
