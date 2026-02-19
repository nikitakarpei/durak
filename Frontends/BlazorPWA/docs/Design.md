# Durak Frontend - Design (MVP)

## Why This Document Exists
- Describe technical architecture and boundaries needed to implement current product scope.
- Keep it aligned with use cases and UI specs.

## Technical Goals
- Thin client: UI reacts to server events.
- Deterministic state projection for rendering.
- Mobile-first UI implementation.

## Non-Goals (Technical)
- Client-side enforcement of full Durak rules.
- Offline gameplay logic.
- Automatic reconnect strategy in MVP.

## Architecture Overview
- UI: Blazor WebAssembly pages/components
- Transport: LibRpc over HTTP
- Service layer: clients for PlayerIdentityService and GameService
- State: in-memory projection updated from game events
- Orchestration: connection/session service coordinating login/access/stream/join and actions

## Data and Control Flow
1. User initiates connect from join screen.
2. Client performs auth/access flow and starts event stream.
3. Event reducer applies old/new events into UI state.
4. UI renders from projected state only.
5. User actions are sent as commands; visible state changes when corresponding events are applied.

## UI/State Boundaries
- UI components:
  - render current projected state
  - emit user intent only
- Reducer:
  - pure state transitions from incoming events
- Service layer:
  - remote communication and connection status

## Error Strategy (MVP)
- Failures surface as user-visible error feedback.
- No automatic retry/recovery workflow in MVP.

## Configuration
- `appsettings.json` provides service endpoints and auth-related client settings.

## Alignment
- Product behavior contracts: `docs/use-cases/`
- Detailed UI contracts: `docs/ui-specs/`
