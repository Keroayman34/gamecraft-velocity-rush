# Open Questions — Velocity Rush

This list prevents assumptions from being mistaken for decisions. Companion file: `DECISIONS.md`.
Product target: **Commercial V1.0** (DECISIONS D-028). Development stages are defined in D-030 (Development Prototype → Vertical Slice → Commercial V1.0) and are never the product scope.

Legend: **Blocks now** = work cannot responsibly proceed before resolution; **Blocks later** = must be resolved before the listed roadmap phase; **Monitor** = resolution can happen opportunistically.

## Product and game design

| ID | Question | Why it matters | When it must be resolved | Blocking? |
|---|---|---|---|---|
| OQ-01 | Camera perspective and behavior (chase cam, cockpit, hood, multiple cameras?) | Defines driving feel, HUD layout, and how the player reads the track | During driving-feel prototyping (Roadmap Phase 5–6); a first default can be picked for the prototype | Blocks later — driving prototype needs at least a temporary default |
| OQ-02 | Exact Simcade handling model (grip curves, assist level, drifting model, per-vehicle differentiation) | The core of the game; determines enjoyment | Established and validated during the driving prototype, closing at the driving-feel validation milestone (Roadmap Phases 5–6) | Blocks later — does not block Unity environment installation or initial environment preparation |
| OQ-03 | Final scoring formulas per challenge type (all 8 long-term types, priority per milestone) | Star ratings, rewards, and progression depend on them; one universal formula is explicitly rejected | Design per challenge type before its content production; validated in playtests | Blocks later |
| OQ-04 | Star thresholds (1/2/3 stars per event) | Defines success communication and replay goals | With OQ-03 through playtesting | Blocks later |
| OQ-05 | Championship progression details (how candidate tier names Rookie/Street/Pro/Elite/World map to championships, tier gating requirements, difficulty ramp, D-018/D-029) | Structures the multi-championship commercial content plan | During championship design (before Roadmap Phase 10 content production) | Blocks later |
| OQ-06 | Vehicle statistics, balance, and per-class handling differentiation for the full 10-vehicle roster | Drives garage UI, upgrades, event design, and difficulty tuning | Before vehicle asset production and tuning at scale (Roadmap Phase 8) | Blocks later |
| OQ-07 | Upgrade formulas and balance values | Must preserve skill-based gameplay; wrongly tuned upgrades break the core promise | After OQ-06, validated in playtests | Blocks later |
| OQ-08 | Formal Commercial V1.0 scope and documentation sign-off | Makes the owner's approval explicit: approving (a) this documentation set as the source of truth and (b) the Commercial V1.0 product scope D-028 as the release target | Roadmap Phase 1 validation step — before production commitments beyond documentation | **RESOLVED — APPROVED 2026-11-05.** The owner explicitly approved the Commercial V1.0 documentation baseline (this documentation set as source of truth and the D-028 release target). Unlocks Roadmap Phase 2; recorded in the milestone register M0 (`production/ROADMAP.md`) and in the `DECISIONS.md` change history. This resolution does NOT change the status of any other open or provisional item. Distinct from D-028 (the approved-initial scope direction) and OQ-13 (final release acceptance criteria, still open) |
| OQ-09 | Save and progression rules (what persists, per-profile: best results, stars, unlocks, upgrades, championship state, settings, language) | Player trust and progression integrity | Before gameplay systems phase (Roadmap Phase 11) | Blocks later. **Owner review note (2026-11-05):** the owner provisionally agreed with the layered save architecture direction (SaveModel + ISaveStore + SaveService — DECISIONS D-036); the persistence rules themselves are not decided. **Remains OPEN — NOT resolved** |
| OQ-10 | Session duration targets per event | Web players expect efficient sessions; shapes event and championship design | During event design (Roadmap Phase 10) | Blocks later |
| OQ-13 | Final Commercial V1.0 acceptance criteria (formerly "final MVP acceptance criteria" — updated with the scope transformation) | Defines when the commercial release is done | Before QA/release preparation (Roadmap Phase 14) | Blocks later |

## Commercial V1.0 content planning

| ID | Question | Why it matters | When it must be resolved | Blocking? |
|---|---|---|---|---|
| OQ-14 | Exact identities and specifications of the 10 vehicles (fictional names, silhouettes, per-vehicle stats, class fit) | The core content plan of the commercial roster; naming and design must be fiction-based/licensed (D-014 planning distribution) | Before Roadmap Phase 8 (10-vehicle production) | Blocks later |
| OQ-15 | Championship names and identities (minimum three distinct championships per D-029) | Championships are the primary player-facing structure; identities drive menu, narrative framing, and environment pairing | During championship design (before Roadmap Phase 10) | Blocks later |
| OQ-16 | Event distribution across the minimum three championships (which event types, how many, per-championship identity) | Defines the meaningful progression structure the owner approved | With OQ-15/OQ-18 during championship design | Blocks later |
| OQ-17 | Unlock and progression requirements for Commercial V1.0 | Drives motivation, retention design, and reward economy | With OQ-09/OQ-16 before Roadmap Phase 11 | Blocks later |
| OQ-18 | Exact number of events in Commercial V1.0 | Defines content production workload (vehicle/environment/event packages) | With OQ-15/OQ-16 before Roadmap Phase 10 | Blocks later |
| OQ-19 | Event distribution across the five environments | Balances environment use, variety, and workload | With OQ-16/OQ-21 during championship/event design | Blocks later |
| OQ-20 | Detailed design of the five environments (visual identity, lighting, landmarks, atmosphere, kit reuse rules that still keep the five visibly distinct) | Each environment must be a distinct experience, not cosmetics on one kit | Per environment, before each environment's production (Roadmap Phase 9) | Blocks later |
| OQ-21 | Track count and layouts per environment | Governs track-building workload and event design freedom | With OQ-18/OQ-19 | Blocks later |

