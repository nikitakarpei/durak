# Use Case Traceability

This matrix maps MVP use cases to tests in `Frontends/BlazorPWA.Tests`.

| Use Case | Test Coverage | Status | Notes |
|---|---|---|---|
| UC-001 Start Offline-Capable App Shell | No automated test in bUnit | Partial | Requires browser/PWA cache behavior; cover via manual or E2E test in a real browser.
| UC-002 Enter Existing Game ID and Connect | `UC002_ConnectTests` | Covered | Covers join screen, connect disabled while connecting, empty game id block, success screen switch, error display, and trimmed game/custom id pass-through.
| UC-003 View Match State on Mobile-First Screen | `UC003_ViewMatchStateTests` | Covered | Covers header, all opponent seats (including "Not joined" placeholders), table pairs, and hand rendering.
| UC-004 Attacker Plays One of Their Hand Cards | `UC004_PlayAttackCardTests` | Covered | Covers playable card highlighting, selection, missing selection guard, missing token guard, command dispatch, and reflected state update (hand -> table).
| UC-005 Beat Attack Card with Explicit Target Selection | `UC005_BeatAttackCardTests` | Covered | Covers explicit target selection, missing selection guard, missing token guard, command dispatch, and reflected defended pair update.
| UC-006 Give Up Defence | `UC006_GiveUpDefenceTests` | Partial | Covers command dispatch and missing token guard; reflected table/turn/cards update still needs a higher-level integration/e2e check.
| UC-007 Show Errors for Connection and Move Failures | `UC007_ErrorHandlingTests` | Covered | Covers error message/toast on join and match screens, plus join screen remains visible in error state without game id.

## Execution
- Latest run: `dotnet test ../BlazorPWA.Tests/BlazorPWA.Tests.csproj`
- Result: Passed 27 / 27
