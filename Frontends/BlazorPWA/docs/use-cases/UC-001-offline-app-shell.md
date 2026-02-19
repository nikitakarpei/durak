# UC-001: Start Offline-Capable App Shell

- Priority: Must
- Actor: Player
- Action: Open the app even when internet is unavailable
- Related UI Spec: `docs/ui-specs/UC-001-offline-app-shell.md`

## Why This Use Case Is Needed
- Player must be able to start the app shell in unreliable network conditions and reach the entry screen.

## Preconditions
- The app was previously opened so required shell assets are cached

## Trigger
- Player opens the app without internet

## Main Flow
1. The app shell loads from cache.
2. The join screen is shown.

## Alternative / Failure Flows
- If required assets are not cached, the app cannot start offline.

## Postconditions
- The player can access the app shell while offline.
- No live game session is started while offline.

## Observable Changes
- Join screen is visible without network.
- Match data is not visible until connection succeeds.

## Out of Scope (MVP)
- Offline gameplay
- Bot gameplay
