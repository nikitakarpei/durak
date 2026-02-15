# Durak Frontend - Project Notes

## Overview
- Game: Online Durak (Fool)
- Goal: Frontend for an existing game backend
- Client model: Thin client, close-to-zero business logic
- Platforms:
  - Primary: Mobile screens (responsive-first)
  - Secondary: Desktop (future, not a priority now)
- Offline mode: Required (PWA/offline support)
  - Milestone 1: App should start without internet (no local gameplay yet)

## Milestones
1. Playable game screen for a match (core gameplay UI)

## Non-Goals (for now)
- Desktop-first layout
- Complex client-side game rules or validation

## Current Decisions
- Framework: Blazor WebAssembly (standalone)
- Project root: /home/nikita-karpei/homespace/durak/Frontends/BlazorPWA
- Game variant: Basic Durak only (future variants later)
- Realtime: LibRpc (transport-agnostic; transport TBD)
- Transport (MVP): HTTP
- API source: `Protos/game_service/api` (GetGameEventsCommands)
- State flow: Reconstruct state from domain events; update with new events
- Visual direction: Minimalistic MVP
- Auth (MVP): `LoginWithCustomId` via PlayerIdentityService
  - Token: `player_id_token` passed to GameService via middleware
  - `custom_id` from `appsettings.json` (temporary for MVP)
  - Game access: call `GetGameAccessTokenCommand` with `game_id`
  - Use `access_token` for all other game commands
- Game events: single stream per game (no snapshot)
- UI scope: show other players + card counts, table cards, and your hand
- Moves: show and allow all actions; no client-side validation for now
- RPC methods (GameService):
  - `CreateGame`, `JoinLobby`, `PlayAttackCard`, `BeatAttackCard`, `GiveUpDefence`
  - `GetGameAccessToken`, `GetGameEvents` (stream)
- RPC methods (PlayerIdentityService):
  - `LoginWithAndroidDeviceId`, `LoginWithIosDeviceId`, `LoginWithCustomId`, `GetIdentityTokenPublicKey`
- Service URLs: provided via `appsettings.json`
- Flow requirement: call `JoinLobby` for every player
- Call sequence:
  - `LoginWithCustomId` -> `GetGameAccessToken` -> `GetGameEvents` (start stream)
  - After stream starts: `JoinLobby`
  - All actions sent; UI updates only on incoming events (thin-client)
- Errors: generic error toast
- Stream drop: show error (no retry yet)
- Identity display: `player_id` is sufficient for MVP
- Layout: support up to 5 enemies (6 players total)
- Card rendering: text-only (rank + suit)
- Seat mapping: local player bottom; others clockwise by seat order

## Risks / Constraints
- Thin client requirement implies server is source of truth
- Offline mode needs clear strategy for caching assets and handling connectivity transitions
