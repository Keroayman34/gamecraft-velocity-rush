# Production Roadmap — Velocity Rush

**Status:** Proposed roadmap for the **Commercial V1.0** product target (DECISIONS D-028). Phases marked *In progress* are real; all later phases are planned, not completed or started. No calendar dates or durations are assigned — that remains policy until scope, dependencies, and available working time are understood.

The development **scope model** (D-030): the *Development Prototype* validates foundations, the *Vertical Slice* validates the production quality bar, and **Commercial V1.0** is the release target. The prototype is never treated as the product.

**Consolidation note (2026-11-05, owner-approved):** the standalone `production/MILESTONES.md` was consolidated into this file (§Milestone register M0–M12 below) and retired; its full content was migrated verbatim before deletion. This file now holds phases, statuses, dependencies, gates, milestones, acceptance criteria, and remaining work in one place.

## Proposed development phases (in order)

| # | Phase | Outcome | Status |
|---|---|---|---|
| 1 | Documentation & product scope approval | Commercial V1.0 documentation set validated; formal sign-off of this documentation and the Commercial V1.0 scope (OQ-08) by the project owner | **Complete — sign-off recorded 2026-11-05** (OQ-08 resolved; two administrative M0 confirmations — repository name/visibility, branching approach — remain open). **Phase 2 unblocked** |
| 2 | Environment setup | Unity Hub + Editor (Unity 6000.6.8f1 — Unity 6.6 Supported Update, developer-recommended for new projects, not LTS) + C# tooling on Ubuntu; WebGL Build Support verified; clean web-target build; installed version documented | **In progress — environment verification complete 2026-11-05** (owner approved Unity 6000.6.8f1 (Unity 6.6 Supported Update, not LTS; build `237b6593792b`; replacing the originally-planned LTS per OQ-T01 procedure evidence — WebGL Build Support present, license granted, clean batch-mode start); OQ-T01's Playgama-integration scope remains open). Remaining: validate C# tooling against real code (extensions installed 2026-11-05 — VS Code ms-dotnettools.csharp/csdevkit/vstuc/vscode-dotnet-runtime; IntelliSense partially working, debugger not yet verified; no standalone .NET SDK installed — separate owner approval required if wanted), first actual web-target build, and runtime/deployment validation (open OQ-T01 scope) |
| 3 | Technical architecture planning | TDD decisions validated against a real project: Unity version, render pipeline, voice; asset pipeline & quality acceptance criteria (OQ-A08/A05); performance risk approach | **In progress — Phase-3 architecture package complete AND consolidated 2026-11-05** (discovery report + owner provisional review recorded as PROVISIONAL decisions DECISIONS v0.3.3 D-032…D-038 + D-006 note; detailed architecture corrected per audit corrections A1–A4; design package produced — vehicle physics, input+camera, scene/data/save, WebGL performance/build, integration review, readiness report; **the nine standalone Phase-3 documents + MILESTONES.md were consolidated into `docs/TDD.md` Part II (§§28–46) and this file per the owner-approved documentation-consolidation policy**; readiness verdict preserved: *READY WITH DOCUMENTED OPEN DECISIONS* — see `docs/TDD.md` §44.3). Remaining (Phase 3 NOT complete): owner review/approval of the architecture and the draft D-039…D-042 filings (§44.1); final technical decisions gated on validation evidence (A/B pipeline profiling OQ-T02, driving-feel validation OQ-T03, package-install approvals, first web build evidence OQ-T01/T07); no open question resolved |
| 4 | Unity project foundation | Unity project created in-repo; tracked settings; conventions; initial project tooling | Not started — proposed entry conditions and task breakdown below (owner-gated) |
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

## Milestone register M0–M12 (consolidated 2026-11-05 from the retired `production/MILESTONES.md`; content preserved)

Measurable milestones for the **Commercial V1.0** target (D-028). A milestone is complete **only with recorded evidence**; boxes stay unchecked until then. Milestones map to the phases above. Development stages (Development Prototype / Vertical Slice / Commercial V1.0) follow D-030 — the prototype never substitutes for the product.

