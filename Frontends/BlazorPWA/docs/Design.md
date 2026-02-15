# MVP Design Doc - Durak Frontend (Blazor WASM)

## Goals
- Thin-client: UI updates driven by server events only.
- Mobile-first game screen for a playable match.
- Offline start (app loads without internet).

## Non-Goals (MVP)
- Client-side rule validation.
- Local/offline gameplay.
- Desktop-optimized layout.
- Reconnect strategy beyond basic error.

## Architecture Overview
- UI: Blazor WebAssembly pages/components.
- Transport: LibRpc HTTP client.
- Services: API clients for PlayerIdentityService and GameService.
- State: In-memory projection from event stream (client-side model only).
- Config: `appsettings.json` (service URLs, `custom_id`).
- Orchestration: `GameConnectionService` (session flow + event stream).

## Separation of Concerns
- UI components render state and dispatch user intent only.
- `GameConnectionService` runs the session flow and forwards events to the reducer.
- Reducer applies events to pure state (no RPC, no UI).
- API clients are thin wrappers over LibRpc calls.

## GameConnectionService (MVP)
Responsibilities:
- Execute login/access/stream/join sequence.
- Maintain connection status: `Idle`, `Connecting`, `Connected`, `Error`.
- Start `GetGameEvents` stream and forward events to the reducer.
- Expose commands to UI: `Connect(gameId)`, `Disconnect()`, `PlayAttack(cardId)`,
  `BeatAttack(attackerCardId, defenderCardId)`, `GiveUpDefence()`.
Notes:
- No retry strategy in MVP.
- Errors surface as generic toast and transition to `Error`.

## Screen Flow (MVP)
1. App loads (offline-capable shell).
2. "Enter Game ID" screen.
3. On submit:
   - `LoginWithCustomId`
   - `GetGameAccessToken`
   - Start `GetGameEvents` stream
   - `JoinLobby`
4. Match screen renders and updates on incoming events.

## Data Flow
- API clients produce:
  - unary calls: login, access token, join, actions
  - streaming call: game events
- Event stream:
  - Reconstruct state from old events
  - Apply new events incrementally
  - UI updates only when events arrive

## State Model (Client Projection)
Minimal in-memory model tailored to UI:
- Game:
  - Players: id, seat, card_count, is_current_turn (derived from events)
  - Talon: count, trump card
  - Table: attack/defense pairs + timestamps
  - Your hand: cards list
  - Round status: attacker_id, defender_id
  - Possible moves (from `YourTurnStarted`)
- View state:
  - Loading, connected, error
  - Active game_id

## UI Components (MVP)
- `EnterGameId` screen
  - Input field + connect button
  - Inline status (loading/error)
- `MatchScreen`
  - Opponents bar (up to 5 enemies)
  - Table area (attack/defense pairs)
  - Your hand (selectable cards)
  - Action buttons: Play Attack, Beat, Give Up Defence
  - Status: turn indicators, round info, trump card
- `ErrorToast`

## Event Handling Notes
Events used:
- `GameCreated`, `PlayerJoined`, `CardsDealtToPlayer`, `RoundStarted`
- `YourTurnStarted`, `OpponentTurnStarted`
- `AttackCardPlayed`, `AttackCardBeaten`, `PlayerTookCardsFromTable`
- `CardsOnTableDiscarded`, `DefenceGivenUp`, `RoundFinished`, `FoolDeclared`
- `CommonTrumpCardChosen`, `CardsInTalonShuffled`

## Action Handling (MVP)
- `PlayAttackCard` -> uses selected card id
- `BeatAttackCard` -> uses selected attacker card + selected defender card
- `GiveUpDefence`
- Buttons enabled regardless of possible moves (no validation)

## Configuration
`appsettings.json` keys:
- `Services:GameServiceUrl`
- `Services:PlayerIdentityServiceUrl`
- `Auth:CustomId`

## Error Handling
- Any RPC error -> generic toast + state set to error
- Event stream closed -> show error

## Task Breakdown
1. Configuration and typed settings
2. LibRpc HTTP transport client wrapper
3. PlayerIdentityService client (LoginWithCustomId)
4. GameService client (access token, join, events, actions)
5. Event projection model + reducer
6. "Enter Game ID" screen
7. Match screen layout (mobile-first)
8. Action wiring (buttons -> commands)
9. Error toast + basic status UI

## Open Questions
## Decisions
- Card rendering: text-only (rank + suit)
- Player count: dynamic from events (max 6 total)
- Seat mapping: local player bottom; others clockwise by seat order
- Talon/trump: show as soon as event arrives
