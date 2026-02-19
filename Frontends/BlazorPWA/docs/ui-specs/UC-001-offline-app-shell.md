# UI Spec: UC-001 Offline App Shell

- Covers Use Case: `docs/use-cases/UC-001-offline-app-shell.md`
- Status: `Current UI`

## Why This Spec Is Needed
- Defines expected user-visible shell behavior when offline so startup behavior is clear and testable via manual/E2E checks.

## Screen States
1. When cached shell assets are available offline, app shell opens to join entry context.
2. When required shell assets are unavailable offline, app cannot fully load.

## Interaction Rules
1. Offline startup does not automatically start a live match session.
2. Match data remains unavailable until a successful connection is established.

## Layout and Responsive Rules
1. Offline shell preserves the same mobile-first entry layout as online join screen.

## Copy Rules
- Offline-specific wording is not fixed at this stage; behavior is the primary contract.

## Accessibility Expectations
1. Entry screen remains navigable when opened offline from cache.

## Out of Scope
- Offline gameplay.
- Bot gameplay.
