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
- When a turn highlights playable or beatable hand cards, the other hand cards are visually de-emphasized but remain opaque and keep normal card shadows.
- Hovered or selected hand cards can lift upward without being clipped by the hand container.
- The trump suit and remaining deck count are shown in a compact HUD widget that does not occupy table space.
- Opponent seats are distributed around the top half of the screen in fixed positions so the table stays centered.

## Out of Scope (MVP)
- Desktop-first layout optimization
