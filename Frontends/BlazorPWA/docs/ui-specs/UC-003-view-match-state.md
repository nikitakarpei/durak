# UI Spec: UC-003 View Match State

- Covers Use Case: `docs/use-cases/UC-003-view-match-state.md`
- Scope: Current UI details for match-state presentation (can evolve over time).

## Deck Area
1. Show trump card face-up when known.
2. When talon has cards, show a face-down deck card and visible talon count.
3. Current UI: deck card is visually rotated for stacked-deck effect.

## Opponents Area
1. Show all configured opponent seats, including unoccupied seats.
2. Joined seats show a compact tile with identity placeholder and card-count indicator.
3. Unoccupied seats show a placeholder tile.
4. Current UI placeholders: friendly seat names such as `Player N` and unoccupied label such as `Not joined`.

## Table Area
1. Show all attack/defence pairs currently on table.
2. If defence card is missing, show an empty defence slot.

## Hand Area
1. Show player hand cards with rank and suit visible.
2. Current UI uses recognizable card styling (rounded white card look with suit color).
3. Current suit color rule: Hearts/Diamonds are red, Spades/Clubs are dark.

## Actions Area
1. Show action controls based on available moves in current state.
2. Hide action buttons when no moves are available.
