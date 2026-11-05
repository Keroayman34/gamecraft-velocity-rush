# Prompt Library — `ai/prompts/`

This folder stores reusable, task-specific prompts. Prompts are **entry points for agents**, not alternatives to the documentation: each prompt points to authoritative project documents rather than duplicating specifications.

## Organization rules
- **One prompt per specific task type.** Do not create overlapping prompts for the same purpose; check existing prompts before adding a new one.
- **Naming convention:** lowercase kebab-case, `<domain>-<task>.md` — e.g. `unity-environment-setup.md`.
- **Naming categories:**
  - `design-*` — design production tasks (UI screens, asset references)
  - `unity-*` — Unity project and editor tasks
  - `runtime-*` — gameplay/runtime implementation tasks
  - `playgama-*` — platform integration tasks
  - `review-*` — audit/testing/review tasks
- **Inside each prompt:** task statement, "Read first" list (following `../WORKFLOW.md`'s template), constraints, acceptance criteria, and reporting expectations.
- **Avoiding conflicts:** prompts must never contradict `docs/AI_AGENT_GUIDELINES.md`, `docs/DECISIONS.md`, or `docs/PLAYGAMA_CONSTRAINTS.md`. If a prompt conflicts with a doc, the doc wins and the prompt is corrected. Phases not yet reached (e.g. integration) must state that explicitly rather than imply the SDK is ready.
- Update `CHANGELOG.md` when a reusable prompt is added or materially changed.

## Suggested future prompts
- `unity-environment-setup.md` — confirm Unity version, install Hub/Editor modules (Roadmap Phase 2)
- `vehicle-controller-prototype.md` — driving prototype plan and acceptance criteria (Phase 5)
- `garage-ui-implementation.md` — reference-first Garage screen implementation (Phase 8)
- `playgama-bridge-integration.md` — SDK integration and constraints verification (Phase 13)
- `visual-regression-review.md` — visual comparison against approved references

None of these prompts exist yet; create them only when the task becomes current.

Phase mapping (Commercial V1.0 roadmap): `unity-environment-setup` = Phase 2; `vehicle-controller-prototype` = Phase 5; `garage-ui-implementation` = Phase 11; `playgama-bridge-integration` = Phase 13.
