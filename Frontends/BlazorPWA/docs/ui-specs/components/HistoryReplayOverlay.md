# Component Contract: HistoryReplayOverlay

- Used By: `Pages/Game.razor`
- Status: `Current UI`

## Why This Contract Is Needed
- Gives the player clear visual feedback that the game history is being loaded before the match becomes interactive.

## Public UI Contract
1. Inputs:
   - `IsVisible` (`bool`) — when `true`, renders the overlay; when `false`, renders nothing.
2. Outputs: none.
3. Required accessibility semantics:
   - Overlay has `role="status"` and `aria-label="Loading game history"` when visible.

## Required States
1. Visible: full-match overlay with centred circular progress spinner.
2. Hidden: nothing rendered.

## Test Hooks
- `data-testid='history-replay-overlay'` — present only when `IsVisible` is `true`.

## Out of Scope
- Progress percentage or event count display.