## Technical

| ID | Question | Why it matters | When it must be resolved | Blocking? |
|---|---|---|---|---|
| OQ-T01 | Exact Unity Editor version and WebGL module compatibility | Determines the whole toolchain. Resolved by the environment setup task verifying WebGL Build Support compatibility and documenting the actually installed version — a version is never approved silently in `DECISIONS.md` | Roadmap Phase 2 (environment setup) | **PARTIALLY RESOLVED — environment verification complete (2026-11-05).** The owner explicitly approved Unity `6000.6.8f1` (Unity 6.6 **Supported Update release, not LTS**; build `237b6593792b`, Linux x86_64) on 2026-11-05. Verified: Unity Hub 3.22.5 detects the editor; batch-mode startup exited cleanly (code 0, `Editor.log` confirmed the version, no fatal/licensing/graphics errors; UnityPersonal entitlement granted); Web Build Support module present at `~/Unity/Hub/Editor/6000.6.8f1/Editor/Data/PlaybackEngines/WebGLSupport/`. Remaining **open / pending planned integration testing**: a real WebGL build from the actual project, exact compatibility with Playgama Bridge v2.4.0, and runtime/deployment validation on Playgama Web. Observe also: NVIDIA driver installed but kernel module inactive; no standalone .NET SDK or C# VS Code extension — non-blocking. Details in milestone register M1 (`production/ROADMAP.md`) and `docs/TDD.md` §2 |
| OQ-T02 | Render pipeline choice (Built-in vs URP) for high-quality yet browser-performant presentation | Affects art and performance work directly; the stronger quality bar (D-006) must be proven under WebGL limits | After prototyping browser performance (Roadmap Phase 3) | Blocks Phase 3+. **Owner review note (2026-11-05):** owner provisionally agreed to a controlled Built-in vs URP comparison before any selection (DECISIONS D-032); URP is NOT installed now. **Remains OPEN — NOT resolved:** a final choice requires same-scene A/B profiling evidence and an explicit owner decision |
| OQ-T03 | Vehicle physics implementation strategy (built-in physics vs package-based) | Core prototype risk | Technical architecture planning (Roadmap Phase 3–5) | Blocks later. **Owner review note (2026-11-05):** owner provisionally agreed with a WheelCollider-first prototype direction and documented hybrid fallback (DECISIONS D-033). **Remains OPEN — NOT resolved:** final strategy requires driving-feel validation (D-023 gate) and WebGL profiling, then an owner decision |
| OQ-T04 | Input abstraction for keyboard, mouse, and touch | Web delivery requires both; browser-reserved keys (see `PLAYGAMA_CONSTRAINTS.md`) | With the driving prototype | Blocks later. **Owner review note (2026-11-05):** owner provisionally agreed with Unity Input System as the preferred direction behind abstraction interfaces (DECISIONS D-034); **package installation is NOT authorized by that agreement**. **Remains OPEN — NOT resolved:** final choice requires prototype validation plus an explicit owner approval of the manifest change |
| OQ-T05 | Save strategy supported by Playgama Bridge (what the current SDK offers) | Progression persistence for the full Commercial V1.0 save set (OQ-09) depends on it | During Bridge integration planning (Roadmap Phase 13) | Blocks later. **Owner review note (2026-11-05):** owner provisionally agreed with the layered save design (SaveModel + ISaveStore + SaveService — DECISIONS D-036); Bridge integration unchanged and strictly deferred to Roadmap Phase 13. **Remains OPEN — NOT resolved:** to be decided against the actual Bridge SDK during Phase-13 planning |
| OQ-T06 | Scene organization and runtime architecture (content scaling to 5 environments / 10 vehicles) | Prevents coupled, untestable systems; large content set pressures loading/memory | Technical architecture planning, after the prototype | Blocks later — must not be invented now. **Owner review note (2026-11-05):** additive scene loading (bootstrap + additive content) recorded as a candidate direction (DECISIONS D-037). **Remains OPEN — NOT resolved:** final organization requires practical loading/memory and vertical-slice validation |
| OQ-T07 | Automated build and test strategy for WebGL | Reduces regressions in a remote/agent workflow across a bigger content set | Roadmap Phase 3 | Blocks later. **Owner review note (2026-11-05):** owner provisionally agreed with local batch build automation first, CI considered later when justified (DECISIONS D-038); the CI deferral is recorded as deferral, not rejection. **Remains OPEN — NOT resolved:** requires first empty-project batch build evidence and a later explicit CI decision |
| OQ-T08 | Performance budgets for a high-quality presentation (fps, memory, archive headroom, loading time per environment/vehicle) | The upgraded quality target (D-006) must be proven reachable under Playgama limits; budgets are profiling-gated, not assumed | After first representative profiling (Roadmap Phase 5–7) | Blocks later. **Owner review note (2026-11-05):** owner provisionally confirmed measurement-first planning — numeric budgets are never invented and require measured evidence from the Empty Build, Prototype, and Vertical Slice stages (D-006 review note, DECISIONS v0.3.3). **Remains OPEN — NOT resolved:** no budget numbers exist yet |
| OQ-T09 | Competitor AI: which events use opponents versus solo time/skill trials in Commercial V1.0 | Affects game feel and systems scope | During championship/event design (Roadmap Phase 10) | Blocks later |

