# AI Agent Guidelines — Velocity Rush

**Status:** Mandatory project rules  
**Audience:** Any coding, design, or research agent working in this repository.  
**Companion:** `ai/MASTER_AGENT_PROMPT.md` (role definition), `ai/WORKFLOW.md` (per-task steps), `ai/prompts/README.md` (prompt library).

## 1. Source of truth
The repository is the source of truth. Do not rely on chat history when a current project document exists.

Before work:
1. Read `README.md`.
2. Read this file.
3. Read the relevant sections of `docs/PROJECT_CHARTER.md`.
4. Read the relevant GDD/TDD sections when they exist.
5. Read `docs/PLAYGAMA_CONSTRAINTS.md` for platform-facing work.
6. Read the relevant design specification and inspect its approved references.

## 2. Do not invent approvals
- Never silently change an approved product or visual decision.
- Never treat a draft, moodboard, generated image, or placeholder as an approved production asset.
- Never invent vehicle specifications, gameplay rules, costs, platform capabilities, performance budgets, or UI designs.
- If required information is missing, list the unknowns and ask for a decision or propose clearly labelled options.
- Check `docs/DECISIONS.md` (decision status) and `docs/OPEN_QUESTIONS.md` (unresolved items) before adding new content. Handle ambiguity by documenting it as an open question — never by guessing.

## 3. Work in small tasks
- Implement only the scope of the assigned issue.
- Do not refactor unrelated systems without approval.
- Identify dependencies before editing shared systems.
- Prefer small, reviewable changes.
- Do not create architecture patterns or frameworks without a documented need.

## 3a. Product scope discipline (Commercial V1.0)
- **Commercial V1.0 is the intended product target** (`DECISIONS.md` D-028): 10 playable vehicles (D-010), at least 3 distinct championships (D-029), all 5 environment themes, high-quality realistic-inspired 3D presentation (D-006).
- A **development prototype** and the **vertical slice** are development stages (D-030) — never the product definition, never presented as production content.
- Do not reduce the product scope without owner approval; do not silently convert the product plan into prototype-scale work.
- Do not silently downgrade the visual quality target; performance tradeoffs require profiling evidence and documented decisions.
- Do not create placeholder content and present it as production-ready; report missing specifications before implementation instead.
- Never silently change an existing decision: use the `docs/DECISIONS.md` change procedure with a dated entry.

## 4. Design fidelity
- Inspect the feature's approved visual references before implementing UI or visual work.
- Follow `design/ART_DIRECTION.md` and feature-specific specifications.
- Report any deviation from a reference.
- Do not claim visual completion just because the scene compiles or runs.
- If an exact reference cannot be implemented, explain why and propose alternatives before changing it.

### 4a. Design-driven implementation workflow (mandatory for any screen)
Before implementing any UI/visual feature:
1. Read the relevant design documentation.
2. Inspect available visual references.
3. Identify whether each reference is `APPROVED`, `DRAFT`, `REJECTED`, or `ARCHIVED` (`design/references/README.md`).
4. Follow the specified layout, visual hierarchy, colors, typography, spacing, and interactions.
5. Identify any missing design decisions.
6. Request clarification when essential specifications are missing.
7. Report any unavoidable deviations.
Never invent an arbitrary UI design.

## 5. Engineering quality
- Follow the approved TDD and coding conventions once established.
- Keep gameplay logic testable and avoid unnecessary coupling.
- Do not add third-party packages without explaining purpose, license, size, and platform impact.
- Do not commit secrets, credentials, tokens, or private keys.
- Do not introduce external analytics or services contrary to platform requirements.
- Add or update tests for changed behavior where practical.

## 6. Asset handling
For every imported asset, record:
- Source / creator.
- License and commercial-use permission.
- Intended use.
- File format and relevant import settings.
- Optimization status.
- Reference or specification it satisfies.

Do not put concept-reference images into Unity runtime folders unless they are intentionally used as in-game assets.

## 6a. File modification rules
- Modify only files within the assigned task's scope; leave unrelated files untouched.
- Never delete existing documentation; instead, correct, extend, or reorganize it — with the original intent preserved. *(Exception: deletions may occur only under an explicit owner-approved consolidation instruction, after every piece of unique content is verified as migrated and cross-references are updated.)*
- Never overwrite reference images or replace content with shortened summaries (extension, not substitution).
- Do not create new files without a justified, unique responsibility (`docs/PROJECT_STRUCTURE.md`).
- When changing any decision, follow `docs/DECISIONS.md` change procedure; never overwrite a decision silently.
- Update `CHANGELOG.md` for meaningful project changes.