### M0 — Documentation & Commercial Product Scope Approved
- [x] Documentation set reviewed and approved as source of truth. *(Evidence: owner baseline approval, 2026-11-05; pre-implementation audit completed with findings resolved — see Task 0.1, 2026-11-05.)*
- [x] Art direction document and reference board in place and honored. *(Evidence: `design/ART_DIRECTION.md` + `design/references/art-direction-v0.1/`; wording aligned to D-006 on 2026-11-05.)*
- [x] Formal Commercial V1.0 scope + documentation sign-off (OQ-08) granted by the project owner. *(Evidence: owner approval communicated 2026-11-05; recorded in `docs/OPEN_QUESTIONS.md` Resolved questions and `docs/DECISIONS.md` change history v0.3.1.)*
- [ ] Confirm repository name and visibility. *(Administrative confirmation not yet obtained from the owner.)*
- [ ] Confirm Git branching approach. *(Approach documented in `CONTRIBUTING.md`; explicit owner confirmation pending.)*

**Acceptance criteria:** documentation cross-checked per `docs/TRACEABILITY.md`, no unresolved critical gaps, no contradictions; the owner explicitly approves (a) this documentation set and (b) the Commercial V1.0 scope (D-028: 10 vehicles, ≥3 championships, 5 environments, high-quality realistic-inspired presentation) as the release target. Final release acceptance criteria remain open (OQ-13). *Status: sign-off recorded 2026-11-05 (OQ-08 resolved) — two administrative confirmations (repository name/visibility, branching approach) remain open before M0 closes fully.*

### M1 — Development Environment Ready
- [x] Confirm and document the owner-approved Unity Editor version and verify its installed WebGL Build Support module (OQ-T01). *(Deviation from the original wording, owner-directed: the owner explicitly approved Unity **6000.6.8f1** (Unity 6.6 **Supported Update release, not LTS**; build `237b6593792b`) on 2026-11-05 instead of an LTS — evidence: batch-mode launch exited cleanly, `Editor.log` confirmed version, WebGL Build Support module present, UnityPersonal entitlement granted. Unity guidance treats Supported Updates as production-ready for new projects.)*
- [x] Install Unity Hub and required modules (including WebGL Build Support). *(Evidence: Unity Hub 3.22.5 installed (`/usr/bin/unityhub`); Editor at `~/Unity/Hub/Editor/6000.6.8f1/` with `PlaybackEngines/WebGLSupport/` present — verified 2026-11-05.)*
- [ ] Install/configure C# tooling. *(Partially satisfied 2026-11-05, Task 0.2.3/0.2.8: VS Code 1.129.0 with `ms-dotnettools.csharp` 2.160.4, `csdevkit` 3.40.215, `vstuc` 1.3.2, `vscode-dotnet-runtime` 3.2.0 installed; solution generation verified with the fresh project (expected empty output — no scripts yet). Still unverified: IntelliSense against real project code and the debugger — requires actual C# code to test; no standalone .NET SDK installed (would need a separate owner approval). Item stays unchecked until the tooling is validated with real code.)*
- [ ] Verify a clean Unity project builds for the web target. *(Not yet performed — requires the actual project; also pending: real WebGL build + Playgama Bridge v2.4.0 compatibility + runtime validation = the open scope of OQ-T01.)*
- [x] Document the actually installed Unity version and modules. *(Recorded via `docs/DECISIONS.md` D-008 + `docs/OPEN_QUESTIONS.md` OQ-T01 on 2026-11-05; module list documented: WebGL Build Support + LinuxStandaloneSupport.)*

**Acceptance criteria:** documented Unity version + module list on the Ubuntu workstation; clean project builds successfully for WebGL; findings recorded (e.g., `docs/TDD.md` §2 update). Evidence: build log/screenshot. *No Unity version is silently approved in `DECISIONS.md`.* *Status: environment verification complete 2026-11-05 (3/5 items checked, per owner-approved version Unity 6000.6.8f1 — Unity 6.6 Supported Update, not LTS); remaining: C# tooling config and the first actual web-target build.*

### M2 — Unity Foundation & Architecture Ready
- [ ] Confirm render pipeline with performance evidence (OQ-T02).
- [ ] Define scene and content organization (OQ-T06).
- [ ] Complete TDD and architecture decisions.

**Acceptance criteria:** `docs/TDD.md` §25 deferred list resolved for the decided items with evidence; ADRs recorded per `docs/TDD.md` §25; Unity project committed with correct `.gitignore` behavior.