## Art and UX

| ID | Question | Why it matters | When it must be resolved | Blocking? |
|---|---|---|---|---|
| OQ-A01 | Final font families, licenses, Arabic glyph coverage, file size | Unlicensed or too-large fonts block production work | Before UI production (Roadmap Phase 11) | Blocks later |
| OQ-A02 | Approved final color tokens and contrast checks (incl. accessibility) | Current palette is provisional | With UI production | Blocks later |
| OQ-A03 | Final UI specifications for the complete product (all screens including multi-championship navigation, garage, upgrades, progression, results, rewards) | Design-driven development requires them; the full product needs the complete screen set | Screen by screen before each screen is built (Roadmap Phase 11) | Blocks later |
| OQ-A04 | Environment kit selection order for production (which of the five themes the first environment package represents) | Foundation of environment production work | Before Roadmap Phase 9 | Blocks later |
| OQ-A05 | Production asset pipeline and licensing (environments, vehicles, effects; modeling, texturing, optimization steps, source/licensing rules) | Needed for commercial-grade assets across the full content set | Roadmap Phase 3, refined for Phase 8–9 | Blocks later |
| OQ-A06 | Camera composition and field-of-view specifics | Driving feel and readability | With OQ-01 during driving validation | Blocks later |
| OQ-A07 | Final logo artwork and brand assets | Needed for store/submission identity and UI polish | Before release preparation | Blocks later — no invented logo allowed |
| OQ-A08 | Visual quality acceptance criteria for the high-quality presentation (D-006): per-asset and per-environment quality gates | Without criteria, quality decisions become subjective and inconsistent | Roadmap Phase 3 / at the vertical slice (Phase 7), before large-scale asset production | Blocks later |
| OQ-A09 | Vehicle asset quality requirements (modeling/texturing detail appropriate to hero vehicles, LOD/reflection strategy under WebGL limits) | 10 vehicles must reach the commercial quality bar; prevents both under- and over-production | At/after the vertical slice | Blocks later |
| OQ-A10 | Lighting and post-processing strategy per environment (cinematic presentation vs WebGL cost) | The premium look depends on it; major performance driver | Roadmap Phase 3/7 | Blocks later |
| OQ-A11 | Audio quality and vehicle sound differentiation (engine sound per class/vehicle, music, UI feedback) | Sound quality contributes to the premium feel; drives audio pipeline work | Before Roadmap Phase 12 | Blocks later |

## Platform and release

| ID | Question | Why it matters | When it must be resolved | Blocking? |
|---|---|---|---|---|
| OQ-P01 | Mobile browser QA depth for the commercial release | Playgama requires mobile fullscreen/touch support (`PLAYGAMA_CONSTRAINTS.md`), but the validated device matrix and quality bar are unknown | Roadmap Phase 13–14 | Blocks later |
| OQ-P02 | Final Playgama Bridge integration details per current official documentation | The recorded constraints are baseline notes from project discovery | Re-verify at Roadmap Phase 3 and again before submission | Blocks later |
| OQ-P03 | Exact advertising configuration (ad units, frequency caps, reward amounts) | Monetization tuning; must respect D-021 | After retention/monetization testing | Blocks later |
| OQ-P04 | Analytics event taxonomy | Measurable playtesting depends on it | Before Roadmap Phase 13 | Blocks later |
| OQ-P05 | Title trademark validation | Legal clarity before broader release | Before release preparation | Blocks later |

## Rule
The AI agent may research and propose options, but must not mark an open question as approved without project-owner confirmation.

## Resolved questions
| ID | Resolution | Evidence |
|---|---|---|
| OQ-08 (resolved 2026-11-05) | Owner approved the Commercial V1.0 documentation baseline: (a) the documentation set as the project's source of truth, (b) the Commercial V1.0 scope D-028 as the release target | Owner sign-off communicated 2026-11-05; recorded in milestone register M0 (`production/ROADMAP.md`) and the `DECISIONS.md` change history |
