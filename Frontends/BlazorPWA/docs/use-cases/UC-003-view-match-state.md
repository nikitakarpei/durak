# UC-003: View Match State on Mobile-First Screen

- Priority: Must
- Actor: Player
- Action: Observe current match status and cards
- Related UI Spec: `docs/ui-specs/UC-003-view-match-state.md`

## Why This Use Case Is Needed
- Player needs a clear, continuously updated match view to make decisions during play.

## Preconditions
- Player is connected to a game

## Trigger
- Match screen is open and game state changes

## Main Flow
1. App shows deck information, including the trump card (when known) and remaining talon size.
2. App shows all opponent seats configured for the game, including seats not yet occupied.
3. App shows table cards.
4. App shows player hand and action controls.

## Alternative / Failure Flows
- If some data is not yet available, placeholders are shown.

## Postconditions
- Player can see the current match state.

## Observable Changes
- Table, hand, trump, and talon information update over time.
- The talon count decreases each time cards are dealt to a player.
- Any unoccupied opponent seat within the configured seat count is shown as an empty placeholder until a player joins it.
- Cards in the player's hand are displayed as recognizable playing cards with rank and suit information.
- Cards on the table are displayed as recognizable playing cards with rank and suit information, matching the same face layout used in the hand.
- Hand cards remain visually consistent even when some cards are playable or beatable.
- Hovered or selected hand cards can lift upward without being clipped by the hand container.
- The trump suit and remaining deck count are shown in a compact central HUD widget that does not occupy table space.
- The central HUD shows a role icon when the player is the attacker or defender and it is their turn, and shows no role icon otherwise.
- All opponent seats are displayed in a top zone above the table, arranged in up to two symmetric horizontal rows. The arrangement adjusts by seat count so the layout is always balanced and never lopsided.
- Defended table pairs show the defence card overlaid on top of the attack card at an angle, so the player can see both the beating card and the card that was beaten.
- When the player has no available moves, hand and table cards are shown as non-interactive.
- While historical game events are being replayed on (re)connect, a full-page loading indicator is shown and the match screen is not displayed until the state is fully loaded.

## Out of Scope (MVP)
- Desktop-first layout optimization
