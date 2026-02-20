# UI Spec: UC-003 View Match State

- Covers Use Case: `docs/use-cases/UC-003-view-match-state.md`
- Scope: Current UI details for match-state presentation (can evolve over time).

## Central HUD
1. Show a compact central HUD widget for trump and deck status.
2. The HUD uses a pill container with a light background, rounded corners, and a subtle border without a drop shadow. Thin dividers separate the three sections.
3. Trump section shows a mini outlined card with centered rank and suit when known.
4. Deck section shows a mini outlined card-back icon with diagonal hatching and the remaining deck count.
5. A role indicator slot sits between the trump and deck sections.
6. When the player is defender and it is their turn, the role slot shows a prominent shield emoji centered in the slot.
7. When the player is attacker and it is their turn, the role slot shows a prominent crossed swords emoji centered in the slot.
8. When the player is not the active player, the role slot is empty.
9. When the player is not active, the HUD shows only the trump and deck sections.
10. The trump and deck sections always share the same width, while the role slot may be narrower.
11. The central HUD sits directly below the opponent top zone and does not overlap any opponent seat.
12. The central HUD width stays stable across roles and must not grow or shrink based on the presence or absence of action buttons — its height must be determined solely by its own content.

## Opponents Area
1. Show all configured opponent seats, including unoccupied seats.
2. Joined seats show a compact tile with identity placeholder and card-count indicator.
3. Unoccupied seats show a placeholder tile.
4. Current UI placeholders: friendly seat names such as `Player N` and unoccupied label such as `Not joined`.
5. All opponent seats are displayed in a top zone above the central HUD and table, arranged in up to two horizontal rows (tiers). The layout is symmetric and adjusts to seat count:
   - 1 seat  → tier 1: center
   - 2 seats → tier 1: left + right
   - 3 seats → tier 1: left + center + right
   - 4 seats → tier 1: left + right / tier 2: outer-left + outer-right
   - 5 seats → tier 1: left + center + right / tier 2: outer-left + outer-right
6. All game sections stack in a fixed order: opponents → central HUD → table (1fr) → hand → actions. Section positions must not shift based on the content or visibility of sibling sections.

## Table Area
1. Show all attack/defence pairs currently on table.
2. If defence card is missing, show an empty defence slot.
3. Table cards use the same face layout as hand cards: rank and suit are stacked with compact spacing.
4. When the player has no available moves, table cards are non-interactive.

## Hand Area
1. Show player hand cards with rank and suit visible.
2. Current UI uses recognizable card styling (rounded white card look with suit color).
3. Current suit color rule: Hearts/Diamonds are red, Spades/Clubs are dark.
4. When playable or beatable hand cards are highlighted, non-playable hand cards keep the same visual styling.
5. Hovered or selected hand cards can lift upward without being clipped by the hand container.
6. When the player has no available moves, hand cards are non-interactive.

## Actions Area
1. Show action controls based on available moves in current state.
2. Hide action buttons when no moves are available.

## History Replay Loading
1. While old events are being applied on (re)connect, hide the match screen entirely and show the same circular loading progress indicator used during Blazor startup.
2. No intermediate game state is shown during replay — the match screen appears only once all historical events have been fully applied.
