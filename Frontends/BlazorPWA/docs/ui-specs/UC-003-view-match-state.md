# UI Spec: UC-003 View Match State

- Covers Use Case: `docs/use-cases/UC-003-view-match-state.md`
- Scope: Current UI details for match-state presentation (can evolve over time).

## Deck Area
1. Show a compact talon HUD widget for trump and deck status.
2. The HUD uses a pill container with a light background, rounded corners, subtle border or shadow, and two sections separated by a faint divider.
3. Trump section shows a mini card face with rank and suit when known.
4. Deck section shows a small blue card-back icon and the remaining deck count.
5. The talon HUD sits near the top of the screen and does not overlap the top-center or upper-right opponent seats.

## Opponents Area
1. Show all configured opponent seats, including unoccupied seats.
2. Joined seats show a compact tile with identity placeholder and card-count indicator.
3. Unoccupied seats show a placeholder tile.
4. Current UI placeholders: friendly seat names such as `Player N` and unoccupied label such as `Not joined`.
5. Opponents are positioned in a U shape around the top half of the screen, leaving the table centered.
6. Seat positions (visual order): left-middle, upper-left, top-center, upper-right, right-middle.
7. The visual positions assigned depend on the number of opponent seats to achieve a symmetric, balanced layout:
   - 1 seat → top-center (pos-3)
   - 2 seats → upper-left, upper-right (pos-2, pos-4)
   - 3 seats → left-middle, top-center, right-middle (pos-1, pos-3, pos-5)
   - 4 seats → left-middle, upper-left, upper-right, right-middle (pos-1, pos-2, pos-4, pos-5)
   - 5 seats → all positions (pos-1, pos-2, pos-3, pos-4, pos-5)

## Table Area
1. Show all attack/defence pairs currently on table.
2. If defence card is missing, show an empty defence slot.

## Hand Area
1. Show player hand cards with rank and suit visible.
2. Current UI uses recognizable card styling (rounded white card look with suit color).
3. Current suit color rule: Hearts/Diamonds are red, Spades/Clubs are dark.
4. When playable or beatable hand cards are highlighted, non-playable hand cards are visually de-emphasized with a greyed-out look that remains opaque and keeps normal card shadows.
5. Hovered or selected hand cards can lift upward without being clipped by the hand container.

## Actions Area
1. Show action controls based on available moves in current state.
2. Hide action buttons when no moves are available.
