# Contributing to Velocity Rush

## Workflow
- Use feature branches for changes.
- Keep changes focused and reviewable.
- Link work to a GitHub Issue.
- Update documentation when behavior, architecture, or approved design changes.
- Do not merge without project-owner approval.

## Suggested branch names
- `feature/<short-name>`
- `bugfix/<short-name>`
- `docs/<short-name>`
- `chore/<short-name>`

## Commit message conventions
Use concise, imperative one-line summaries, optionally with a short body explaining context:

```text
docs: clarify Commercial V1.0 event types in GDD
feat: add vehicle controller input abstraction
fix: correct RTL layout on results screen
chore: update gitignore for Unity build artifacts
```

- Prefix the summary with the change type (`docs/feat/fix/chore/refactor` are sufficient).
- Do not rewrite or force-push shared history.
- Do not mix unrelated changes in one commit.

## Pull request checklist
- [ ] Scope matches the linked issue.
- [ ] Relevant documentation updated.
- [ ] Tests/checks run and results reported.
- [ ] No secrets or unlicensed assets added.
- [ ] Visual references followed where relevant.
- [ ] Performance impact considered.
- [ ] Deviations and limitations documented.

## Code quality (future Unity/C# work)
- Follow the coding conventions documented in `docs/TDD.md` once established there.
- Keep components small and focused; avoid unnecessary coupling between gameplay, UI, and platform code.
- No third-party packages, SDKs, or asset-store dependencies without documented purpose, license, size, and platform impact.
- Refer to code as implementation guidance in `docs/TDD.md`; do not finalize architecture without technical validation.

## Reference-first design implementation
- UI and visual features must be implemented against an approved reference and its written acceptance criteria (`design/ui-ux/`).
- Never invent layouts, colors, or interactions that contradict `design/ART_DIRECTION.md` or an approved reference.
- Report unavoidable deviations explicitly in the task report.

## Testing expectations
- Documentation tasks: verify internal links and requirement consistency (see `docs/TRACEABILITY.md`).
- Runtime code tasks: add or update tests where practical, report what was tested and on which target (editor/playmode/browser), and include profiling evidence for performance claims.

## Current phase
During documentation foundation, do not create Unity runtime code. Keep changes limited to project documentation, design references, and repository setup.
