# UC-004: Attacker Plays One of Their Hand Cards

- Priority: Must
- Actor: Player
- Action: As attacker, play one selected card from hand
- Related UI Spec: `docs/ui-specs/UC-004-play-attack-card.md`

## Why This Use Case Is Needed
- Player needs to perform an attack move to progress the round.

## Preconditions
- Player is connected to a game
- It is player's attack turn
- Player has at least one card in hand
- Player selected one hand card

## Trigger
- Player clicks the highlighted table area

## Main Flow
1. Player selects one card from hand.
2. Player clicks the highlighted table area.
3. App sends the move request.
4. Match state updates and reflects result.

## Alternative / Failure Flows
- If no hand card is selected, the table area is not highlighted and no move is sent.
- If request fails, app shows error state.

## Postconditions
- Attack intent is submitted.

## Observable Changes
- The table area is highlighted only when it is the player's attack turn and a hand card is selected.
- Player can select and unselect a hand card before submitting.
- Selected card appears on the table and is removed from hand after the game state updates.
- After the play is confirmed, the previous hand selection is cleared.

## Out of Scope (MVP)
- Client-side move validity enforcement
