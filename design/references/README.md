# Reference Library — Velocity Rush

How visual references are stored, named, approved, and used. Compliance rules: `docs/AI_AGENT_GUIDELINES.md` §4a, `../ART_DIRECTION.md` §9–10.

## Storage
- One folder per reference board or feature batch, e.g. `art-direction-v0.1/`.
- Screen/UI references eventually live under the relevant screen folder in `../ui-ux/` or here with a clear link from the specification.
- Image source, generation date, tool, and creator must be recorded in the folder README.

## Naming
- Lowercase kebab-case with a version suffix: `<topic>-v<minor>.png` (e.g. `art-direction-board.png` inside `art-direction-v0.1/`).
- No `final`, `new`, or `final-final2` style names (`docs/PROJECT_STRUCTURE.md` §5).

## Reference statuses

| Status | Meaning | Agent behavior |
|---|---|---|
| `APPROVED` | Project owner has explicitly approved this reference as a production target | Implementable; follow it and report deviations |
| `DRAFT` | Captures direction; not yet named as production target | Read for guidance; do not implement as a production spec |
| `REJECTED` | Explicitly not to be used | Do not implement; keep for history |
| `ARCHIVED` | Superseded but historically useful | Record-keeping only |

Status is set by the project owner only — agents never promote a reference's status themselves.

## Usage rules
- A reference is a **visual target**, not a production-ready 3D model, texture, editable UI source, or validated technical specification.
- Text embedded in reference images is not authoritative copy; implement copy from written specifications.
- Never silently replace or reinterpret an approved reference; report the limitation and propose options instead.
- Every reference must connect to a written specification and acceptance criteria (Hybrid workflow — D-007).