### M3 — First Vehicle Controllable (development prototype)
- [ ] Basic vehicle movement on a test track.
- [ ] Steering, acceleration, braking on keyboard.
- [ ] Camera follow (temporary default — OQ-01).
- [ ] Baseline performance profiling.

**Acceptance criteria:** vehicle completes a closed test lap without loss of control; input responsive on keyboard; profiling evidence recorded as the first reference numbers (OQ-T08 input). *Prototype content size is a development allowance only (D-030).*

### M4 — Driving Prototype Validated (first critical quality gate)
- [ ] Structured playtests of handling, acceleration/braking, drifting feel, camera feel, driving feedback.
- [ ] Documented results and tuning decisions.

**Acceptance criteria:** playtest evidence supports that driving feels enjoyable and understandable (D-023); verdict and decisions recorded in `docs/DECISIONS.md`; go/no-go before further production is made **by the project owner**.

### M5 — Vertical Slice Validated (production quality gate)
- [ ] One representative section at production quality: vehicle, environment, driving, lighting, UI, audio, gameplay.
- [ ] Performance evidence on target browsers/devices for the high-quality presentation (D-006).
- [ ] Quality acceptance criteria drafted and applied (OQ-A08/OQ-A09) where defined.

**Acceptance criteria:** the slice demonstrates the intended commercial quality bar in review; profiling results recorded in `PLAYGAMA_CONSTRAINTS.md` performance evidence notes; owner confirms the quality bar is representative before mass content production (Phases 8–10) begins.

### M6 — Vehicle Production Complete (10 vehicles)
- [ ] 10 production vehicles built (classes per D-014: Starter 2 / Sport 3 / Drift 2 / Supercar 3).
- [ ] Per-vehicle specs approved (OQ-14/OQ-06): stats, class fit, upgrade mapping.
- [ ] Quality criteria (OQ-A09) met for each vehicle; provenance/licensing recorded.

**Acceptance criteria:** all ten vehicles pass vehicle-package checklists (`design/vehicles/README.md`), including quality review against references and WebGL profiling evidence (OQ-T08); roster statistics approved; no unlicensed identity.

### M7 — Environment Production Complete (five distinct themes)
- [ ] Five environments produced: Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena.
- [ ] Each environment has distinct identity and driving experience (distinctness review OQ-20).
- [ ] Track readability/gameplay visibility verified per environment; performance evidence recorded.

**Acceptance criteria:** per-environment packages complete (`design/environments/README.md` items); visually distinct in review (not one kit with cosmetic swaps); track counts/layouts per approved design (OQ-21); performance evidence per environment.

### M8 — Championship & Event Content Complete
- [ ] ≥3 championships with approved names/identities (OQ-15) and approved structure (OQ-16).
- [ ] Approved total event count (OQ-18) implemented across environments (OQ-19).
- [ ] Event progression and unlocking per design (OQ-17/OQ-05).

**Acceptance criteria:** all designed events playable end-to-end; championship mapping documented; no invented event counts beyond the approved plan.

### M9 — Gameplay, Progression, Garage, Upgrades & UI Integration Complete
- [ ] Full gameplay systems integrated: scoring/results, rewards, upgrades, garage/roster, save/progression (OQ-09 rules resolved), settings.
- [ ] Complete commercial UI implemented against approved specifications (OQ-A03), bilingual readiness verified.

**Acceptance criteria:** complete loop (Select Championship → Select Event → Prepare Vehicle → Drive → Results → Rewards → Upgrade/Unlock → Progress → Replay) works end-to-end in a build; per-screen RTL/LTR acceptance criteria pass; no D-020 excluded feature present.

### M10 — Audio, Localization & Monetization Integrated
- [ ] Audio implemented incl. vehicle sound differentiation (OQ-A11), with correct pause/resume behavior.
- [ ] Arabic (RTL) + English (LTR) localization complete across all screens.
- [ ] Ad integration per D-021/D-027 rules (no ads during driving; rewarded grants gated).

**Acceptance criteria:** localization audit passes (all strings, RTL mirroring); audio plays/pauses correctly with ads and tab state; monetization respects approved rules (config still OQ-P03).

### M11 — Playgama Integration Complete
- [ ] Bridge SDK installed at the planned phase (Roadmap Phase 13) and integrated.
- [ ] Rewarded + limited interstitial ads with correct pause/resume and reward gating.
- [ ] Bridge Analytics events flowing; no external analytics.
- [ ] Save via the Bridge-backed strategy (OQ-T05/OQ-09 resolved).

