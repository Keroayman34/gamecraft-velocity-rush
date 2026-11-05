# Gameplay Design Documentation — Velocity Rush

Structure of gameplay design material. The authoritative game design specification is [`../../docs/GDD.md`](../../docs/GDD.md); decisions live in `../../docs/DECISIONS.md`; unresolved items in `../../docs/OPEN_QUESTIONS.md`. Product target: **Commercial V1.0** (D-028) — multi-championship, full-roster, five-environment. This folder holds gameplay/presentation material tied to how systems are *presented* — camera format, HUD composition, feedback, control layouts.

## Documentation structure (planned per topic)

| Topic | Status | Primary reference |
|---|---|---|
| Core gameplay loop | APPROVED-initial direction | `GDD.md` §6 |
| Championship system (≥3 distinct championships) | APPROVED count / OPEN detail | `GDD.md` §13–§15 |
| Challenge types (8 long-term types) | PROVISIONAL planning basis | `GDD.md` §16 |
| Event distribution (per championship/environment) | OPEN | `GDD.md` §17 |
| Scoring | OPEN formulas/thresholds | `GDD.md` §20 |
| Rewards and unlock economy | PROVISIONAL baseline | `GDD.md` §21 |
| Difficulty | PROVISIONAL | `GDD.md` §15 |
| Driving skill systems | APPROVED direction / OPEN detail | `GDD.md` §7 |
| Playtesting and balancing | Required process | `GDD.md` §33 |

Visual-material folders to be created here as work begins: feedback presentation, HUD composition per challenge type, camera-format comparisons, control-layout specifications (desktop + touch).

## Commercial V1.0 gameplay structure — what is settled vs open
- **Settled (approved):** ≥3 distinct championships (D-029); the 8-type event planning basis (D-031); the full gameplay loop; skill-over-upgrades rule; Simcade direction.
- **Open:** championship names/identities (OQ-15), events per championship (OQ-16), total event count (OQ-18), event-per-environment distribution (OQ-19), scoring formulas and star thresholds (OQ-03/OQ-04), unlock/progression rules (OQ-17), per-event session targets (OQ-10).
- Candidate tier names Rookie / Street / Pro / Elite / World (D-018) are vocabulary, not finalized championship definitions.

## Playtesting and balancing
- Playtest results are recorded with observation notes and linked from the affected topic.
- Any balance decision that changes gameplay must be captured via `docs/DECISIONS.md` change history — never silently tuned.
- **First** playtest focus is always the driving feel itself (D-023), before balancing progressions or economies.
- The vertical slice (D-030) validates gameplay-and-quality together before large-scale content production.
