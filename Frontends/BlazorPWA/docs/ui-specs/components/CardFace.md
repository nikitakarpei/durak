# Component Contract: CardFace

- Used By: `Components/Game/HandSection.razor`, `Components/Game/TableSection.razor`
- Status: `Current UI`

## Why This Contract Is Needed
- Keeps card-face rendering consistent across hand and table contexts while sections retain selection logic.

## Public UI Contract
1. Inputs:
   - `Card` (`Card`) for rank and suit.
   - `IsInteractive` (`bool`) to render a button for actionable cards or a div for static cards.
   - `StateClasses` (`string?`) for selected/beatable/defence visual states passed from the parent section.
   - `VisualState` (`string?`) for optional `data-visual-state` rendering.
   - `TestId` (`string`) to render a stable `data-testid`.
2. Outputs:
   - `OnClick` when an interactive card is selected.
3. Required accessibility semantics:
   - Interactive cards are actionable controls.

## Required States
1. Interactive card renders as a button with the card face content.
2. Non-interactive card renders as a div with the card face content.
3. Rank and suit are stacked with compact spacing.
4. Hearts/Diamonds render in red; Spades/Clubs render in dark.

## Test Hooks
- `data-testid` uses the `TestId` input value.

## Out of Scope
- Selection rules and move legality.