**Acceptance criteria:** `PLAYGAMA_CONSTRAINTS.md` §6 integration-applicable submission checklist passes; performance risk register items relevant to integration (`PLAYGAMA_CONSTRAINTS.md` §5) have no open blockers; rewards granted only after confirmed rewarded completion; no ad-break corruption of game/audio/input state.

### M12 — QA, Optimization & Release Readiness
- [ ] Full platform QA across supported browsers/devices (mobile + desktop) — OQ-P01 depth resolved.
- [ ] Performance/memory/archive optimization meets re-verified platform limits (size ≤ re-verified limit, startup memory < re-verified limit, no startup warnings, acceptable loading).
- [ ] Responsive layouts without overflow/clipping; popup closeability; Escape conflicts absent.
- [ ] Submission package correctness (archive root `index.html`, Latin filenames).
- [ ] Final Commercial V1.0 acceptance criteria (OQ-13) verified.

**Acceptance criteria:** every testing + submission checklist item in `PLAYGAMA_CONSTRAINTS.md` §§4/6 checked with recorded evidence; performance risk register (§5) items closed or explicitly accepted; official Playgama documentation date/version used is recorded in the release report; final release criteria (OQ-13) verified.

### Historical mapping (preserved)
Former milestone IDs from the MVP-era plan were re-mapped at the Commercial V1.0 transformation (2026-11-03): former M5 "First Playable Event" is absorbed into M3/M8; former M6 "MVP Feature Completion" is superseded by M9; former M7 "Playgama Integration" is now M11; former M8 "QA & Release Readiness" is now M12. The former "MVP feature completion" concept no longer applies — the product target is Commercial V1.0.

### Evidence rule (preserved)
Each completed milestone links its evidence (playtest notes, build logs, profiling output, QA reports) and is confirmed by the project owner. Never mark a milestone complete on agent self-assessment; never present prototype content as Commercial V1.0 progress.

## Proposed Phase-4/5 task breakdown (from Phase-3 planning — proposals only, owner-gated)

Dependency-ordered; task numbering is a suggestion, not fixed; every step remains owner-gated per governance rules.

