# UI/UX Design Documentation — Velocity Rush

This folder holds the per-screen UI design packages. The project follows **design-driven development** (see `docs/AI_AGENT_GUIDELINES.md` §4a): no screen is implemented without a reference and a written specification approved for it. The moodboard at `../references/art-direction-v0.1/` is conceptual guidance only — not a production UI specification.

The product target is **Commercial V1.0** (D-028): the UI must support the complete game — multiple championships, the full 10-vehicle roster, garage/upgrade flows, progression and unlock presentation, results, and rewards — not a reduced prototype navigation. AI agents must not invent production UI: approved references + written specifications only.

## Planned screens (Commercial V1.0)

| Screen | Purpose | Package status |
|---|---|---|
| Main Menu | Entry navigation, language settings hook | Planned |
| Championship (selection) | Multi-championship selection for ≥3 championships with identity + progress state | Planned |
| Event Selection | Event browsing/progression within a championship | Planned |
| Garage | 10-vehicle roster browsing, stats, class differentiation, unlock state | Planned |
| Upgrades | Vehicle upgrade consumption and visualization (6 categories) | Planned |
| Pre-Event | Event briefing, vehicle/assist/controls confirmation | Planned |
| In-Game HUD | Driving state, timers, score, feedback per challenge type | Planned |
| Pause | Pausable state, retry/settings/quit, safe state for ads | Planned |
| Results | Score, stars, rewards, progression deltas, retry/next actions | Planned |
| Settings | Language, audio, controls preferences | Planned |
| Rewards / Unlock presentation | Reward flow and unlock moments across events/championships | Planned |

All are planned screens; none has a completed design package yet. The exact final screen set (and any navigation niceties for multi-championship flow) is part of the full-product UI specification work (OQ-A03).

## Required contents per screen package
Every production screen must receive **all** of the following before implementation:
1. Reference image(s) with status (APPROVED/DRAFT/REJECTED/ARCHIVED) in `../references/` or this folder.
2. Screen purpose statement.
3. Layout specification (zones, hierarchy, alignment).
4. Component specification (buttons, lists, panels, states).
5. Interaction behavior (navigation, focus, input).
6. Responsive behavior (landscape breakpoints, safe areas, no clipping).
7. Arabic RTL behavior (mirroring rules).
8. English LTR behavior.
9. Acceptance criteria (testable).
10. Approval status (project owner sign-off).

## Workflow rules
- One folder per screen (`main-menu/`, `garage/`, ...) created only when work begins on that screen.
- The color palette and typography rules come from `../ART_DIRECTION.md` and `../brand/README.md`; screen specs reference them and never redefine tokens.
- UI quality targets the professional commercial bar (`../ART_DIRECTION.md` §2); quality is never silently reduced on any screen.
- Any unavoidable deviation must be reported in the task report (`docs/AI_AGENT_GUIDELINES.md` §8).
