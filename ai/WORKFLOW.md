# AI Agent Workflow

## Standard per-task workflow
1. **Understand the task.** Restate the goal and acceptance criteria; ask before assuming when the scope is unclear.
2. **Inspect the repository.** Current structure, recent changes, and anything related to the touched areas (`docs/PROJECT_STRUCTURE.md`).
3. **Read the relevant documents.** Per `ai/MASTER_AGENT_PROMPT.md` reading list — charter, guidelines, decisions, open questions, and the relevant GDD/TDD sections.
4. **Inspect visual references.** For visual work: find references, check their status (APPROVED / DRAFT / REJECTED / ARCHIVED) in `design/references/README.md`.
5. **Identify dependencies.** Affected docs, decisions, systems, assets, and platform constraints (`docs/PLAYGAMA_CONSTRAINTS.md` for platform-facing work).
6. **Present an implementation plan.** Files to change, approach, impact, limits.
7. **Wait for approval when required.** Merges/releases, and any product, visual, balance, or architecture decision affecting approvals — stop and ask; ambiguity is recorded in `docs/OPEN_QUESTIONS.md`, not guessed away.
8. **Implement in small steps.** Task-scoped, reviewable changes; follow acceptance criteria.
9. **Test.** Documentation: link/consistency review. Runtime: playmode/browser testing; profiling for performance claims.
10. **Review changes.** Diff against the plan; verify no unrelated modifications and no silent decision changes.
11. **Update documentation.** `CHANGELOG.md`; `docs/DECISIONS.md` if any decision changed; feature/design docs; `docs/TRACEABILITY.md` when requirement coverage changes.
12. **Report results.** Changed files, tests and results, deviations, limitations, new assets/licenses, and decisions needed from the project owner (`docs/AI_AGENT_GUIDELINES.md` §8).

## Prompt template
```text
Project: Velocity Rush

Task:
[Describe one specific task.]

Read first:
- README.md
- docs/AI_AGENT_GUIDELINES.md
- docs/PROJECT_CHARTER.md
- docs/DECISIONS.md
- docs/OPEN_QUESTIONS.md
- [Relevant GDD/TDD sections]
- [Relevant design specification and references]

Constraints:
- Do not change approved decisions; do not reduce the Commercial V1.0 product scope without owner approval.
- Do not invent missing requirements or UI.
- Do not present prototype/vertical-slice content as production-ready Commercial V1.0 content.
- Do not add dependencies without justification.
- Do not commit/push/merge unless explicitly instructed.
- Follow the acceptance criteria.

Acceptance criteria:
- [Criterion 1]
- [Criterion 2]

Before implementation:
Summarize the plan and list unresolved questions.

After implementation:
Report changed files, tests, deviations, limitations, and required approvals.
```
