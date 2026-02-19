# Component Contract: ErrorToast

- Used By: `Pages/Game.razor`, `docs/use-cases/UC-007-handle-connection-and-move-failures.md`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps match-context error feedback behavior explicit and testable through a stable component contract.

## Public UI Contract
1. Inputs:
   - Derived from connection state service (`Status`, `ErrorMessage`).
2. Outputs:
   - None (presentational feedback component).
3. Required accessibility semantics:
   - Error feedback remains visible/readable when active.

## Required States
1. Hidden when there is no active error state/message.
2. Visible when connection state is `Error` and message is present.

## Test Hooks
- `data-testid='error-toast'`

## Out of Scope
- Error recovery actions and retry flow logic.
