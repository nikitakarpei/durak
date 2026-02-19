# UI Specs

UI specs define exact user-visible UI behavior so HTML/CSS can be generated consistently.

## Why This Document Type Exists
- Use cases define what must happen for the user.
- UI specs define how that behavior is presented on screen (states, copy, layout intent, responsive behavior, accessibility expectations).

## Rules
1. Keep content user-visible only.
2. Do not include framework internals or implementation mechanics.
3. Link each UI spec to a use case, and link use cases back to UI specs when details matter.

## Templates
- Screen-level UI spec: `docs/ui-specs/_template.md`
- Component contract spec: `docs/ui-specs/components/_template.md`

## Enforcement Checklist
- `docs/ui-specs/HTML-CSS-GENERATION-CHECKLIST.md`
