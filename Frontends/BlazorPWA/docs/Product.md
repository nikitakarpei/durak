# Durak Frontend - Product Scope

## Why This Document Exists
- Define product scope and priorities from user/business perspective.
- Keep implementation details out of product decisions.

## Product Overview
- Game: Online Durak (Fool)
- Product goal: Mobile-first frontend for existing backend matches
- Client principle: Thin client (server is source of truth)

## Platforms
- Primary: Mobile screens (responsive-first)
- Secondary: Desktop (later, lower priority)

## MVP Scope
1. Player can open the app shell, including offline shell startup when assets are cached.
2. Player can join an existing game by ID.
3. Player can view current match state (opponents, table, hand, talon/trump).
4. Player can perform available actions (attack, beat, give up defence).
5. Player sees clear error feedback when connection or move submission fails.

## Milestone
1. Playable match screen for an existing game.

## Non-Goals (Current)
- Creating a new game from frontend.
- Offline gameplay.
- Bot gameplay.
- Desktop-first optimization.
- Complex client-side game rule validation.

## Sources of Truth
- Use cases: `docs/use-cases/`
- Detailed UI contracts: `docs/ui-specs/`
