# HTML/CSS Generation Checklist

Use this checklist for every user-visible UI change.

1. Tokens and Visual Consistency:
Use shared design tokens for spacing, typography, colors, radius, and shadows.
2. State Coverage:
Define behavior for default, loading, empty, error, disabled, and active states (as applicable).
3. Component Contract:
Document public UI contract (inputs, outputs, required states) for reusable components.
4. Responsive Behavior:
Specify how layout behaves on mobile-first breakpoints.
5. Accessibility Baseline:
Define role/label/focus/keyboard expectations for new or changed UI.
6. Stable Test Hooks:
Add stable `data-testid` hooks for key interactive/observable elements.
7. Copy Contract Clarity:
Mark copy as either required contract or changeable current wording.
8. Motion and Feedback:
Specify user-visible transitions/feedback (if any) and keep timing consistent.
9. Verification Mapping:
Ensure test coverage (unit/component/E2E/manual note) exists for changed states and interactions.
10. Definition of Done:
Update use case + UI spec + tests in the same task.
