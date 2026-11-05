# Milestones — Velocity Rush

Measurable milestones for the **Commercial V1.0** target (D-028). A milestone is complete **only with recorded evidence**; boxes stay unchecked until then. Map to `ROADMAP.md` phases. Development stages (Development Prototype / Vertical Slice / Commercial V1.0) follow D-030 — the prototype never substitutes for the product.

## M0 — Documentation & Commercial Product Scope Approved
- [x] Documentation set reviewed and approved as source of truth. *(Evidence: owner baseline approval, 2026-11-05; pre-implementation audit completed with findings resolved — see Task 0.1, 2026-11-05.)*
- [x] Art direction document and reference board in place and honored. *(Evidence: `design/ART_DIRECTION.md` + `design/references/art-direction-v0.1/`; wording aligned to D-006 on 2026-11-05.)*
- [x] Formal Commercial V1.0 scope + documentation sign-off (OQ-08) granted by the project owner. *(Evidence: owner approval communicated 2026-11-05; recorded in `OPEN_QUESTIONS.md` Resolved questions and `DECISIONS.md` change history v0.3.1.)*
- [ ] Confirm repository name and visibility. *(Administrative confirmation not yet obtained from the owner.)*
- [ ] Confirm Git branching approach. *(Approach documented in `CONTRIBUTING.md`; explicit owner confirmation pending.)*

**Acceptance criteria:** documentation cross-checked per `docs/TRACEABILITY.md`, no unresolved critical gaps, no contradictions; the owner explicitly approves (a) this documentation set and (b) the Commercial V1.0 scope (D-028: 10 vehicles, ≥3 championships, 5 environments, high-quality realistic-inspired presentation) as the release target. Final release acceptance criteria remain open (OQ-13). *Status: sign-off recorded 2026-11-05 (OQ-08 resolved) — two administrative confirmations (repository name/visibility, branching approach) remain open before M0 closes fully.*

## M1 — Development Environment Ready
- [ ] Confirm compatible Unity Editor version: suitable stable **LTS**, verified for WebGL Build Support (OQ-T01).
- [ ] Install Unity Hub and required modules (including WebGL Build Support).
- [ ] Install/configure C# tooling.
- [ ] Verify a clean Unity project builds for the web target.
- [ ] Document the actually installed Unity version and modules.

**Acceptance criteria:** documented Unity version + module list on the Ubuntu workstation; clean project builds successfully for WebGL; findings recorded (e.g., `docs/TDD.md` §2 update). Evidence: build log/screenshot. *No Unity version is silently approved in `DECISIONS.md`.*

## M2 — Unity Foundation & Architecture Ready
- [ ] Confirm render pipeline with performance evidence (OQ-T02).
- [ ] Define scene and content organization (OQ-T06).
- [ ] Complete TDD and architecture decisions.

**Acceptance criteria:** `docs/TDD.md` §25 deferred list resolved for the decided items with evidence; ADRs recorded per `TDD.md` §25; Unity project committed with correct `.gitignore` behavior.

## M3 — First Vehicle Controllable (development prototype)
- [ ] Basic vehicle movement on a test track.
- [ ] Steering, acceleration, braking on keyboard.
- [ ] Camera follow (temporary default — OQ-01).
- [ ] Baseline performance profiling.

**Acceptance criteria:** vehicle completes a closed test lap without loss of control; input responsive on keyboard; profiling evidence recorded as the first reference numbers (OQ-T08 input). *Prototype content size is a development allowance only (D-030).*

## M4 — Driving Prototype Validated (first critical quality gate)
- [ ] Structured playtests of handling, acceleration/braking, drifting feel, camera feel, driving feedback.
- [ ] Documented results and tuning decisions.

**Acceptance criteria:** playtest evidence supports that driving feels enjoyable and understandable (D-023); verdict and decisions recorded in `docs/DECISIONS.md`; go/no-go before further production is made **by the project owner**.

## M5 — Vertical Slice Validated (production quality gate)
- [ ] One representative section at production quality: vehicle, environment, driving, lighting, UI, audio, gameplay.
- [ ] Performance evidence on target browsers/devices for the high-quality presentation (D-006).
- [ ] Quality acceptance criteria drafted and applied (OQ-A08/OQ-A09) where defined.

**Acceptance criteria:** the slice demonstrates the intended commercial quality bar in review; profiling results recorded in `PLAYGAMA_CONSTRAINTS.md` performance evidence notes; owner confirms the quality bar is representative before mass content production (ROADMAP Phases 8–10) begins.

