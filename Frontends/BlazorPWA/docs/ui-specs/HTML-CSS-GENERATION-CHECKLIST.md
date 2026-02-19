# HTML/CSS Generation Checklist

Use this checklist for every user-visible UI change.

## 1) Tokens and Visual Consistency
- [ ] Use shared design tokens for spacing, typography, colors, radius, and shadows.
- Why: prevents inconsistent UI and reduces rework when style changes.

## 2) State Coverage
- [ ] Define behavior for default, loading, empty, error, disabled, and active states (as applicable).
- Why: most regressions happen outside the happy path.

## 3) Component Contract
- [ ] Document public UI contract (inputs, outputs, required states) for reusable components.
- Why: enables safe reuse and prevents hidden coupling.

## 4) Responsive Behavior
- [ ] Specify how layout behaves on mobile-first breakpoints.
- Why: avoids surprises on real devices.

## 5) Accessibility Baseline
- [ ] Define role/label/focus/keyboard expectations for new or changed UI.
- Why: ensures UI is usable and testable beyond pointer interactions.

## 6) Stable Test Hooks
- [ ] Add stable `data-testid` hooks for key interactive/observable elements.
- Why: keeps tests robust when copy or styling changes.

## 7) Copy Contract Clarity
- [ ] Mark copy as either required contract or changeable current wording.
- Why: avoids brittle tests and unclear UX expectations.

## 8) Motion and Feedback
- [ ] Specify user-visible transitions/feedback (if any) and keep timing consistent.
- Why: preserves perceived quality and interaction clarity.

## 9) Verification Mapping
- [ ] Ensure test coverage (unit/component/E2E/manual note) exists for changed states and interactions.
- Why: turns specs into enforceable quality gates.

## 10) Definition of Done
- [ ] Update use case + UI spec + tests in the same task.
- Why: keeps behavior, implementation, and verification aligned.
