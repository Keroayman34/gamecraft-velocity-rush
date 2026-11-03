# AI Agent Workflow

## Per-task workflow
1. Receive a narrowly scoped issue.
2. Read the project instructions and relevant feature documents.
3. Inspect the approved visual references if the task is visual.
4. Identify unknowns and dependencies.
5. Propose an implementation plan.
6. Wait for clarification/approval when a product or architecture decision is required.
7. Implement the agreed scope.
8. Run relevant tests and checks.
9. Compare visual output against references.
10. Report changes, tests, deviations, and open decisions.

## Prompt template
```text
Project: Velocity Rush

Task:
[Describe one specific task.]

Read first:
- README.md
- docs/AI_AGENT_GUIDELINES.md
- docs/PROJECT_CHARTER.md
- [Relevant GDD/TDD sections]
- [Relevant design specification and references]

Constraints:
- Do not change approved decisions.
- Do not invent missing requirements.
- Do not add dependencies without justification.
- Follow the acceptance criteria.

Acceptance criteria:
- [Criterion 1]
- [Criterion 2]

Before implementation:
Summarize the plan and list unresolved questions.

After implementation:
Report changed files, tests, deviations, limitations, and required approvals.
```
