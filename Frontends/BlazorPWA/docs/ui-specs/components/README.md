# Component Contracts

Component contracts define stable UI module boundaries for implementation and tests.

## Why This Document Type Exists
- Enforces modular UI design.
- Reduces coupling between page orchestration and section rendering.
- Improves maintainability and AI-assisted code generation quality.

## Required Rules
1. Every user-visible section component must have a contract file in this folder.
2. Contract must define explicit inputs, outputs, required states, and test hooks.
3. Page-level files should orchestrate state only; section behavior/rendering belongs to components.
4. Internal DOM/styling implementation details are not part of the contract.

## Template
- `docs/ui-specs/components/_template.md`
