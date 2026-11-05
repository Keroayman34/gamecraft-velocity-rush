# Production Roadmap — Velocity Rush

**Status:** Proposed roadmap for the **Commercial V1.0** product target (DECISIONS D-028). Phases marked *In progress* are real; all later phases are planned, not completed or started. No calendar dates or durations are assigned — that remains policy until scope, dependencies, and available working time are understood.

The development **scope model** (D-030): the *Development Prototype* validates foundations, the *Vertical Slice* validates the production quality bar, and **Commercial V1.0** is the release target. The prototype is never treated as the product.

## Proposed development phases (in order)

| # | Phase | Outcome | Status |
|---|---|---|---|
| 1 | Documentation & product scope approval | Commercial V1.0 documentation set validated; formal sign-off of this documentation and the Commercial V1.0 scope (OQ-08) by the project owner | **Complete — sign-off recorded 2026-11-05** (OQ-08 resolved; two administrative M0 confirmations — repository name/visibility, branching approach — remain open). **Phase 2 unblocked** |
| 2 | Environment setup | Unity Hub + Editor (stable LTS) + C# tooling on Ubuntu; WebGL Build Support verified; clean web-target build; installed version documented | Not started — **unblocked** by the Phase 1 sign-off (2026-11-05); version compatibility still gated on OQ-T01 (the task itself) |
| 3 | Technical architecture planning | TDD decisions validated against a real project: Unity version, render pipeline, voice; asset pipeline & quality acceptance criteria (OQ-A08/A05); performance risk approach | Not started |
| 4 | Unity project foundation | Unity project created in-repo; tracked settings; conventions; initial project tooling | Not started |
| 5 | Core driving prototype | First controllable vehicle with driving feedback on a test vehicle + test environment (temporary prototype content only, D-030) | Not started |
| 6 | Driving-feel validation | Driving feel proven enjoyable through playtests (D-023) — gates large-scale production | Not started |
| 7 | High-quality vertical slice | Production-quality representative section proving the intended commercial quality bar (vehicle, environment, driving, lighting, UI, audio, gameplay, performance) before mass asset production | Not started |
| 8 | Vehicle production — 10 vehicles | Full commercial roster (2/3/2/3 per D-014) produced against approved references and quality criteria | Not started |
| 9 | Environment production — five themes | All five environments (Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena) produced as distinct, high-quality packages | Not started |
| 10 | Championship & event production | ≥3 championships designed (names/identities/structure — OQ-15/16), events designed & built, track/config per environment (OQ-18/19/21) | Not started |
| 11 | Gameplay, progression, garage, upgrades & UI integration | Full systems integration: progression, unlocks, scoring, save, garage/upgrades, complete commercial UI | Not started |
| 12 | Audio, localization, monetization integration | Audio quality & vehicle sound differentiation (OQ-A11); Arabic/English (RTL/LTR) localization; ad rules per D-021/D-027 | Not started |
| 13 | Playgama Bridge integration | Bridge SDK integration, ads/analytics boundaries, save strategy per actual SDK (OQ-T05), platform constraint re-verification | Not started |
| 14 | QA, optimization & release preparation | Full QA across browsers/devices incl. mobile; performance/memory/archive optimization to platform limits; submission package | Not started |

## Dependency notes
- **Phase 6 (driving-feel validation) gates Phases 7–14**: no large-scale asset/UI/progression/monetization investment before the driving experience is validated (D-023).
- **Phase 7 (vertical slice) gates Phases 8–10 mass content production**: both the quality bar and WebGL performance headroom for the high-quality presentation (D-006) must be proven with profiling evidence before the 10-vehicle and five-environment production workload is scaled.
- Championship/event design (Phases 10–11) depends on the open content planning questions (OQ-15…OQ-21) being resolved with the owner.
- Phase 13 happens after architecture and content systems exist; the Bridge Unity SDK is not installed before that point (`docs/PLAYGAMA_CONSTRAINTS.md` #1).
- Historical v0.x roadmap terms: "Design Packages" work now lives inside Phases 7–10; "AI/GitHub Workflow" runs continuously across all phases; the former "Vertical Slice" concept is formalized as Phase 7.
