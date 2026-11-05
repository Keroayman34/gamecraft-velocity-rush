# Changelog

All notable project changes are recorded here.
This changelog records documentation and process milestones; it does not pre-announce features that have not been implemented.

## [Unreleased]
### Commercial V1.0 scope transformation (owner-confirmed, 2026-11-03)
- Product scope model changed: previous limited MVP framing (3 vehicles, 1 environment kit, one championship) is retired as the product definition (DECISIONS D-015/D-016/D-017/D-019 RETIRED with dated change history) and replaced by **Commercial V1.0** (new D-028): 10 playable vehicles, at least three distinct championships (new D-029), all five environment themes, high-quality realistic-inspired 3D presentation.
- Visual quality target upgraded: from "Balanced realism" to high-quality realistic-inspired 3D presentation (D-006) — Cinematic Motorsport preserved; quality balanced with WebGL limits via profiling gates, never silently downgraded.
- Development scope model added (D-030): Development Prototype → Vertical Slice → Commercial V1.0; prototypes never presented as the product.
- Event planning basis: the 8 long-term challenge types govern Commercial V1.0 planning with a per-championship mix; per-championship event distribution open (D-031, replacement for retired D-016).
- GDD rebuilt for Commercial V1.0 (35 sections); TDD expanded with material/shader strategy, lighting strategy, VFX strategy, audio system, profiling gates, browser compatibility, and content-scaling considerations.
- OPEN_QUESTIONS rebuilt: commercial content-planning questions added (OQ-14…OQ-21, OQ-A08…OQ-A11), MVP-era wording updated (OQ-13 → final Commercial V1.0 acceptance criteria; OQ-08 sign-off now covers the Commercial V1.0 scope).
- ROADMAP restructured into 14 commercial-target phases (vertical slice added as the quality gate before mass content production); MILESTONES rebuilt as M0–M12 with measurable acceptance criteria and a historical mapping note.
- PLAYGAMA_CONSTRAINTS: performance risk register added (Commercial V1.0 scope vs platform reality) with an empty profiling evidence log — no untested values presented as achieved performance.
- README/PROJECT_CHARTER/PROJECT_STRUCTURE/TRACEABILITY updated to the commercial product vision; game explicitly marked as not yet implemented.
- AI agent rules updated: scope discipline (§3a), quality-downgrade prohibition, placeholder-content prohibition, prototype/vertical-slice distinction.

### Documentation foundation (v0.2 documentation audit)
- Completed documentation audit: expanded GDD and TDD skeleton sections with provisionally specified content (no architecture or balance values finalized).
- Added docs/TRACEABILITY.md: requirement-to-document traceability matrix.
- Consolidated all known Playgama platform constraints into docs/PLAYGAMA_CONSTRAINTS.md with verification and submission checklists.
- Normalized the decision register: added MVP scope, monetization, challenge types, environment kit, championship tiers, and MVP vehicle-count decisions with explicit statuses.
- Restructured OPEN_QUESTIONS with impact, resolution timing, and blocking status.
- Expanded design folder READMEs (brand, ui-ux, vehicles, environments, gameplay, references, art-direction-v0.1) and linked the art direction board from ART_DIRECTION.md.
- Expanded AI agent rules: MASTER_AGENT_PROMPT, WORKFLOW (12-step), prompts library conventions.
- Expanded production/ROADMAP.md to the 12 proposed phases and production/MILESTONES.md with measurable acceptance criteria.

## [0.1] — 2026-11-03
- Established initial project documentation foundation.
- Recorded initial championship, simcade, global, and landscape direction.
- Recorded cinematic motorsport art direction with balanced realism.
- Recorded initial target of 10 vehicles and six upgrade categories.
- Added AI agent rules, repository structure guide, decision register, and open questions.
- Added GDD and TDD skeletons; detailed content and architecture remain pending.
