# AI Agent Guidelines — Velocity Rush

**Status:** Mandatory project rules  
**Audience:** Any coding, design, or research agent working in this repository.

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
- Never invent vehicle specifications, gameplay rules, costs, platform capabilities, or performance budgets.
- If required information is missing, list the unknowns and ask for a decision or propose clearly labelled options.

## 3. Work in small tasks
- Implement only the scope of the assigned issue.
- Do not refactor unrelated systems without approval.
- Identify dependencies before editing shared systems.
- Prefer small, reviewable changes.
- Do not create architecture patterns or frameworks without a documented need.

## 4. Design fidelity
- Inspect the feature's approved visual references before implementing UI or visual work.
- Follow `design/ART_DIRECTION.md` and feature-specific specifications.
- Report any deviation from a reference.
- Do not claim visual completion just because the scene compiles or runs.
- If an exact reference cannot be implemented, explain why and propose alternatives before changing it.

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

## 9. Completion rule
A task is complete only when its acceptance criteria are met, checks are reported, and the project owner approves the result. Do not merge or publish without authorization.