**Phase 4 — Architecture & foundation**
1. `T-4.1` Owner reviews the consolidated architecture (`docs/TDD.md` Part II) (gates everything else).
2. `T-4.2` Record accepted proposals as DECISIONS/ADR entries + update open-question statuses in a documentation pass.
3. `T-4.3` Owner-approved package steps as required (Input System per D-034 gate; URP if the owner leans that way on P1) — manifest edits with full approval.
4. `T-4.4` Create `Assets/` skeleton minimal folders + assembly definitions (±§33.1 "Yes now" set; no gameplay content yet) — recorded in TDD via ADR.
5. `T-4.5` Boot composition + GameFlow state-machine stubs + scene-transition utility (no UI visuals).
6. `T-4.6` Local build automation script + first batch web build of the empty project → **first OQ-T08 empty-data evidence** entry (also D-038's first gate).
7. `T-4.7` Profiling instrumentation (Profiler markers, FPS/memory capture into the evidence log).

Status notes (2026-11-05, owner-directed Parts 4.1–4.2):
- Part 4.1: the `Assets/` skeleton folders and the four §33.1 "Yes now" assembly definitions were created and validated by Unity batch import (zero compile errors); conventions accepted by the owner and registered as D-043. Details: `docs/TDD.md` §33 implementation note.
- Part 4.2 (a `T-4.5` subset — boot composition only): persistent-root `Assets/Scenes/Bootstrap.unity` added to Build Settings index 0, `Bootstrapper` composition root + ordered fail-closed `ApplicationStartup` implemented in `VelocityRush.Core`, EditMode tests 7/7 passing, final clean compile 0 errors / 0 warnings. **Not yet done from `T-4.5`: GameFlow state-machine stubs and the scene-transition utility** (owner-directed prohibition on fake GameFlow implementations in this step — separate follow-up task). Details: `docs/TDD.md` §35 implementation note.

**Phase 5 — Core driving prototype**
8. `T-5.1` Input abstraction implementation (interface layer; package install per D-034 gate). — **COMPLETE** (2026-11-09).
9. `T-5.2` WheelCollider vehicle rig per data-driven profile + 1 test track (D-030 allowance; temporary content only). — **COMPLETE** (2026-11-09).
10. `T-5.3` Camera rig (§38) in data-driven profile form. — **COMPLETE** (2026-11-09).
11. `T-5.4` First track chips: checkpoint/time capture; minimal race event loop driving Results. — **COMPLETE** (2026-11-08).
12. `T-5.5` First playable prototype build (web) + measure → OQ-T08 prototype evidence gate. — **COMPLETE** (2026-11-10).

Status note (2026-11-08, T5.4 prototype implementation — **complete; actual-project validation passed**):
- Implemented the minimal race loop on the existing straight `TestTrack` as an explicitly **one-way timed run** (not a lap): `Countdown → Running → Finished`, configurable 2 s prototype countdown, monotonic `Stopwatch` race timer that excludes countdown and freezes at finish, three ordered checkpoint trigger gates, and a finish gate blocked until all checkpoints are accepted.
- Added an immutable `RaceResult` (elapsed seconds, accepted/required checkpoints, completion) and exactly-once `RaceFinished` delivery; duplicate finish/checkpoint trigger contacts cannot emit a second result or advance progress. No UI/Results screen, scoring, rewards, progression, save, multiplayer, AI, or audio was added.
- Retry is an in-scene reset (state, progress, timer, result guards, vehicle spawn pose, linear/angular velocity) driven by the existing retry intent; it performs no `SceneManager` load/unload or object recreation.
- Reconciliation with the planned "driving Results" wording: this slice intentionally stops at the Race Rules result notification because Results UI is out of scope (see task scope and TDD §35.5). The eventual Results handoff remains for the authorized UI/GameFlow step.
- Validation: actual-project Unity 6000.6.8f1 batch import/compile passed with 0 compiler errors and 0 warnings; the full suites passed (**129/129 EditMode**, **4/4 PlayMode**, zero failures); all `TestTrack` script references resolve; retry was verified to reuse the same scene instance and vehicle with no reload. The isolated-copy run had produced the same counts (129/129, 4/4).

Status note (2026-11-08, T5.5.1 WebGL development build tool — tool validated; build/runtime evidence completed 2026-11-10):
- Added the permanent editor-only tool `Assets/Editor/VelocityRushWebGLBuilder.cs` and the menu entry `Tools/GameCraft Studio/Velocity Rush/WebGL Build`.
- Preflight is blocking: editor idle, WebGL Build Support installed, active build target WebGL, and exactly `Assets/Scenes/Bootstrap.unity` then `Assets/Scenes/TestTrack.unity`, both enabled. The tool never adds, removes, reorders, or enables scenes.
- Output is an explicit folder or the documented default `~/VelocityRushBuilds/WebGL/dev-<timestamp>` (outside the Unity project). Non-empty folders require explicit confirmation; no previous output is deleted.
- Builds use the WebGL **Development** player and write `velocity-rush-webgl-build-report.json` next to the output with the BuildReport summary, result, duration, errors/warnings, and artifact sizes for index.html/wasm/data/framework.js/loader.js.
- Validation: actual-project editor compile passed with 0 errors/warnings; the menu item resolved; 9/9 preflight checks passed; four negative guards passed (inside-project output rejected, non-empty output requires confirmation, confirmation clears the guard, build refused when preflight fails); the JSON report validated. **No WebGL build was executed in that step.** T5.5 was subsequently completed on 2026-11-10 (see the closure note below).

Status note (2026-11-09, T5.1/T5.2/T5.3 prototype modules — **T5.1, T5.2 and T5.3 COMPLETE; Phase 5 remained IN PROGRESS pending T5.5, subsequently completed 2026-11-10**):
- **T5.1 (Input abstraction)** — owner-accepted: `IVehicleInput`/`IAppInput` in `VelocityRush.Core`, an allocation-free legacy keyboard adapter, and a `StateChanged` reset seam (`AppState` + `IAppStateSource`) bound explicitly by the composition root. EditMode coverage includes normalization, lifecycle, and reset-on-transition.
- **T5.2 (Vehicle rig)** — `VehiclePhysicsProfile` now drives engine torque falloff, front/rear brake bias, steering speed sensitivity, suspension, friction, and rigidbody parameters; `VehicleController` uses one code path for all vehicles and publishes polled `VehicleTelemetry` through the new `IVehicleTelemetry` Core interface (speed, forward/lateral speed, wheel slip, grounded). Profile validation rejects non-positive mass/travel, out-of-range bias, and invalid or out-of-range response curves. The authored prototype profile serializes the placeholder response values.
- **T5.3 (Camera rig)** — `VehicleCameraRig` now consumes `IVehicleTelemetry` (no direct `Rigidbody` reads), applies speed look-ahead, a critically damped translation spring plus a deterministic critically damped quaternion spring, a linear FOV ramp, and non-allocating sphere-cast avoidance. A dedicated `Track` layer (8) was added; the prototype scene assigns track/environment objects to it and the rig's serialized mask is Track-only.
- Validation (actual project, Unity 6000.6.8f1 batch mode): compile **0 errors / 0 warnings**; EditMode **160/160**; PlayMode **6/6** (including two new camera tests for telemetry resolution, Track-only mask, finite follow pose, distance bounds, and FOV bounds). Zero per-frame allocations were introduced in `FixedUpdate`/`LateUpdate` paths (struct math and preallocated hit buffers only).
- At that point Phase 5 remained open pending T5.5 (first playable web build + OQ-T08 measurement) and its browser runtime validation.

Status note (2026-11-10, T5.4/T5.5 completion — **PHASE 5 COMPLETE**):
- **T5.4 (race loop)** — complete: one-way timed run with `Countdown → Running → Finished`, three ordered checkpoint gates, a finish gate, an immutable `RaceResult`, exactly-once delivery, and an in-scene retry reset. Details remain in the 2026-11-08 note.
- **T5.5 Stage 1 (WebGL Development build)** — succeeded in `dev-20261109-210202`: exit 0, 0 errors, 0 warnings, cold build duration **17.354 s**, BuildReport total **29,722,007 B**. Payload: `index.html` 8,773 B; `.wasm` 25,226,369 B; `.data` 3,724,581 B; `framework.js` 687,861 B; `loader.js` 60,309 B. The five-artifact payload is **29.71 MB (~9.9%)** of the 300 MB Playgama archive limit, leaving ~270 MB headroom. Archive/download size is not runtime memory.
- **T5.5 Stage 2 (OQ-T08 browser runtime)** — validated in Chromium 152 / WebGL 2.0 from a local static server: Bootstrap completed in **2–3 ms**, `TestTrack` loaded, and there were no WebGL startup errors or warnings. The wasm linear heap initialized at **95,944,704 B (91.50 MiB)**, ~4.5% of the 2 GB WebGL budget; the JS heap held **~9.5 MiB** against a 4,192 MiB V8 limit. No observable GC hitches occurred during the active race loop; steering, acceleration, braking, and the telemetry-driven chase camera operated correctly. The prototype renders untextured (white) by design — OQ-T02 art/pipeline work is deferred. These are prototype measurements, not final budgets.
- **T5.5.3 (tool hardening)** — `build.reportPath` now serializes the real path; added the permanent batch entry `VelocityRush.EditorTools.VelocityRushWebGLBuilder.BuildFromCommandLine` (`-vrOutput` or `VR_WEBGL_OUTPUT`, optional `-vrAllowNonEmptyOutput` or `VR_WEBGL_ALLOW_NONEMPTY`) with non-zero exit on failure; default outputs are collision-free; output folders that contain the project are refused; overwrite consent is scoped to the confirmed path; and the editor window clears stale summaries on reload and on output-path edits. A refused non-empty build left its sentinel untouched; a clean CLI build exited 0 and wrote a populated `reportPath`.
- Final code-state validation: compile **0 errors / 0 warnings**; EditMode **160/160**; PlayMode **6/6** (0 failed, 0 skipped).
- **Phase 5 is officially COMPLETE (2026-11-10).** Next gate: Phase 6 driving-feel validation (D-023).

**Phase 6 — Driving-feel validation (D-023 gate)**
13. `T-6.1` Feel-tuning data iterations + daily playtest cadence; hybrid-fallback decision only if the gate fails.
14. `T-6.2` OQ-T02 A/B render-pipeline profiling on the same prototype; formal owner pipeline decision.

**Phase 7 / Phase 13** proceed per the phase table and their own gates (vertical-slice quality gate; Bridge integration behind the OQ-P02 re-verification ritual).