## M6 — Vehicle Production Complete (10 vehicles)
- [ ] 10 production vehicles built (classes per D-014: Starter 2 / Sport 3 / Drift 2 / Supercar 3).
- [ ] Per-vehicle specs approved (OQ-14/OQ-06): stats, class fit, upgrade mapping.
- [ ] Quality criteria (OQ-A09) met for each vehicle; provenance/licensing recorded.

**Acceptance criteria:** all ten vehicles pass vehicle-package checklists (`design/vehicles/README.md`), including quality review against references and WebGL profiling evidence (OQ-T08); roster statistics approved; no unlicensed identity.

## M7 — Environment Production Complete (five distinct themes)
- [ ] Five environments produced: Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena.
- [ ] Each environment has distinct identity and driving experience (distinctness review OQ-20).
- [ ] Track readability/gameplay visibility verified per environment; performance evidence recorded.

**Acceptance criteria:** per-environment packages complete (`design/environments/README.md` items); visually distinct in review (not one kit with cosmetic swaps); track counts/layouts per approved design (OQ-21); performance evidence per environment.

## M8 — Championship & Event Content Complete
- [ ] ≥3 championships with approved names/identities (OQ-15) and approved structure (OQ-16).
- [ ] Approved total event count (OQ-18) implemented across environments (OQ-19).
- [ ] Event progression and unlocking per design (OQ-17/OQ-05).

**Acceptance criteria:** all designed events playable end-to-end; championship mapping documented; no invented event counts beyond the approved plan.

## M9 — Gameplay, Progression, Garage, Upgrades & UI Integration Complete
- [ ] Full gameplay systems integrated: scoring/results, rewards, upgrades, garage/roster, save/progression (OQ-09 rules resolved), settings.
- [ ] Complete commercial UI implemented against approved specifications (OQ-A03), bilingual readiness verified.

**Acceptance criteria:** complete loop (Select Championship → Select Event → Prepare Vehicle → Drive → Results → Rewards → Upgrade/Unlock → Progress → Replay) works end-to-end in a build; per-screen RTL/LTR acceptance criteria pass; no D-020 excluded feature present.

## M10 — Audio, Localization & Monetization Integrated
- [ ] Audio implemented incl. vehicle sound differentiation (OQ-A11), with correct pause/resume behavior.
- [ ] Arabic (RTL) + English (LTR) localization complete across all screens.
- [ ] Ad integration per D-021/D-027 rules (no ads during driving; rewarded grants gated).

**Acceptance criteria:** localization audit passes (all strings, RTL mirroring); audio plays/pauses correctly with ads and tab state; monetization respects approved rules (config still OQ-P03).

## M11 — Playgama Integration Complete
- [ ] Bridge SDK installed at the planned phase (ROADMAP Phase 13) and integrated.
- [ ] Rewarded + limited interstitial ads with correct pause/resume and reward gating.
- [ ] Bridge Analytics events flowing; no external analytics.
- [ ] Save via the Bridge-backed strategy (OQ-T05/OQ-09 resolved).

**Acceptance criteria:** `PLAYGAMA_CONSTRAINTS.md` §6 integration-applicable submission checklist passes; performance risk register items relevant to integration (`PLAYGAMA_CONSTRAINTS.md` §5) have no open blockers; rewards granted only after confirmed rewarded completion; no ad-break corruption of game/audio/input state.

## M12 — QA, Optimization & Release Readiness
- [ ] Full platform QA across supported browsers/devices (mobile + desktop) — OQ-P01 depth resolved.
- [ ] Performance/memory/archive optimization meets re-verified platform limits (size ≤ re-verified limit, startup memory < re-verified limit, no startup warnings, acceptable loading).
- [ ] Responsive layouts without overflow/clipping; popup closeability; Escape conflicts absent.
- [ ] Submission package correctness (archive root `index.html`, Latin filenames).
- [ ] Final Commercial V1.0 acceptance criteria (OQ-13) verified.

**Acceptance criteria:** every testing + submission checklist item in `PLAYGAMA_CONSTRAINTS.md` §§4/6 checked with recorded evidence; performance risk register (§5) items closed or explicitly accepted; official Playgama documentation date/version used is recorded in the release report; final release criteria (OQ-13) verified.

## Historical mapping (preserved)
Former milestone IDs from the MVP-era plan were re-mapped at the Commercial V1.0 transformation (2026-11-03): former M5 "First Playable Event" is absorbed into M3/M8; former M6 "MVP Feature Completion" is superseded by M9; former M7 "Playgama Integration" is now M11; former M8 "QA & Release Readiness" is now M12. The former "MVP feature completion" concept no longer applies — the product target is Commercial V1.0.

## Evidence rule
Each completed milestone links its evidence (playtest notes, build logs, profiling output, QA reports) and is confirmed by the project owner. Never mark a milestone complete on agent self-assessment; never present prototype content as Commercial V1.0 progress.