## 6a-1. Documentation baseline & no-new-Markdown policy
The project maintains a small, stable set of authoritative documents. A task must normally **update these files** instead of creating another planning document.

**Authoritative baseline (updated in place; consulted before any work):**
| File | Authoritative for |
|---|---|
| `docs/GDD.md` | Product vision, game design, gameplay loop, vehicles/championships/events/environments, screens/UX, progression & rewards |
| `docs/TDD.md` | Complete technical architecture (Part I baseline + Part II detailed design), vehicle physics, input, camera, scene lifecycle, data/runtime state, save, audio/logic boundaries, WebGL performance & build, testing/validation |
| `docs/DECISIONS.md` | Approved decisions, provisional decisions, decision history & rationale (dated change procedure only) |
| `docs/OPEN_QUESTIONS.md` | Unresolved questions, evidence required for resolution, owner-approval requirements |
| `production/ROADMAP.md` | All 14 phases, tasks/statuses, dependencies & gates, milestone register M0–M12 with acceptance criteria, completed vs remaining work |
| `docs/TRACEABILITY.md` | Requirement → design → task → test mapping |

**Supporting set (retained in place; never duplicated):** `README.md`, `docs/PROJECT_STRUCTURE.md`, `docs/AI_AGENT_GUIDELINES.md` (this file), `docs/PLAYGAMA_CONSTRAINTS.md`, `CHANGELOG.md`.

**File-creation rule:** **No new Markdown file may be created for an individual phase, task, feature, audit, design, or review** unless (a) the project owner explicitly requests it, or (b) the content has a clear independent operational purpose (e.g., a deliverable spec file inside an approved `design/` package per the design-folder rules). A per-task planning document is never created.

**Default locations (route content into the baseline instead of new files):**
- Game design questions → `docs/GDD.md`
- Technical architecture / implementation design → `docs/TDD.md`
- Decisions & their history → `docs/DECISIONS.md`
- Unresolved questions → `docs/OPEN_QUESTIONS.md`
- Execution progress, planning, status, milestones → `production/ROADMAP.md`
- Requirement coverage & traceability → `docs/TRACEABILITY.md`
- Operational project instructions → this file (`docs/AI_AGENT_GUIDELINES.md`)
- Platform constraints → `docs/PLAYGAMA_CONSTRAINTS.md`

Ask the owner before deviating from these locations.

## 6a-2. Implementation-first workflow (mandatory per task)
1. Read the relevant GDD/TDD sections.
2. Check approved decisions (`DECISIONS.md`) and open questions (`OPEN_QUESTIONS.md`) that govern the work.
3. Identify the implementation task and its acceptance criteria (status in `production/ROADMAP.md`; authorized scope from the task assignment).
4. Implement within the authorized scope only.
5. Run the relevant tests and validation.
6. Review code and Unity behavior before reporting.
7. Update the roadmap and authoritative documents **only when the work genuinely requires it** (status changes, decision changes, new open questions, coverage changes) — following the change procedures of those files.
8. Report completed work and remaining blockers per the required task report (§8).

Do not create a planning document for every task; do not restate the task outline in a new file.

## 6b. Git safety rules
- Preserve the repository, branch, commit history, and remote configuration.
- Do not commit or push unless the task explicitly instructs it; never force-push or rewrite history.
- Do not merge, publish, or approve your own work.
- Never expose credentials, tokens, or private information; never commit `.env` files or secrets.

## 6c. Security
- No secrets or keys in code, docs, logs, or commits.
- No external services or analytics beyond what `docs/PLAYGAMA_CONSTRAINTS.md` and `docs/TDD.md` §19 permit.
- No unlicensed assets, fonts, or models.

## 7. Platform and performance
- Check `docs/PLAYGAMA_CONSTRAINTS.md` for Playgama work.
- Profile before claiming performance is acceptable.
- Do not invent polygon, texture, draw-call, memory, or frame-rate budgets. Establish them through representative prototypes and measurements.

## 8. Required task report
At the end of each task, report:
1. What changed.
2. Files created or modified.
3. Tests and checks performed.
4. Known limitations.
5. Deviations from approved design.
6. New dependencies or assets and their licenses.
7. Decisions needed from the project owner.

## 9. Definition of Done
A task is complete only when:
1. Its acceptance criteria are met.
2. Documentation affected by the work is updated (`CHANGELOG.md`, `DECISIONS.md` if a decision changed, the relevant feature/design doc).
3. Checks/tests are reported with results.
4. The project owner approves the result.

Do not merge or publish without authorization.
