# Technical Design Document — Velocity Rush

**Version:** 1.1-planning (Commercial V1.0; consolidated 2026-11-05)
**Status:** Technical planning for the Commercial V1.0 quality bar (DECISIONS D-006/D-028). **Architecture not yet approved.** Every architecture item is `Proposed`, `Recommendation`, or `Open` unless explicitly approved in `DECISIONS.md`. Do not infer final architecture from this document. Documentation completion ≠ implementation validation: no code, scene, package, or build exists.

**Consolidation note (2026-11-05, owner-approved):** the nine standalone Phase-3 root documents (technical architecture discovery, detailed game architecture, architecture audit, vehicle physics design, input & camera design, scene/data/save design, WebGL performance & build design, architecture integration review, final readiness report) were consolidated into this document (Part II, §§28–46) and retired from the repository root. Their unique content was migrated intact before deletion; decision statuses, open questions, and evaluation statuses are preserved untouched (§44/§45; see `CHANGELOG.md`). This document is now the single authoritative home for technical architecture.

## Table of contents

- **Part I — Baseline technical direction** (§§1–27): approved direction, platform constraints, Playgama plan, high-level boundaries, per-domain Open items (input/vehicle/camera/UI/loc/save/audio/assets/visual), performance gates, testing, deferred decisions, P1–P8 provisional directions.
- **Part II — Phase-3 detailed architecture (consolidated)** (§§28–49): discovery synthesis & platform facts (§28) · architecture goals & 15-system responsibilities (§29) · GameFlow & state model (§30) · UI navigation (§31) · communication (§32) · assembly boundaries (§33) · data definitions & ownership (§34) · scene & runtime lifecycle (§35) · vehicle physics (§36) · input (§37) · camera/presentation (§38) · save (§39) · WebGL performance & build (§40) · error/recovery (§41) · testing & validation (§42) · integration review results (§43) · draft decision records & owner-gated register (§44) · risks (§45) · references (§46) · vertical-slice Stage-1 definition, quality criteria, asset pipeline, lighting and UI standards (§47) · Phase-9 environment production (§48) · Phase-10 AI system: pathing foundation and kinematic controller (§49)

Part I sections remain valid high-level statements; where they overlap Part II, **Part II is the more detailed authoritative treatment** of the same concept.

# Part I — Baseline technical direction

## 1. Purpose
Define how Commercial V1.0 — a high-quality, realistic-inspired 3D driving game — will be implemented in Unity/C# for Playgama Web, and the **planning process** by which unknown architecture will be decided. High visual quality must be planned together with browser performance: never assume an arbitrary desktop visual feature is deliverable in WebGL; document tradeoffs and enforce profiling gates.

## 2. Approved technical direction

| Area | Decision | Source |
|---|---|---|
| Engine | Unity **6000.6.8f1** (Unity 6.6 Supported Update release, **not LTS**; build `237b6593792b`) — owner-approved 2026-11-05, environment verified (Hub 3.22.5, WebGL Build Support present, licensing granted, clean batch-mode startup); runtime Playgama integration still pending (OQ-T01) | DECISIONS D-008, `OPEN_QUESTIONS.md` OQ-T01, milestone register M1 (`production/ROADMAP.md`) |
| Language | C# | DECISIONS D-008 |
| Product target | Commercial V1.0 (prototype/vertical-slice are development stages, D-030) | DECISIONS D-028/D-030 |
| Visual quality bar | High-quality realistic-inspired 3D presentation, planned with WebGL performance limits | DECISIONS D-006 |
| Target distribution | Playgama Web (Unity WebGL) | DECISIONS D-009 |
| Runtime platform constraints | WebGL/browser limits per `PLAYGAMA_CONSTRAINTS.md` | Platform requirements |
| Localization | Arabic (RTL) and English (LTR) | DECISIONS D-013 |
| SDK dependency | Playgama Bridge SDK (Unity Package Manager via official Git repo) — integrated only at the planned phase, not during documentation | `PLAYGAMA_CONSTRAINTS.md` #1 |

## 3. Unity Web platform & WebGL Build Support (constraints, not solutions)
- Unity WebGL output must satisfy Playgama packaging rules: `index.html` at archive root, Latin filenames, no external asset hosting, archive-size and startup-memory limits (`PLAYGAMA_CONSTRAINTS.md` §§1, 6).
- WebGL Build Support module: **verified present** for Unity 6000.6.8f1 at environment verification (2026-11-05, OQ-T01 — environment portion complete); compatibility with an actual project build and Playgama integration remains pending.
- Avoid WebGL startup warnings; no YouTube embedding; no external analytics; shared browser keys (e.g., Escape) never critical to flow.
- Loading time, mobile fullscreen/rotation, and touch quality are first-class requirements of a commercial web product.

## 4. Playgama Bridge integration plan (Proposed, phase-gated — Roadmap Phase 13)
- **Boundaries to validate against the actual SDK:**
  - Ads: rewarded/interstitial invocation, pause/resume hooks, reward grants gated on confirmed rewarded completion (D-021/D-027).
  - Analytics: Bridge Analytics only (D-022); taxonomy open (OQ-P04).
  - Save: per the save API the current SDK provides (OQ-T05/OQ-09).
- Verify current SDK capabilities from official documentation before integration (OQ-P02). SDK is not installed during documentation phases.

## 5. High-level system boundaries (Proposed — detailed treatment in §29)
Planning frame, not an approved architecture:
- **Vehicle / driving simulation** — control, physics, telemetry (speed, drift, contacts). Deepest iteration risk; owns the driving-feel milestone.
- **Camera** — presentation and readability during driving; selection open (OQ-01/OQ-A06).
- **Input** — device abstraction for keyboard, mouse, and touch (OQ-T04).
- **Event / championship systems** — event rules, per-type scoring, championship structure, progression, unlocks (design open).
- **Garage / roster / upgrades** — vehicle catalog, statistics, upgrade state.
- **Progression / save** — championship state, unlocks, bests, settings.
- **UI** — screen orchestration, localization/RTL, responsive layout.
- **Audio** — vehicle/engine sound differentiation (OQ-A11), music, feedback.
- **Track / environment content** — five-theme content architecture (OQ-20/OQ-21).
- **Platform integration** — the only layer that talks to the Bridge SDK.
No class hierarchy, DI framework, or namespace plan is proposed or approved at this stage.

## 6. Proposed code organization (Proposed outline — open until validated)
Organization is defined **inside the Unity project** after creation. Nothing here is approved: no namespace layout, no folder-level C# structure, no pattern (MVC/MVVM/ECS/service locator) has been chosen. Deciding inputs: input strategy (OQ-T04), vehicle-physics strategy (OQ-T03), UI/localization approach, and content-scaling needs for 10 vehicles / 5 environments. The assembly-boundary working candidate is §33; final names/organization are a Phase-4 owner-gated conventions task.

## 7. Scene management and content scaling (Open — candidates in §35)
- Expected scenes include at minimum: driving/test scene(s), UI scene(s), platform bootstrap; a growing commercial content set raises additive-loading and memory questions (OQ-T06).
- Fast-retry UX (`GDD.md` §6) may imply a low-friction scene reload path — validate early (§30.5 owns the retry-context rules).
- Scene strategy must scale to five environments without five-fold loading costs — likely via shared runtime systems and environment content packages (proposed; unvalidated).

## 8. Input abstraction (Open — OQ-T04; detailed design §37)
- Desktop: keyboard + mouse; mobile: touch (Playgama constraints 13/14/16/22).
- Input must be abstracted from the vehicle controller so touch adaptation does not break keyboard tuning; device-detection and layout rules per `GDD.md` §27.

## 9. Vehicle system and physics architecture considerations (Open — detailed design §36)
Expected responsibilities, not an implementation plan:
- Steering, throttle/brake response, drift state, jump/landing state, contact/obstacle state; telemetry output for scoring.
- Per-vehicle/per-class handling differentiation must be data-driven enough for 10 vehicles to tune without code forks (proposed rationale for a data-driven vehicle configuration approach; strategy itself open).
- Strategy choice (native vs package-based simulation) deferred to technical discovery after the Unity project exists.

## 10. Camera system responsibilities (Open — detailed design §38)
- Readable driving first, cinematic presentation second; perspective default, FOV, smoothing/shake pending driving-feel validation.

## 11. UI architecture (Open, constraint-driven; navigation rules §31)
- Full commercial screen set (`design/ui-ux/README.md`): championship navigation for ≥3 championships, event selection, garage, upgrades, pre-event, HUD, pause, results, settings, and reward/unlock presentation.
- Must support: safe-state orchestration for ads, pause/resume, popup closeability, responsive landscape layouts, RTL/LTR mirroring, persistent language preference. Restoration strategy after large UI flows must not leak state.

## 12. Localization and RTL/LTR (Open, language = Arabic/English)
- All UI strings localized from a catalog; never hard-coded.
- RTL mirroring driven by language state; Arabic uses neutral copy.
- Fonts verified for commercial license + Arabic/English coverage + size (OQ-A01; `design/brand/README.md`).
- Concrete Unity localization strategy: open until the Unity project exists (package/tooling selection owner-gated; the §29 S13 catalog seam is package-agnostic).

## 13. Save and progression architecture (Open — OQ-09/OQ-T05; detailed design §39)
- Expected shape: language/settings, best results, stars, unlocks, upgrade levels, championship progress — sized for the commercial save set, not a prototype subset.
- Direct-file vs Bridge-backed persistence, versioning, migration defaults: decided against the actual Bridge API during Phase 13 planning.

## 14. Ads and analytics integration boundaries (Open, direction approved D-021/D-022)
- Gameplay code must not depend on the SDK directly; platform integration is isolated in one layer (§5/§29 S15).
- Reward grants only after confirmed rewarded completion; never during driving (`PLAYGAMA_CONSTRAINTS.md`).

## 15. Asset management and pipeline (Open — OQ-A05/OQ-A09)
- Imports tracked with provenance/license/intended-use metadata (`docs/AI_AGENT_GUIDELINES.md` §6).
- Reference/generation images never enter runtime folders as production assets.
- Commercial-scale asset pipeline (import → optimization → variant/LOD policy → validation) is designed around the vertical-slice proof point, not assumed beforehand.

## 16. Material and shader strategy (Proposed, validation-gated)
- Goal: convincing realistic-inspired surfaces (D-006) within WebGL limits: controlled shader complexity, restrained transparency, tiling/atlas discipline, and a vehicle paint/reflection approach proven by profiling rather than requested from art intuition.
- Any expensive surface feature (real-time reflections, stacked effects) must pass an explicit profiling gate before being adopted as standard. Gate outcomes recorded (OQ-A08/T08).

## 17. Lighting strategy (Proposed, validation-gated — OQ-A10)
- Cinematic lighting for presentation scenes; gameplay lighting preserves road and obstacle readability.
- Plan must compare: baked vs mixed vs minimal real-time lighting; limited real-time shadows; restrained post-processing. Chosen only after browser performance evidence (OQ-T02/T08), never by default.
- **Measured constraint (2026-11-10):** the prototype lighting is authored for the Built-in Pipeline. The T-6.2 A/B showed URP renders the same scene 71.4% darker with equivalent light intensity/shadow distance, so any future pipeline switch must re-author light intensities/exposure and re-run the same-scene comparison before art production (§28.5).

## 18. Visual effects strategy (Proposed, validation-gated)
- Effects (tire smoke, sparks, speed lines, dust, atmosphere) must communicate events without obscuring the road; overdraw/particle cost constrained per WebGL test data.
- Five distinct environments will not share one effect suite blindly; per-environment atmosphere must pass individual cost checks (OQ-20).

## 19. Audio system (Open — OQ-A11)
- Separate audio boundary owning vehicle sound differentiation (engine sound per class/vehicle), surface feedback, UI feedback, and music.
- Must pause/resume correctly with ads and browser tab behavior; license-clean sources only; acceptable memory footprint on web targets. Prototype scope (§29 S12): engine hum + UI click, silent until first browser gesture.

## 20. Performance constraints, profiling gates, loading and memory management (binding, values open — OQ-T08)
- Platform win conditions: archive ≤ 300 MB (re-verify), startup memory < 2 GB (re-verify), acceptable loading time, smooth driving on representative devices, no startup warnings — see `PLAYGAMA_CONSTRAINTS.md` and its performance risk register.
- **Profiling gates rule:** no performance-sensitive feature (shader, lighting mode, VFX, uncompressed texture, audio stream) becomes standard before a representative WebGL profiling measurement documents its cost (recorded in `PLAYGAMA_CONSTRAINTS.md` performance evidence log).
- Loading/memory management proposals (content streaming vs full-load, addressable strategies, texture compression) are validated only after the Unity project exists; no specific API final choices here.
- Do not present untested values as achieved performance or as guarantees. The measured-evidence ritual is §40.18/§40.23.

## 21. Testing strategy (Open, expectations stated — detailed architecture §42)
- Documentation phase: consistency/link review.
- Prototype phase: editor playmode validation + profiling evidence.
- Later phases: automated checks for scoring/progression rules; browser/device stability matrix per `PLAYGAMA_CONSTRAINTS.md`; visual quality gate reviews (OQ-A08); regression protection as content volume grows (OQ-T07).
- CI/build automation: proposed after Unity setup (D-038 deferral, §40.21).

## 22. Browser compatibility (Open, requirement-driven)
- Test stability across the supported browser/device matrix (Playgama constraint 23), covering desktop keyboard/mouse and mobile fullscreen/touch/orientation; mobile quality bar OQ-P01.
- Startup warnings absence, memory ceiling, loading time, and orientation handling are release-blocking checks, re-verified per platform documentation.

## 23. Build and deployment workflow (Proposed, pending Unity setup — strategy §40)
Loop: local Unity WebGL build → archive structure per Playgama rules → Bridge-level testing → submission rehearsal on Playgama. Automation level decided after the Unity project exists. Never built during documentation phase.

## 24. Security and platform compliance
- Never commit secrets, credentials, tokens, or private keys (`.gitignore` guards `.env*`).
- No external analytics services; no external auth gates; no URL-based operation restrictions; no YouTube embeds; external links only through the Bridge capability gate (`PLAYGAMA_CONSTRAINTS.md` §3).
- Player privacy: invent no data collection beyond platform-approved Bridge Analytics (final scope verified at integration).

## 25. Architecture decision rule & deferred decisions
For each major technical decision, document problem, options, trade-offs, chosen approach, consequences — recorded as ADR entries when the docs/architecture structure is introduced.

**Explicitly deferred until Unity project creation + technical prototype/vertical slice evidence:**
- Render pipeline (OQ-T02) — **RESOLVED 2026-11-10:** Built-in retained for the development prototype after the measured A/B; URP deferred with a Phase-7 re-evaluation trigger (D-032, §28.5). Engine version remains Unity 6000.6.8f1 (owner-approved 2026-11-05)
- Vehicle physics strategy (OQ-T03) — **RESOLVED 2026-11-10:** WheelCollider Simcade approved after the D-023 playtest; hybrid custom-force fallback not adopted (D-033)
- Input abstraction details (OQ-T04)
- Scene/content organization (OQ-T06)
- Save strategy (OQ-T05/OQ-09)
- Build/CI automation (OQ-T07)
- Performance budgets (OQ-T08) and quality acceptance criteria (OQ-A08/OQ-A09)

**Owner-reviewed provisional directions for every item above were recorded 2026-11-05 in `DECISIONS.md` D-032…D-038 (+ D-006 review note) and summarized in §27 below.** OQ-T02 and OQ-T03 were resolved on 2026-11-10 by the Phase-6 A/B and driving-feel evidence (D-032/D-033). Every remaining item stays deferred: a provisional direction is planning input, not a final decision, and resolution still requires the listed validation evidence.

## 26. Do not implement yet
No final class diagrams, namespaces, service locators, dependency-injection frameworks, or manager hierarchy has been approved. Avoid creating them before technical discovery and the prototype validate them. Do not reduce product scope to prototype scope; the Commercial V1.0 target (D-028) always governs.

## 27. Phase-3 architecture review — provisional directions (owner-reviewed 2026-11-05; NOT finalized architecture)
The owner reviewed the Phase-3 Technical Architecture Discovery report (discovery synthesis preserved in §28) and provisionally agreed with the eight proposed directions (P1–P8). Recorded 2026-11-05 in `DECISIONS.md` v0.3.3 as PROVISIONAL entries D-032…D-038 plus a review-note update to D-006 (P8). **These are planning directions, not finalized architecture**; §25 and §26 remain in force, and every governing open question remains OPEN.

| # | Provisional direction | Decision entry | Governing open question | Final decision requires |
|---|---|---|---|---|
| P1 | Controlled Built-in vs URP comparison before any pipeline selection; URP not installed now | D-032 | OQ-T02 | **Met 2026-11-10** — same-scene A/B measured; Built-in retained, URP deferred (D-032, §28.5) |
| P2 | WheelCollider-first prototype physics; custom hybrid physics documented as fallback | D-033 | OQ-T03 | **Met 2026-11-10** — D-023 playtest passed; WheelCollider Simcade approved, hybrid fallback not adopted (D-033) |
| P3 | Unity Input System preferred direction behind input-abstraction interfaces; **package installation NOT authorized by this review** | D-034 | OQ-T04 | Prototype input validation + explicit owner approval of the manifest change |
| P4 | ScriptableObject definition assets; runtime mutable state kept separate from definition assets | D-035 | Data values remain owner-gated (OQ-06/07/14/15/16/17/18/19) | Architecture design detail: asset organization + loading strategy |
| P5 | SaveModel + ISaveStore + SaveService layering; storage provider undecided; Bridge strictly deferred to Phase 13 | D-036 | OQ-09 / OQ-T05 | Browser-storage validation + Phase-13 Bridge reality check + owner policy approval |
| P6 | Additive scene loading (bootstrap + additive content) as a candidate scene organization | D-037 | OQ-T06 | Practical loading/memory and vertical-slice validation |
| P7 | Local batch build automation first; CI considered later when justified by release needs | D-038 | OQ-T07 | First empty-project batch build evidence; CI deferral recorded, not rejected |
| P8 | Measurement-first performance planning; no invented numeric budgets; evidence from Empty Build → Prototype → Vertical Slice | D-006 review note (profiling-gate rule, §20 unchanged) | OQ-T08 | Measured evidence recorded at the three build stages |

**Unresolved implementation details deliberately still open** (must not be invented now): final pipeline settings; WheelCollider tuning values and any assist code; input action maps / data layout; ScriptableObject loading strategy (Addressables vs Resources) and folder organization; scene names and organization; save payload format, versioning, and migrations; CI tooling choice; all numeric performance budgets. Architecture remains `Proposed`/`Open` unless `DECISIONS.md` records a final approval with evidence.

**Phase-3 design package (2026-11-05):** the provisional directions above were expanded into implementation-facing design content — originally across nine standalone documents, now consolidated as Part II of this document (§§28–46) after audit corrections A1–A4 were applied. Their areas: detailed architecture (§§29–35), vehicle physics (§36, OQ-T03/D-033), input+camera (§§37–38, OQ-T04/D-034, OQ-01/OQ-A06), scene/data/save (§§34–35/§39, P6/D-037, D-035, D-036), WebGL performance/build (§40, P8 evidence ritual), integration review (§§41–43), draft decision records D-039…D-042 **proposed but NOT filed** (§44 — filing is owner-gated). All remain recommendations; no decision status was changed by that pass or by this consolidation.

# Part II — Phase-3 detailed architecture (consolidated 2026-11-05)

> Reading discipline: Part II carries the full technical content of the retired Phase-3 standalone documents. Sections marked 【Recommendation】 are this package's own engineering positions needing owner approval; 【Provisional】 = D-032…D-038 directions; 【Open】 = unresolved with the given OQ ID; 【Owner gate】 = explicit owner approval required; 【Approved】 = already-binding decision.

## 28. Discovery synthesis, project assessment, and platform facts

### 28.1 Discovery executive summary
- The project started Roadmap Phase 3 at the documented starting point: clean minimal Unity 6000.6.8f1 bootstrap (built-in renderer baseline, zero gameplay/content packages) with an approved documentation baseline.
- Eight architecture areas were investigated with evidence-based recommendations only, each mapped to a governing open question — nothing resolved.
- Highest-consequence findings: (a) Web GC behavior documented by Unity materially shapes runtime architecture (no per-frame allocations, pooling/`NativeArray`); (b) WheelCollider (Vehicles module, already in the manifest) is the strongest Simcade baseline but must be proven on WebGL with profiling; (c) URP-vs-Built-in (OQ-T02) remains genuinely undecided — both viable; the prototype must measure before the owner commits (resolved 2026-11-10 by the T-6.2 A/B, §28.5: Built-in retained, URP deferred).
- Implementation order defers all mass content and Playgama Bridge integration behind a validated driving prototype (D-030, D-023, 14-phase roadmap).

### 28.2 Project assessment (documented vs. actual, verified 2026-11-05)
| Item | Documented state | Actual repository evidence | Match |
|---|---|---|---|
| Unity project at repo root, no invented Assets architecture | `PROJECT_STRUCTURE.md` §2 | `Assets/` empty (0 files); Unity folders at root | ✅ |
| Engine version owner-approved, env verified | D-008, OQ-T01 (environment portion complete) | `ProjectSettings/ProjectVersion.txt` = `6000.6.8f1 (237b6593792b)` | ✅ |
| Identity normalized | Task 0.2.5 | `companyName: GameCraft Studio`, `productName: Velocity Rush` | ✅ |
| Render pipeline decided for the development prototype (2026-11-10) | D-032, OQ-T02, §28.5 | `GraphicsSettings.asset`: `m_CustomRenderPipeline: {fileID: 0}`; no URP/HDRP in manifest; same-scene A/B evidence in `PLAYGAMA_CONSTRAINTS.md` §5 | ✅ (Built-in retained; URP deferred) |
| Playgama Bridge not installed | §4/§24; OQ-T01 pending scope; constraints #1 | Not in manifest/lock | ✅ |
| Packages | Only IDE packs expected | `manifest.json`: `com.unity.ide.rider` 3.1.0, `com.unity.ide.visualstudio` 2.0.26 + 36 built-in modules; lock adds `ext.nunit`, `test-framework` (Editor infrastructure) | ✅ |
| Baseline commit | Task 0.2.7 report | `637f952` on `main`; tree clean | ✅ |

Minor observations: root `*.slnx` generated locally, correctly git-ignored (`.slnx` gitignore rule preserved), empty solution expected with no scripts; no test folder/assembly definitions/ADR folder yet — consistent with Phase-3 outputs.

### 28.3 Official Unity Web facts (accessed 2026-11-05 【Official-doc fact】)
| Topic | Fact |
|---|---|
| Unity heap | Contiguous WebAssembly `Memory` block; grows up to a configurable max (16 GB ceiling mentioned by docs), but growth may crash when a contiguous block fails; keep heap small; mobile browsers need manual Initial Memory Size tuning to typical usage |
| Asset data | Build emits `.data` → virtual file system → kept uncompressed in browser memory while running; pack content into AssetBundles to control download/unload; Data Caching (IndexedDB/Cache API) avoids re-downloads |
| Garbage collection | Web GC runs only between frames (cannot run mid-frame); per-frame high allocation can create quadratic pressure and OOM (e.g., string concatenation); recommended: `StringBuilder`, pre-allocated containers, `NativeArray<T>`, Burst/jobs for non-GC work |
| AssetBundles | Downloaded directly into the Unity heap (no extra browser allocation); unload explicitly to free memory |
| Data Caching | IndexedDB/Cache API; enabled in Build Profiles → Player Settings → Publishing |
| Graphics | WebGL2 baseline context in Unity 6; renderer features limited vs native; validate per environment |
| Audio | User-gesture unlock requirements and browser audio-context behavior documented |
| Multithreading | Works but requires COOP/COEP host headers for cross-origin isolation — build/deployment constraint, needs verification on Playgama's host |
| .NET API support | Documented subset; some BCL APIs (reflection/serialization) differ on Web |
| WebAssembly 2023 | Unity 6 default feature set; affects `Memory64` etc. |
| Debugging | Browser DevTools / debug-build paths documented |

### 28.4 Engineering assumptions (measured in Phases 4–6, never assumed) & mandated C# runtime patterns
- Actual upload sizes/shader cost measurable only by builds; test pipeline: iterate local WebGL builds in Chrome/Firefox on this workstation; measure load times, memory, FPS via Unity Profiler + browser DevTools.
- Disable real-time GI (cheap to test baked-only), limit dynamic lights, test settings shape (over-quality is the largest mobile-GPU risk).
- A concrete WebGL memory budget is decided in owner review (OQ-T08) — three-door measurement ritual: Empty build → Prototype build → Vertical-slice build, each recorded in the `PLAYGAMA_CONSTRAINTS.md` §5 evidence log.
- Mandated patterns 【Recommendation, enforced §40.7】: no per-frame allocations in hot loops; pre-allocated `List` churn; no per-frame string concatenation; audio pools; strictly pooled particle/VFX recycling.

### 28.5 OQ-T02 pipeline analysis (A/B complete 2026-11-10 — Built-in retained; D-032)
- **Options:** (A) stay Built-in; (B) install URP 17.x — official prebuilt SRP "optimized across a range of platforms, from mobile to high-end"; (C) HDRP — excluded by WebGL/web restrictions and mobile-target focus.
- **Evidence summary:** URP is the performance-oriented pipeline across mobile→desktop; Built-in is a smaller memory/engine footprint; URP adds pipeline asset/Global Settings and an extra package; URP on WebGL is supported since Unity 6; exact cost on the heap + D-006 quality (bloom, tonemapping, shadows) is measurable only by profiling.
- **Recommended direction (needs evidence first):** prove both in Phases 4–6 by profiling the *same* prototype scene in both configurations (install requires owner approval; reversal before content production is nil-cost). Preliminary engineering lean: **URP** for the D-006 quality bar, *conditional on* acceptable measured WebGL memory/FPS impact. **Owner makes the final call.** — *Completed 2026-11-10: the measured result below did not support the lean, so Built-in was retained.*
- **Test before approval:** same-scene A/B profiling (memory, GPU capture, load time, shader compilation, bloom/tonemap cost, mobile-GPU worst case); feeds the OQ-T08 budget table.
- **Measured result (2026-11-10, T-6.2):** both pipelines were built and run on the identical `Bootstrap → TestTrack` contract through the permanent builder, with URP 17.6.0 configured for equivalent directional-light/shadow settings (HDR off, MSAA 1, render scale 1, 50 m shadow distance, 1 cascade, SRP Batcher on). Built-in: **28.816 s** cold build, **35.64 MB** payload, WASM ArrayBuffer **96.0 MiB**, managed wasm memory **40.0 MB**, JS heap **9.95 MB**. URP: **123.687 s** cold / 3.409 s warm, **78.90 MB** payload (**+2.21×**), WASM ArrayBuffer **142.3 MiB** (**+1.48×**), managed wasm memory **82.0 MB** (**+2.05×**), JS heap **20.52 MB** (**+2.06×**). Both fit the recorded 300 MB / 2 GB limits. Decisive factors beyond size: URP needed every built-in material converted to `Universal Render Pipeline/Lit` (**65.23%** magenta on TestTrack before the fix, **0.00%** after), and with *equivalent* light settings the same scene/camera/capture path rendered **71.4% darker** (mean luminance **225.0 → 64.3**; course **0.00% → 20.89%** near-black). URP is therefore **not** visual parity without re-authored lighting/exposure, and it delivered no approved visual gain for the prototype. **Decision:** Built-in retained for the development prototype; URP deferred with a Roadmap-Phase-7 re-evaluation trigger (D-032 update, `PLAYGAMA_CONSTRAINTS.md` §5).

## 29. Architecture goals and 15-system responsibility design

### 29.1 Binding constraints 【Approved】

| Constraint | Source |
|---|---|
| Unity 6000.6.8f1, C#, Playgama Web (WebGL), single-player | D-008, D-009, D-012 |
| Simcade driving; skill over speed; skill-preserving upgrades | D-002, D-011, GDD §3.1/§12 |
| Commercial V1.0 scope: 10 vehicles (4 classes D-014 planning), ≥3 championships (D-029), 5 environments (Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena), high-quality realistic-inspired 3D (D-006), Arabic(RTL)+English(LTR) (D-013), Bridge integration | D-028 (+ D-010/D-014/D-029/D-031) |
| 8-type event planning basis with per-championship mix (not all 8 per championship) | D-031 【Provisional as planning】 |
| Development Prototype → Vertical Slice → Commercial V1.0 scope model; prototype allowances never define the product | D-030 |
| Product exclusions (multiplayer, PvP, open world, story, AI traffic, marketplace, real-money) | D-020 |
| No ads during active driving; ads only via Bridge; Bridge-only analytics | D-021/D-022/D-027 |
| Profiling-gate rule for every performance-sensitive feature; no invented numeric budgets | §20, D-006 note |
| All UI strings localized, never hard-coded; language persists | §12, GDD §25 |
| Escape never critical to flow; popups closeable; responsive landscape layouts | GDD §27, platform constraints |

### 29.2 Design rules 【Recommendation】
1. **One authoritative home per concept** — no parallel stores of the same fact (per `TRACEABILITY.md` principle).
2. **Dependencies point downward only** across module layers; upward cross-layer calls happen only through interfaces defined in the lower layer's terms.
3. **No per-frame allocations in hot paths** (Web GC runs only between frames — §28.3): events for *state changes*, polled interfaces for *continuous data*.
4. **Testability through purity first**: transition rules, scoring math (when defined), progression/save logic written device-free and engine-light so EditMode tests can run without scenes.
5. **No manager/service for a one-line feature; no framework without demonstrated need** (`AI_AGENT_GUIDELINES.md` §3).

### 29.3 System decomposition (15 systems, 4 layers)
```text
┌───────────────────────────────────────────────────────────────────┐
│  L1  Bootstrap (composition root) • GameFlow (app state machine)  │
├───────────────────────────────────────────────────────────────────┤
│  L2  UI/Navigation (screens, HUD) │ Localization │ Audio          │
│  L2  Camera/Presentation (runtime, race-scoped)                   │
├───────────────────────────────────────────────────────────────────┤
│  L2  Vehicle (physics) │ Track & Event │ Race Rules │ Garage      │
├───────────────────────────────────────────────────────────────────┤
│  L3  Progression (unlocks, rewards, currency-state) │ Runtime State │
├───────────────────────────────────────────────────────────────────┤
│  L4  Data Definitions (SO templates) • Save • Platform Integration│
│      (SaveModel/ISaveStore/SaveService; thin adapters, Bridge later)│
└───────────────────────────────────────────────────────────────────┘
```
Reading rule: an upper system may reference a lower system; a lower system never references an upper one. Cross-layer notifications travel as events; continuous values as polled properties.

### 29.4 Responsibility matrix (summary)
| System | Owns | Must NOT own | Needed in prototype? |
|---|---|---|---|
| Bootstrap | Object graph construction, service lifetime | Game rules, UI | **Yes** (minimal) |
| GameFlow | App-level state machine + transition rules + commands | Screen visuals, race rules, save schema | **Yes** |
| Vehicle | Vehicle runtime physics/tuning consumption, telemetry | Scoring, roster unlock state | **Yes** (1 test vehicle, D-030) |
| Track & Event | Runtime track/world interactions (checkpoints, hazards, surfaces as *gameplay inputs*) | Scoring formulas, event definitions data | Yes (1 test environment, D-030) |
| Race Rules | Event-type judging, lap/clean-line rules, boundary conditions, result snapshot | Driving physics, UI, reward amounts | **Yes** (minimal Time-Trial-shaped judging) |
| Progression | Player-facing unlock/reward/upgrade-level state transitions | Save storage mechanics, event rules | No (**defer** to first championship/content phase; interface reserved) |
| Garage | Roster browsing, stat/upgrade presentation composition | Vehicle physics, progression mutation | No (defer Phase 8+) |
| UI/Navigation | Screen lifecycle, navigation stack, HUD binding | Game state truth, data mutation | Minimal HUD only |
| Data Definitions | Definition template assets; loading/indexing; pure lookups | Any runtime mutable state | Stub index only (pattern validation; values owner-gated) |
| Runtime State | Live session state containers | Persistence, scoring | Yes (tiny) |
| Save | SaveModel schema + versioning + SaveService orchestration + ISaveStore call site | Progression *rules*, currency definitions | No (defer; **interface reserved in Core**) |
| Audio | Playback/mix routing, engine/UI/music boundaries | Gameplay rules | Engine hum/mute only |
| Localization | Language state, string catalog lookups, RTL mirroring signals | Fonts (design/brand), UI layout | Language toggle + string-through from first UI |
| Camera/Presentation | Chase rig readability, camera collision, FOV/drift presentation hooks | Vehicle physics inputs, path logic | **Yes** (temporary default per OQ-01) |
| Platform Integration | Thin adapters: storage (future), Bridge SDK calls (Phase 13) | Gameplay decisions, business rules | **No** (Phase 13); adapter interfaces reserved |

### 29.5 Per-system details

**S1 — Bootstrap.** Responsibility: construct the service graph once, in dependency order; initiate first GameFlow transition; nothing else constructs services. Owns: composition root; service lifetimes (all singletons, resolved once). Must NOT own: rules, UI, data. Exposes `Initialize()` on a `StreamingAssets`-free fast path; handles passed by constructor/`Inject()` once, then discarded. Dependencies: constructs every system; referenced by no one afterward. No events; may log startup segment timings (profiling evidence). Testing: EditMode graph construction without a scene (proves no scene-dependency leak). Prototype: **Yes** from first editor play. 【Recommendation】 formal composition-root style; no DI container unless owner-approved (【Owner gate】).

**S2 — GameFlow.** Responsibility: single owner of application-level state (`Boot → MainMenu → … → Results`); validates/executes transitions; translates requests into commands; coordinates loading of scene/content with the loading subsystem candidate (P6 additive) via async calls only — it never loads content itself. Owns: `AppState` enum/state machine; transition rule table (data-driven — not per-screen code); race-session lifetime scoping; app-level pause/resume policy. Must NOT own: race scoring, progression data, save schema, screens, language setting. Public: `TryTransition(AppState)`, `Request(AppCommand)`, `CurrentState`, events `StateChanged(AppState)`, `TransitionRejected(AppState, reason)`. Dependencies: Data (environment/scene keys), Runtime State (handoff context). Referenced as observer by UI/Navigation; Audio hooks pause events; language changes flow via Localization's own event. Events in: commands (UI/Platform/overlay). Events out: listed + `RaceInterrupted` (browser-tab visibility loss bridged by platform layer later). Testing: pure EditMode tests of the transition table; PlayMode integration of menu→results→menu circuit. Prototype: **Yes** — states exist from day one even where screens don't; prototype reaches MainMenu → PreEvent → Race → Results; placeholder transitions marked `NotProduced` fail closed loudly in editor, cleanly on web (visible in dev build).

**S3 — Vehicle.** Responsibility: physics simulation of one vehicle instance per presentable car 【**Approved (D-059, 2026-11-16): the player is the single physics vehicle (WheelCollider, D-033). AI opponents are kinematic path-followers driven by the Stage-1 `TrackPath` generated from `TrackLayout` at 2.0 m spacing: no `WheelCollider`, no rigidbody solver, and no physics step per opponent. `AIDriver` integrates pose and exposes normalized commands through the `AIVehicleInput` adapter; `AIVisualRig` owns wheel/steer/roll/pitch presentation, forces realtime shadow casting off, and adds one blob shadow. `VehicleController` and the approved player balance are unchanged. See §§49.1–49.9.**】. Consumes tuning definition data + live inputs + track surface readings → outputs chassis state (pose, speed, drift, grounded) + telemetry for scoring (clean-line counters as bytes per Race Rules needs). Owns: per-vehicle physics components and per-wheel rig state (WheelCollider-first candidate, D-033); assist code hooks (undecided, OQ-02). Must NOT own: definitions (reads), scoring (provides inputs), HUD (pure data), track rules. Public: `VehicleRuntime` struct snapshot read; `SetInput(IVehicleInputPorts)`; `ResetToSpawn()`; event-driven `Crashed/BecameAirborne` only as an evidence-gated mitigation for D-033 slip-model quirks. Dependencies: Data (VehicleDefinition), Track & Event (surface/collision channels by layer — no track object refs), input abstraction interfaces (in Core; implementation per D-034). Testing: EditMode tuning-data validation only; PlayMode lap-simulation repeatability harness; driving feel remains a human D-023 playtest. Prototype: **Yes** (1 test vehicle, D-030).

**S4 — Track & Event.** Responsibility: runtime behavior of environment content needed by events: track surfaces (grip zones), checkpoints, hazards, boundaries, jump/obstacle setups, finish line; publishes collision/checkpoint signal stream (typed, allocation-free). Owns: track object runtime behaviors, layer/trigger configuration, track-key referencing. Must NOT own: race judging (reports events; Race Rules interprets), environment art, street/domain name data (definitions live in Data Definitions). Public: `TrackSession` handle per loaded environment (checkpoints array, start/finish handles); events `CheckpointPassed(index)`, `BoundaryHit(type)`, `SurfaceChanged(zone)`. Dependencies: Data (EnvironmentTheme, track config keys); Vehicle reacts via layer/collision setup only. Testing: PlayMode trigger correctness; checkpoint geometry is content → editor validation tooling deferred (【Owner gate】). Prototype: **Yes** (one test track for M3: closed loop with start/finish + gates, Time-Trial-shaped).

**S5 — Race Rules.** Responsibility: per-event judging per event type (initially Time-Trial-shaped lap timing + boundary rules; later the 8-type basis per D-031 【Provisional as planning; per-type formulas Open OQ-03】). Inputs: vehicle telemetry + track signal stream + event rules from EventDefinition; output: **RaceResult** snapshot (uncomputed values stay `0`/none until formulas approved — no invented numbers). Owns: per-race session rules instance (timer, lap counters, violation counters); race sub-state machine (`Countdown → Running → Finished`, §30). Must NOT own: rewards/unlocks computation, results UI, physics. Public: `RaceController` per session (`Start(previewData)`, `FinishRace(reason)`); `RaceResult` read-only struct + `RaceFinished(RaceResult)` event. Dependencies: Vehicle (telemetry), Track & Event (signals), Data (EventDefinition rules incl. per-type config placeholder), Runtime State (spawn context). Testing: **pure logic core extracted** (timer/checkpoint math + violation accounting) → EditMode tests with synthetic signal streams (highest-value test asset); PlayMode scripted-lap integration asserting the result snapshot. Prototype: **Yes (minimal)**.

**S6 — Progression.** Responsibility: apply race outcomes and unlocks to player-facing progression state: championship/event progress flags, stars (provisional model, GDD §20), unlock state, currency-like award bookkeeping 【all amounts Open OQ-17】, upgrade-level storage validated against UpgradeDefinition ranges. Owns: `PlayerProgress` aggregate — the single mutable store of progression facts at runtime. Must NOT own: save persistence (Save converts to SaveModel), event content, UI display of progress, ad reward rules. Public: `ApplyRaceResult(RaceResult)`, `ApplyManualReward(...)` (placeholder without consumer until OQ-17), `IsVehicleUnlocked(id)`, `GetChampionshipProgress(...)`; events `ProgressionUpdated(delta)`, `Unlocked(reward)`. Dependencies: Data (definition lookups), Save (pushes updated snapshot via SaveService). Testing: EditMode unit tests with synthetic definitions (highest coverage target); save round-trip integration. Prototype: **No** — prototype asserts direct vehicle access; the reserved interface location in Core prevents a later rewrite.

**S7 — Garage.** Responsibility: roster browsing context — browse 10-vehicle definitions, filter by class (D-014 planning), inspect stats/unlock state (read from Progression), stage a vehicle selection, host the Upgrades screen flow composition (upgrade levels preview/apply via Progression). Owns: garage-session UI/interaction state; the Upgrades screen's data-binding context. **Upgrades state ownership (audit A4 pin):** the `Upgrades` AppState remains a navigation state for now (its content is a Garage-adjacent flow); whether it stays distinct or folds into Garage presentation is deferred to the OQ-A03 spec pass — the architecture works either way because the state only mounts the binding context. Must NOT own: definitions (reads), progression mutations (requests via Progression), physics. Public: `GarageController` (select/buy-preview), events `VehicleSelected(vehicleId)`, `UpgradeRequested(...)`. Dependencies: Data, Progression, Localization (labels). Testing: EditMode selection/upgrades-binding tests; visuals are OQ-A03-gated. Prototype: **No** (Phase 8+; API surface reserved).

**S8 — UI / Navigation.** Responsibility: screen lifecycle (mount/unmount per AppState change), navigation stack for overlays (Pause, Settings entry points, reward popups), HUD binding (in-race continuous reads), RTL/LTR mirroring, safe-state presentation for ads (pause only outside active driving), retry/quit surfaces per GDD §6/§24. Owns: screen presentation components, overlay/back-stack, HUD display state — never the truth underneath; every displayed value is a projection of state owned elsewhere. Must NOT own: game state truth, progression mutation, save. Public: `IUIScreenRegistry` (mount/unmount), `NavigationStack` (push/pop with Escape-friendly close paths), HUD read interfaces. Dependencies: GameFlow (observer), Localization, Audio (UI feedback), Data (display strings), Runtime State (telemetry). Events: in `StateChanged` + overlay commands; out user intents surfaced **as commands only** (`AppCommand` to GameFlow; progression intents routed to the relevant service) — never direct mutations. Testing: navigation rules EditMode; every screen visual acceptance is OQ-A03/A02-gated (manual, not automated visual diffs). Prototype: minimal HUD only (speed + lap timer) 【Recommendation】. **HUD assembly placement (audit A4 pin, working recommendation pending Phase-4 owner authorization):** the minimal prototype HUD lives in the Core-owned UI-registry seam (presentation, not simulation — NOT a Gameplay adapter); when the `UI` assembly is created (first partial screens Phase 10–11), HUD code moves there unchanged because it only consumes read interfaces.

**S9 — Data Definitions.** Responsibility: own, index, and validate **template assets** (ScriptableObject-based per D-035): `VehicleDefinition`, `UpgradeDefinition`, `ChampionshipDefinition`, `EventDefinition`, `EnvironmentTheme`, reward-rule entries; lookup indexes and structural validation (reference integrity, language-field completeness, key uniqueness). Owns: SO assets; catalog/index; schema-version marker; loaded once into an in-memory index at Boot. Must NOT own: runtime mutable state of any kind (D-035), gameplay decisions, any numeric value **as a design statement** — numbers are owner-gated (OQ-06/07/14–19), never engineering defaults. Public: typed read-only lookups (`GetVehicle(id)`, index/dictionary accessors), validation routine callable from editor tooling (Phase 8+, gated). Dependencies: none of the runtime systems (leaf layer; Unity core only). Testing: EditMode schema tests. Prototype: index pattern + stub `VehicleDefinition` for the 1 test vehicle; no roster values.

**S10 — Runtime State.** Responsibility: hold *transient session* facts shared across one app or race session: the selected-vehicle context entering `PreEvent→Race`, current race-session handle, current environment/scene key, `AppErrorContext` of the last failure; avoids direct system-to-system references for session-scoped context. Owns: session-scope containers, cleared explicitly per lifecycle stage. **Mutation ownership (audit A4 pin):** GameFlow is the **sole writer** of `SelectionContext` (written exactly once per transition into PreEvent/race-scoped flows); Race Rules owns all race-session internals; every other system is a read-only consumer; no container is ever written from two systems. Must NOT own: progression facts, definitions, persistent settings. Public: strongly-typed context holders with clear reset points (`RaceSessionContext`, `SelectionContext`). Dependencies: nothing (structural pivot). Testing: integration flow tests. Prototype: **Yes, small**.

**S11 — Save.** Responsibility: serialize a versioned `SaveModel` snapshot (per D-036 layered design): SaveModel DTOs (progression, unlocked vehicles, upgrade levels, championship state, settings incl. language) ⇄ `ISaveStore` (provider Open OQ-09/T05) ⇄ `SaveService` orchestration (load at boot, autosave on progression events + session end, write-failure policy Open). Owns: SaveModel schema + versioning/migration skeleton; save cadence policy as a boundary owner 【Recommendation; trigger list needs owner validation under OQ-09 — candidates §39.4】. Must NOT own: *deciding* progression values (reads its snapshot), where data comes from, Bridge cloud specifics. Public: `SaveService.Load()/.Save(reason)`, `ISaveStore` (interface defined in Core) — `BridgeSaveAdapter` arrives **Phase 13 only** behind this interface (D-036). Testing: pure DTO round-trip EditMode + PlayMode write→read→compare; browser storage behavior tests per Phase-13 planning reality. Prototype: **No** — nothing to persist; ISaveStore reserved in Core.

**S12 — Audio.** Responsibility: playback + mixing for engine/surface/UI/music with pause/resume compliance and browser user-gesture startup 【Recommendation from §28.3; exact behavior validated at prototype】; no sound-content decisions (OQ-A11). Owns: audio mixers (playback layers), voice pools, silence/unlock state. Must NOT own: gameplay state inference — reacts to events only; no per-frame polling. Public: `IAudioRouter` (one-shot, engine tick by class/physics read interface, duck on pause). Dependencies: GameFlow (pause events), Vehicle (polled engine parameters), UI (feedback hooks). Testing: PlayMode pause-compliance/no-crash; content quality per playtests. Prototype: engine hum + UI click 【Recommendation】 to support feel playtests; full spec Phase 12 (OQ-A11).

**S13 — Localization.** Responsibility: language state (`ar`/`en`), string catalog lookup (all strings from catalog — §12 binding), RTL direction signal driven by language, language persistence handoff (writes through Save's settings snapshot — preference per OQ-09). Owns: current language + catalog index; direction flag. Must NOT own: fonts (brand/design assets, OQ-A01), per-screen string decisions (OQ-A03), layout numbers. Public: `ILocalizationCatalog` (`tr("key")`), `LanguageChanged(lang, isRTL)` event (UI mirrors on it). Dependencies: Data (language key table — owned here rather than inside screen code), Save (persist handoff). Testing: catalog completeness EditMode (en↔ar key parity). Prototype: language toggle from the first UI 【Recommendation】 so RTL correctness is never retrofitted (GDD §33.5). *Language-default fallback (audit A4 cross-ref): if no save exists, default-language choice is an owner Open (OQ-09-related); a prototype may hard-default to English for engineering configuration with no design meaning attached — recorded here as the single origin of that rule; see also §39.3.*

**S14 — Camera / Presentation.** Responsibility: readable chase rig (damped follow, look-ahead, drift framing), collision push-in (sphere-cast over track-collision layers only), FOV/drift presentation hooks consumed as parameters; vehicle-class values in a data-driven `CameraProfile` template (actual values prototype-tuned Phase 6). Owns: rig components + per-profile runtime values. Must NOT own: vehicle physics, gameplay events, HUD — presentation only (clean OQ-01/OQ-A06 surface). Public: none beyond scene-scoped behavior; debug free-cam behind `#if UNITY_EDITOR` 【Recommendation】. Dependencies: Vehicle (pose/state read), Track (collision layers), Data (profile template). Testing: PlayMode manual feel tests (D-023 gate); camera-collision PlayMode test for tunnel cases. Prototype: **Yes** — temporary default (OQ-01 explicitly requires one). Detailed rig design §38.

**S15 — Platform Integration.** Responsibility: the only layer speaking to external SDK/platform APIs. Today: nothing installed — reserved boundaries: `ISaveStore` adapter (Bridge cloud save, Phase 13), ad/pause-safe adapter (Phase 13), analytics adapter (Phase 13, D-022); this section defines *where* it sits, not its design. Owns: SDK calls, capability probing, internet/feature gating, ad safe-state orchestration calls into GameFlow/UI. Must NOT own: reward calculation, progression logic, event flow, gameplay. Public: adapter interfaces from the Core/Platform seam 【Recommendation: not designed beyond the Save interface now — §14 forbids gameplay-to-SDK dependency】. Dependencies: Core (interfaces) only. Testing: mocked adapter tests when they exist (Phase 13; OQ-P02 re-verify ritual first). Prototype: **No** — install nothing 【Owner gate Phase 13】.

## 30. GameFlow and state model

### 30.1 Separation of concerns 【Recommendation — three distinct models】
| Concern | Model | Owner | Why separate |
|---|---|---|---|
| Application lifecycle | `AppState` flat state machine (9 states, §30.2) | GameFlow | Whole-app transitions; one truth for screens to observe |
| Race state | small nested race sub-state (`Countdown/Running/Suspended/ReadyForResult`) | Race Rules (Race Session) | Race logic is content-driven; racing runs without app-level requests; pause does not kill the session |
| UI screen presentation | screen registry + overlay/back-stack mirror (Pause, Settings, rewards popup) | UI/Navigation | Overlays can exist *with* state; the stack handles overlay closing/back without moving `AppState` |
| UI overlay visuals | standard Unity uGUI/EventSystem — domain of UI spec (OQ-A03), not architectural | — | — |

Result: **one flat app FSM + one small nested race FSM + a UI stack.** A single mega-FSM would violate concern separation; a three-level hierarchy would be overengineering.

### 30.2 `AppState` valid transition map (screen names from GDD §24 inventory ONLY — no invented screens)
| # | State | Screen shown (GDD §24) | Entry from | Exit to (valid) |
|---|---|---|---|---|
| 0 | `Boot` | none (bootstrap; black/loading splash is presentation-only) | launch | `MainMenu` (after init) |
| 1 | `MainMenu` | Main Menu | Boot, EventSelection(back), Upgrades(back), Garage(back), Results(final) | `Garage`, `ChampionshipSelection`, `Settings` (overlay; state unchanged) |
| 2 | `Garage` | Garage | MainMenu, ChampionshipSelection (unlock/flow contexts per spec) | `MainMenu`, `Upgrades`, `ChampionshipSelection*`, `EventSelection*`, `PreEvent*` |
| 3 | `Upgrades` | Upgrades | Garage | `Garage` (back) |
| 4 | `ChampionshipSelection` | Championship selection | MainMenu, Garage | `EventSelection`, `MainMenu`, `Garage*` |
| 5 | `EventSelection` | Event Selection | ChampionshipSelection, Results(next) | `PreEvent`, `ChampionshipSelection`, `Garage*` |
| 6 | `PreEvent` | Pre-Event | EventSelection | `Race`, `EventSelection(back)`, `Garage*` |
| 7 | `Race` | In-Game HUD (+ Pause/Settings overlays while race world is alive but sim paused) | PreEvent (via async load), Results(retry) | `Results`, (suspended internally by `Pause` overlay) |
| 8 | `Results` | Results | Race(finished) | `EventSelection`, `Race(retry)`, `MainMenu`, plus the Rewards/Unlock presentation **overlay/flow** layered on Results (annotated overlay-flow placeholder awaiting its spec — not a distinct `AppState`; see OQ-A03/GDD §24.11) |

- `*` = context-conditional edges 【Recommendation — GDD does not hard-specify these navigation details; they belong with OQ-A03 screen specs and must be validated there. Where GDD is silent, these rows are proposal-level, not rules】.
- All 11 screens covered: HUD is the Race state's persistent layer (not a state); Settings is an overlay reachable from MainMenu (and from Pause inside race); Rewards/Unlock presentation is an overlay flow layered on Results/Garage/Championship moments (GDD §24.11 "across events/championships" — inherently a presentation, not a destination).
- **No world-loading pseudo states** in the FSM: async load happens inside transition execution with the FSM already moved to the target state, presentation showing a loading veil — avoids async-in-FSM dead zones 【Recommendation】.

### 30.3 Transition rules, ownership, rejection handling
- Ownership: the FSM rule table is GameFlow's single truth; screens read/receive events but never compute state.
- Rejected transitions (invalid from current state, or world not loadable): `TransitionRejected` event + `#if UNITY_EDITOR` dev-log warning; user-facing behavior = stay on current screen with a disabled/back action 【Recommendation: no crash path, no invented error screen — error presentation needs spec】.
- Commands arriving during loading: queued until completion; `Cancel` honored for async scene-loading (P6 additive) 【Recommendation — only for to-menu back-outs; in-race cancel does not exist (§30.6 step 9 / audit A4)】.

### 30.4 Pause/resume (ads-safe)
- **Pause arbiter (audit A4):** Race Rules' race-session sub-state is the **sole arbiter** of race suspension/freeze. GameFlow observes (via `PauseLevelChanged`) and coordinates app-state presentation; UI renders; Audio ducks. No system pauses the race directly — pause requests enter as commands and Race Rules decides.
- Race pause = BOTH engines (physics and rules) frozen and race sub-state `Suspended` (timer/rules frozen; chase camera & HUD stop updating), handled by the Race Rules sub-state + Audio duck + presentation veil.
- Resume: strict request path only from UI back into the race FSM (no automatic resume on focus — final user-facing decision pending spec).
- D-027 (ads never during driving): interstitials/rewarded only at non-driving points — menu/background states or explicit ad-break flow presentations; the platform layer asks UI to enter safe presentation *before* calling any ad SDK 【Recommendation; enforcement testing in §42】.

### 30.5 Race completion, results, and exactly-once progression handoff
1. Race Rules judges → builds `RaceResult` → emits `RaceFinished`. GameFlow moves race sub-state → `ReadyForResult` (UI frozen) and `AppState → Results`.
2. Results screen reads the result projection; player accept (retry/next/back) are commands to GameFlow, not UI-side navigation.
3. Progression handoff: `ApplyRaceResult` happens **once** — idempotency keyed by a session-unique race-instance ID in the result; the RaceCompletion path is the only progression trigger; the Progression service — not the Results UI — computes any unlock/reward side. 【Recommendation: locate `ApplyRaceResult` in the transition-into-Results step to guarantee exactly-once semantics even on retry.】 *(Formalization pending: when the RaceResult schema is designed (Phase 5), the race-instance ID becomes a mandatory first-class field so the exactly-once rule is EditMode-testable.)*
4. Back-to-menu always re-enters menu-safe state with session runtime state cleared. **Retry-context distinction (audit A2 correction):** (a) *in-race retry* and (b) *Results→Race(retry)* re-use the still-alive race scene where additive loading already loaded it — the race scene is **not** unloaded on the retry path (fast-retry honoring GDD §6's low-friction retry); (c) every *final exit* (to EventSelection/MainMenu, or loading another event/environment) destroys the race-scoped Runtime State and unloads scenes (§30.6 step 16). Final retry UX remains spec-gated — GDD marks the exact retry flow open; do not finalize.

### 30.6 Runtime lifecycle (16 steps)
1. **Launch** → editor/web player starts; Unity engine invokes the scene-less `Boot` composition (`RuntimeInitializeOnLoadMethod` under the WebGL build spawn) 【Recommendation; a dedicated bootstrap scene is the P6/D-037 candidate alternative — owner review decides; both plans keep the `Boot` app state identical】.
2. **Service graph construction** → Bootstrap S1 (order: Data indexes → Core services → Save/Localization/Audio routers → UI registry → Gameplay ready).
3. **Locale + settings hydration** → Localization applies the persisted language (if a Save exists — SaveService rehydrates); the no-save default-language fallback is §29 S13 (single origin; OQ-09-related Open).
4. **Definition index validation** → Data Definitions runs structural validation (fails closed: gameplay blocked with a clear dev-error if templates are missing — loud, not silent).
5. **Audio unlock wait** (web only) → Audio subscribes to the first-user-gesture unlock as browser compliance requires.
6. **`Boot → MainMenu`** → GameFlow transition completes after step 4/5 readiness (not a fixed timeout 【Recommendation】).
7. **Menu/garage/championship navigation** → UI serves GDD §24 screens, one per state (§30.2); GameFlow validates every request.
8. **Event preparation** (`PreEvent`) → Runtime State carries `SelectionContext` (vehicle id, event id, environment/theme key); assist/control confirmation values per spec OQ-A03 later.
9. **Race loading** → GameFlow schedules additive environment/event scene load (P6/D-037 candidate) with a loading veil. **Cancellation scope (audit A4, aligned with §30.3):** cancellation is offered only for menu back-outs from the loading veil; no in-race cancellation exists (an active race exits only via pause→retry/quit commands per Race Rules authority); any richer cancel UX waits for specs (OQ-A03). Loads ONLY additive content; core systems never reload.
10. **Race session create** → Race Rules constructs race instances per `EventDefinition` rules; Vehicle instances spawn from definitions; Camera rig + HUD mount.
11. **Active race** → race FSM runs; commands validated; camera/HUD/audio consume state; no `Find`/tag scans, no per-frame allocations.
12. **Pause/resume** → §30.4.
13. **Failure inside race** → race may restart the current race or exit to menu via GameFlow commands; no silent recovery 【Recommendation; final UX needs spec】.
14. **Completion** → Race Rules finalizes → `RaceFinished(RaceResult)` → GameFlow transitions to `Results` with exactly-once progression handoff (§30.5).
15. **Progression update** → Progression `ApplyRaceResult` (idempotent), progress delta published; reward/unlock presentation overlays flow (Data-driven reward definitions only).
16. **Next event/menu returns** → per §30.2 valid edges; every *final exit* from race/results destroys the race-scoped Runtime State and scenes unload additively (direct additive un-load discipline). **Exception (audit A2):** the retry edges (`Results→Race(retry)`, in-race retry) are not final exits — they re-use the still-alive race scene per §30.5.

## 31. UI navigation separation 【Recommendation】

- **Separate concerns:** state truth (GameFlow) — screen mounting (UI/Navigation) — overlays (stack) — presentation themes/fonts (UI spec OQ-A03).
- Every GDD §24 screen is bound to exactly one `AppState` (§30.2) or is an overlay flow. HUD gets a dedicated always-mounted-on-Race channel that stops updating in all non-Race states.
- Overlay stack rules: at most one blocking overlay; back/Escape always closeable (GDD §27 Escape-never-required rule); an overlay never transitions app state on its own — its actions go through commands.
- Mirroring: RTL/LTR is driven by Localization `LanguageChanged`; layout mirroring happens at the navigation-contained presentation level, not per-screen hacks (binding to per-screen spec OQ-A03 later; no invented scalar layout rules now).
- All screens require an approved reference and written spec before implementation — none built yet (GDD §24; `design/ui-ux/README.md` 【Approved discipline】).

## 32. Inter-system communication 【Recommendation — prototype recommendation finalized in §32.4】

### 32.1 Mechanism comparison
| Option | Pros | Cons | Verdict for Velocity Rush |
|---|---|---|---|
| Direct construction-time references | Fastest, clearest, IDE-trackable; WebGC-friendly (no alloc) | Coupling grows: every new caller edits construction; breaks modularity if overused **without** layering (§29.3) | **Use for downward/reference-only paths (intra-layer)** — the backbone of L1–L4 |
| C# events / interfaces | Decouples direction-inverting signals; interface hides device importance (input adapters per D-034) | Subscription lifecycle leaks; per-event delegate allocs; ordering hazards | **Use for bounded, low-frequency state-change notifications** where direction would invert layering |
| Central event bus | Maximum decoupling; single dispatch point | Hidden global; type-name/string drift; debugging difficulty; unbounded subscriber sprawl; alloc pressure if misused | **Justified ONLY for a small, typed, pan-module ring** (§32.2 closed set); NO string/type-erased global bus |
| Command-based messaging | Auditable single entry, decoupled UI intent | Extra indirection if applied everywhere | **Use exclusively for user-intent → GameFlow transitions** (validated, idempotent, queueable) |

### 32.2 Where an event ring IS justified (exhaustive, closed set)
Working name `GameEvents`, owned by Core, payload as `readonly struct` records. **Allocation characteristic (audit A3 correction — qualified, not unconditional):** dispatch itself does not allocate when payloads are structs passed to typed handlers (no boxing); however **subscription churn DOES allocate** — `+=` on a multi-handler delegate combines into a new delegate instance, closures allocate at registration, and `-=` rebuilds the delegate — so the required discipline is **subscribe once at Bootstrap, unsubscribe exactly once at teardown, never subscribe per frame/race**. This is static analysis of documented C# delegate behavior, **not a WebGL measurement**; it must be confirmed by profiler sampling at the first build (OQ-T08/P8 evidence ritual). Closed set:
1. `StateChanged(AppState)` — the sole way everyone learns navigation.
2. `LanguageChanged(lang, isRTL)`.
3. `ProgressionUpdated(ProgressionDelta)` / `Unlocked(UnlockPayload)` — followers: UI, Audio, Platform (analytics hook).
4. `RaceFinished(RaceResult)` — Results/Progression/analytics handoff.
5. `PauseLevelChanged(...)` — audio + UI safe-state compliance.
Topics beyond this list need a documented reason in a DECISIONS/ADR entry first — prevents bus sprawl (risk R-A2, §45.2).
**Ring lifecycle rules (audit F-D2):** (1) idempotent unsubscribe guaranteed at teardown (no callbacks into destroyed objects); (2) copy-on-dispatch or an explicit prohibition of subscribe/unsubscribe during dispatch (no mid-dispatch list mutation); (3) a dev-mode duplicate-subscription assert; (4) subscription order documented as non-guaranteed.

### 32.3 Where an event ring is NOT justified
- Per-frame telemetry (vehicle pose, speed, HUD numbers): direct polled interface reads (alloc-free).
- Asset/definition loading or any pull/value-return flows: plain method calls.
- Inside one module (vehicle ↔ its own wheels/telemetry): plain fields/methods.
- UI→progression display binding: projection interface per screen, not a bus topic.

### 32.4 Prototype-simplicity recommendation (integration review R-3 response)
**Recommended prototype model: direct calls + interfaces at seams + plain C# events per emitter.** No typed ring, no central bus in the prototype. Concretely: `StateChanged` as a plain C# event on GameFlow; `LanguageChanged` on Localization; `RaceFinished` on Race Rules; `ProgressionUpdated/Unlocked` added when Progression lands (Phase 10–11); `PauseLevelChanged` on Race Rules. No cross-layer topic discovery; no global static; consumers subscribe via injected emitter references at Bootstrap. (§32.2's closed set stands as the upper bound of future ring topics.)

**Ring re-introduction requires all three evidence pieces (integration review §6.3):** (1) ≥3 distinct layers demonstrably listen to the same topic and duplicate subscription setup is recurring observed friction (from play-tested code, not prediction); (2) measured subscription-lifecycle defect rate in the prototype that a single choke point would actually reduce; (3) profiling evidence (P8 log) that dispatch ordering/batching matters for a WebGL budget.

## 33. Assembly boundaries 【Recommendation — working candidate; final names Phase-4 owner-gated】

### 33.1 Reference matrix (candidate set; do not auto-create all)
| Candidate (names illustrative) | Include now? | Responsibility | May reference | Must NOT reference | Why | When |
|---|---|---|---|---|---|---|
| `VelocityRush.Core` | **Yes** | App lifecycle/FSM, command validation, event-ring types, cross-cutting interfaces (`IVehicleInput`, `ISaveStore`, `ILocalizationCatalog`, `IAudioRouter`), Runtime State containers, assert helpers | Engine only | `Gameplay`, `UI`, `Platform` | Testability of pure logic; cycle protection | Phase 4 |
| `VelocityRush.Data` | **Yes** | SO definitions, indexes, schema validation | Engine + `Core` (key types) | `Gameplay`, `UI` | Modular data authoring; keeps templates pristine (D-035) | Phase 4 |
| `VelocityRush.Gameplay` | **Yes** | Vehicle, Track runtime, Race Rules (state + scoring core), race-session composition | `Core`, `Data` | `UI`, `EditorTooling` | Largest test-pure winnings; keeps vehicle code UI-agnostic | Phase 4 (vehicle stubs Phase 5) |
| `VelocityRush.UI` | Later | Screens, HUD, navigation stack, overlay presentation | `Core`, `Data`, `Gameplay` (read interfaces only) | — | Avoid UI dragging gameplay into one assembly; premature before specs (OQ-A03) | Phase 10–11; minimal HUD code lives in the Core UI-registry seam until then (§29 S8) |
| `VelocityRush.Platform` | **No** | Adapter implementations (save/store, ads, analytics) | `Core` only | everything internal | SDK isolation per §14 | Phase 13 |
| `VelocityRush.Tests.EditMode` (first) + `VelocityRush.Tests.PlayMode` | **Yes (EditMode first)** | EditMode unit tests (Core logic, Data schema, save DTO, race-rule math) | all production assemblies (test-runner allowed) | — | Regression protection per AI guidelines §5; supports OQ-T07 investigation while formally OPEN | Phase 4 EditMode; PlayMode tests with M3 vehicle |
| editor-only tooling assembly | Later | Authoring validation tools for definition sets, trigger-geometry checks | Full | Runtime code kept separate, editor-guarded | Prevents editor-only helpers leaking into WebGL builds | Phase 7–8, 【Owner gate】 for tooling scope |

### 33.2 Dependency diagram (target shape)
```text
            ┌─────────────────────────────┐
            │  UI (later)                 │
            │  Platform (later, Phase 13) │
            ├── refs ──▶ Core ◀──constructs── Bootstrap (inside Core)
            │               │   ▲
            │ refs          │   │ implemented by
            │               ▼   │
            │     Gameplay ─▶ Data
            │       │         (leaf)
            └── Tests (EditMode/PlayMode) reference all
```
**Circular-dependency checklist (asmdef compiler enforces; violations fail the build, not review):** UI must not be referenced by Gameplay nor Platform; Platform depends only on Core; Data never references Gameplay (authoring-order discipline); Progression→Save is a single-direction push; UI↔Gameplay resolved by read-only interfaces owned by Gameplay consumed via Core seams.
Notes (audit F-E1/E2): `Data → Core(key types)` is a deliberate, unusual inversion edge — assets carry IDs whose types live in Core to avoid data→gameplay inversion; no defect; flagged so nobody "simplifies" it into a cycle. Assembly names are illustrative; owner pins final names in the Phase-4 conventions task.

**Implementation status (Part 4.1, owner-directed, 2026-11-05):** the "Yes now" set was created as files — `Assets/Scripts/Core/VelocityRush.Core.asmdef` (engine-only, no refs), `Assets/Scripts/Data/VelocityRush.Data.asmdef` (refs Core — the inversion edge above preserved), `Assets/Scripts/Gameplay/VelocityRush.Gameplay.asmdef` (refs Core + Data), and `Assets/Tests/EditMode/VelocityRush.Tests.EditMode.asmdef` (Editor platform only, `overrideReferences` + `nunit.framework.dll`, refs test-runner assemblies + all three production assemblies, `autoReferenced: false`). No C# scripts, scenes, or prefabs were created (no placeholder content); empty assemblies legitimately compile without scripts. The "Later"/"No" assemblies (UI, Platform, PlayMode tests, editor tooling) were deliberately NOT created. Skeleton asset folders created empty: `Assets/{Scenes,Prefabs,Art,Audio,Materials,Data,UI,Settings}`. Validated with the installed Unity 6000.6.8f1 (237b6593792b) in batch mode: import exit code 0, zero compilation errors, all four asmdefs imported by `AssemblyDefinitionImporter`; `Packages/` and `ProjectSettings/` untouched. **Naming update (2026-11-05, owner acceptance in the Part 4.2 assignment):** the root namespace `VelocityRush` (Core code in `VelocityRush.Core`) and the four assembly names above were accepted and are recorded as D-043 (APPROVED-INITIAL); renaming later remains cheap (see that entry). The "Later"/"No" assemblies stay excluded.

## 34. Data definitions and runtime-state ownership 【Recommendation; containers per P4/D-035; D-039/040/042 drafts §44】

### 34.1 Definition inventory & relationships (conceptual; no values, no assets)
```text
ChampionshipDefinition (≥3) ──ordered event list──▶ EventDefinition (8-type basis, D-031)
EventDefinition ──track key + rules config──▶ EventRulesConfig (typed payload; per-type scoring fields, values OQ-03 Open)
VehicleDefinition (×10, OQ-14 identities) ──class──▶ VehicleClass (Starter/Sport/Drift/Supercar, D-014)
UpgradeDefinition (×6, D-011) ──curve points → UpgradeModifiers──▶ VehicleDefinition (runtime composition)
EnvironmentTheme (×5, D-028) ──scene key──▶ SceneDescriptor (P6-candidate)
RewardRule entries (OQ-17) ──awards──▶ applied by Progression at runtime
```
Justified additions 【Recommendation, mirroring existing GDD requirements only】: `EventRulesConfig` as a typed payload *within* EventDefinition (per-type rule placeholders, schema-only in V1 — formulas open OQ-03); `SceneDescriptor` mapping definitions→scene keys so scene names are never hard-coded (§30.6 discipline). `CameraProfile`, `VehiclePhysicsProfile`, `AudioProfile` are documented in their owning sections (§38, §36.6, §29 S12) under the same SO pattern per D-035.
**Prohibition carried forward:** data values (car stats, reward points, thresholds) remain owner-gated (OQ-06/OQ-07/OQ-14…OQ-19) — proposed containers hold no numbers.

### 34.2 Data ownership rules
| Rule | Statement |
|---|---|
| Ownership | All definition assets owned by Data Definitions (Data assembly; no runtime writer exists — D-035) |
| References between assets | Editor-authored **asset references** (SO-to-SO links + IDs for runtime-safe lookups); no brittle GUID/string references for intra-project assets |
| Runtime instantiation | Definitions read **as-is** into runtime structs/values at load time (never mutated; never written back); consumers get indexed lookup handles (§29 S9) |
| Missing/invalid definition | **Fail-closed** (§30.6 step 4): validation at Boot index; gameplay blocked with dev diagnostics naming the defect; release-build behavior per §41; presentation wording spec-gated (OQ-A03) — never silently patched |
| Validation rules (schema) | Required-fields completeness; language fields (`en`/`ar`) present for every display string (D-013 binding); key uniqueness; reward-rule reference integrity; scene-key existence cross-check (against a generated key list, not filenames at runtime) |
| Data versioning | Each asset family carries `schemaVersion`; migrations applied **in editor authoring**, never at runtime; the runtime rejects unknown versions loudly instead of guessing |
| Editor authoring responsibilities | Authoring validation tooling (schema checker) is Phase 7–8 owner-gated editor tooling (§33.1 last row) |
| Testing | EditMode schema-integrity tests over fixture samples; runtime index lookups via EditMode tests with stand-in data |

### 34.3 Runtime-state single-mutator matrix (no duplicated sources of truth)
| State entity | Sole mutator | Written when | Readers | Persisted via |
|---|---|---|---|---|
| `RuntimeVehicleState` | Vehicle physics (per race instance) | physics steps | Camera/HUD/Audio/Race Rules | never (session-scoped) |
| Current race sub-state | Race Rules (race session) | race lifecycle | GameFlow/UI/Save | via RaceResult only |
| `RaceResult` | Race Rules (once, then readonly) | finish | Progression/Results | consequences only (below) |
| `PlayerProgress` | **Progression only** | ApplyRaceResult / manual reward (OQ-17 future) | Garage/Championship/Results/HUD | SaveService → SaveModel |
| Garage state | Garage (UI context only) | garage interactions | Garage UI | no |
| Upgrade state (levels) | Progression (applies upgrade purchase) | Upgrade screen actions → ProgressionService | Vehicle (via UpgradeModifiers) | SaveModel |
| Championship progress | Progression | ApplyRaceResult | ChampionshipSelection UI | SaveModel |
| Unlock state | Progression | ApplyRaceResult / rewards | Garage/race gate | SaveModel |
| Language/settings state | Localization (language) / Settings UI (writes via the Settings persistence path into `SaveModel.settings`) | change events | UI mirroring | SaveModel.settings |
| `SelectionContext` | GameFlow (§29 S10 pin — written exactly once per transition into PreEvent/race-scoped flows); **Phase-8 debug exception:** the Garage is the temporary writer until Phase 11 | transitions | PreEvent/Race readers | no (session) |

Binding guarantee: **any given fact has exactly one mutable home**; Save never mutates meanings, only snapshots. Runtime State containers remain session-scoped.

## 35. Scene and runtime lifecycle architecture 【Recommendation — satisfies OQ-T06 candidates; nothing created】

### 35.1 Two candidate structures (compared; P6/D-037 remains provisional)
**Option S-A — Persistent root + additive race content (recommended working candidate):**
```text
Persistent root scene (loads once at launch)
  └─ Boot composition: service graph + GameFlow
  └─ All menu screens as UI panels (Main Menu, Championship/Event Selection,
     Garage, Upgrades, Pre-Event, Settings, Results)
Race scene (additive per event — P6 candidate)
  └─ Environment content (one of 5 themes)
  └─ Race systems + Vehicle + Camera rig
```
**Option S-B — Screen-per-scene navigation: every GDD screen gets its own Unity scene.** Pros: matches a "scene = page" mental model; simpler state-machine mapping. Cons: 8+ UI scenes serialize/deserialize repeatedly; loading-veil spam between menu screens; breaks fast-retry continuity; asset duplication; heavier memory churn on web. Verdict 【Recommendation】: rejected — GDD screens are UI panels, not worlds; screens are navigation *within* a persistent UI layer.
Comparison: S-A keeps one lightweight context swap per race and satisfies GDD §6 low-friction-retry; menu navigation becomes pure state change with zero scene-reload cost. Whether the pattern is safe long-term comes only from vertical-slice validation (OQ-T06 stays OPEN with load-speed/memory criteria). Persistent root + additive environment scene is the closest match to GDD §24's UI-centric screen set and to Web memory discipline (official Unity guidance: pack content into loadable/unloadable units; keep the always-resident blob small — §28.3).
*Superseded discovery sketch:* the discovery's early scene-set idea (Boot / MainMenu / Garage / Race_<Environment> / Results scenes) was refined by this design: menu screens live in the persistent root as UI panels (no per-screen scenes); `Boot` exists as app state §30.2, not a mandatory separate scene.

### 35.2 Scene ownership & conventions (candidate level; nothing created)
| Scene | Owner system | Persistence | Loaded when | Unloaded when |
|---|---|---|---|---|
| Persistent root (Boot+UI) | Bootstrap/GameFlow | whole app lifetime | launch | never (contains the composition root) |
| Race scene (per event/environment composition) | GameFlow coordinates load; Race Rules/Vehicle/Camera own contents | per race session | `PreEvent→Race` transition (async) | final race exit (§30.6 step 16) — **not** on retry (§30.5 A2 rule) |
| Optional per-theme environment-variant split (later) | Track & Event | same | with Race scene | same |

Naming/serialization conventions (exact scene names, folder placement, text-serialization settings) are Phase-4 owner-gated deliverables (PROJECT_STRUCTURE deferral note) — not invented here.

### 35.3 Persistent services & scene lifecycle
- The composition root lives in the persistent root; **no per-scene service setup** — race scenes contain only content-bound behaviors (Vehicle rig instances, Track triggers, Camera rig), never service construction (prevents duplicate-manager defects; risk R-A4 family).
- Unity `DontDestroyOnLoad` is **not** the base approach 【Recommendation】: the persistent-root scene keeps additivity clean and avoids root-object migration bugs (DDOL objects re-parent out of scene coordination; makes additive-unload accounting harder); confirm feasibility in the Phase-4 spike.
- Boot composition 【Recommendation】: the game launches directly into the persistent root scene; the composition root's `Awake` constructs services once; no separate Boot scene needed (a distinct visual splash is presentation only if the UI spec asks).

**Implementation status (Part 4.2, owner-directed, 2026-11-05):** the persistent root scene exists as `Assets/Scenes/Bootstrap.unity` and is enabled at Build Settings index 0. Its single root GameObject `Bootstrap` carries `VelocityRush.Core.Bootstrapper` (S1 composition root): it runs the ordered `ApplicationStartup` step sequence exactly once (instance-level duplicate-initialization guard; duplicate step names rejected at construction), fails closed on the first failing step (`StartupException` names the step; later steps never run), and emits startup segment timings as profiling evidence (`StartupReport`). Uses `DontDestroyOnLoad` nowhere — the persistent root stays loaded per §35.3. The only current step is `environment-snapshot` (boot-time environment identity logging); later systems attach ordered steps here rather than building parallel boot paths. No service construction exists yet (Data indexes, Save/Localization/Audio routers and the GameFlow transition arrive in later authorized tasks, §30.6 step order preserved). Validation evidence: Unity batch-mode import + scene validation (scene opened, exactly one Bootstrapper, zero missing scripts), 7/7 EditMode tests passing (ordering, fail-closed, duplicate-name rejection, exactly-once entry point), final clean compile 0 errors / 0 warnings for the shipped scripts.

### 35.4 Scene transitions & loading screens
- Transitions validated by GameFlow (§30.2), executed as async additive loads (`SceneManager.LoadSceneAsync` now; Addressables candidate deferred — its package requires owner approval, Phase-4 gate).
- Loading veil presentation: owned by UI/Navigation (blocking overlay during any transition that loads). Capabilities recorded (visuals per OQ-A03): progress readout; cancel affordance limited to menu back-outs (§30.6 step 9).
- Same-scene transitions (navigation inside persistent UI) perform no scene load at all — instant swap.

### 35.5 Loading cancellation & fast retry
- **Cancellation:** only for menu back-outs initiated from the loading veil, per §30.3/§30.6; cancelled additive loads trigger the `Resources.UnloadUnusedAssets` discipline to drop partially-resident content. Capability boundary (integration review R-4): no guarantee of instant cancel responsiveness (cancellation honors the async boundary); richer UX (double-back semantics, partial progress display) belongs to the OQ-A03 spec pass — not invented.
- **Fast Retry (GDD §6 minimal-friction; §30.5):** `Results→Race(retry)` and in-race retry do **not** touch scene loading — the race scene is still resident; retry runs a race-state reset (positions, timers, telemetry, respawn).
- Transition-speed expectations (load times in target browsers) are recorded as OQ-T06 validation evidence at vertical slice — no invented numbers.
- **T5.4 prototype implementation note (2026-11-08, development prototype — actual-project validated):** `TestTrack` is a straight road, so the first race loop is deliberately a **one-way timed run**, not a lap. `RaceRules` (Gameplay) owns `Countdown → Running → Finished`, monotonic `Stopwatch` timing, ordered checkpoint acceptance, and one immutable `RaceResult` per attempt; `RaceController` adapts scene triggers and the existing retry intent. Retry resets state/progress/timer/result guards and the vehicle's captured spawn pose plus linear/angular velocity in the resident scene, with no `SceneManager` call. Checkpoint/finish gates are simple trigger colliders on the existing road; no UI, HUD, rewards, progression, or save exists. Validation status: actual-project Unity 6000.6.8f1 batch import/compile passed with 0 compiler errors and 0 warnings; the full suites passed (129/129 EditMode, 4/4 PlayMode, zero failures); all `TestTrack` script references resolve; retry was verified to reuse the same scene instance and vehicle without a reload. This is prototype evidence only and does not close any open decision or question.

### 35.6 Scene cleanup & memory lifecycle
- Race exit unloads the additive race scene, then triggers an explicit `Resources.UnloadUnusedAssets` cycle 【Recommendation】 (official Web-memory guidance: unload explicitly; bundles land inside the Unity heap).
- Asset-delivery model for race content: AssetBundles as the delivery candidate (official docs; §28.3) — exact tooling selection (bundles vs direct-packed scenes) is a Phase-4 owner-gated ADR.
- WebGL memory boundary reminder: the built `.data` stays resident while running; keep static content lean; per-environment content lives behind the loadable boundary.

### 35.7 Failed loading recovery
| Failure | Behavior |
|---|---|
| Additive race scene fails to load (missing key, corrupt content) | cancel, unload partial state, return to the prior safe state; user-facing presentation wording spec-gated (OQ-A03) |
| Persistent-root content issue (boot-time corruption definition invalid) | fail-closed per §34.2 — blocked at startup with dev diagnostics |

### 35.8 WebGL scene-loading considerations 【Official-doc facts; no invented numbers】
- Data Caching (IndexedDB/Cache API) avoids re-download across sessions — candidate enabled in Player Settings **with owner approval** (a ProjectSettings change, owner-gated).
- Decompression/Brotli/GZip hosting support and decompression fallback are deployment/host concerns (§40.3 + re-verification for the Playgama host, OQ-P02).
- COOP/COEP multithreading, if ever enabled, is host-config territory — not planned for gameplay; recorded as a discovery fact.

## 36. Vehicle physics technical design (Simcade)

> Status: does **not** finalize OQ-T03/D-033 — WheelCollider remains the provisional candidate (P2/D-033), hybrid fallback documented contingency; no vehicle controller is implemented or authorized. Every numeric is a tuning surface; acceptance comes only from the Phase-6 D-023 playtest gate + WebGL profiling.

### 36.1 Driving-feel objectives & constraints
Objectives 【Approved, GDD §7/D-002】: responsive controls; believable accel/braking; convincing cornering; controlled drifting; per-class/vehicle distinct handling; readable road feedback; consistent camera; skill-based mastery ("easy to learn, hard to master"); upgrades change effort, never skill-irrelevance (skill-preservation binding).
Constraints: (1) WebGL single-threaded-first budget discipline (frame-bound GC, §28.3) — per-frame allocation is a defect class; (2) **one code path, ten vehicles** — differentiation via data; (3) no simulation realism creep contradicting Simcade identity (full tire-model sim, clutch/heat systems are out of V1 without an owner decision per D-002 rule); (4) feel is not decided here — every dynamic parameter is a tuning surface gated by D-023 playtest + OQ-T02/T03 evidence.

### 36.2 Architecture overview
```text
IVehicleInput (Core abstraction) ─┐
VehiclePhysicsProfile (Data SO template) ─┤─▶ VehicleController (one code path per raced vehicle, Gameplay, FixedUpdate)
UpgradeModifiers (computed from levels) ─┘        │→ WheelRigService (4 per vehicle)
        CTL → StabilityAssistLayer (evidence-gated, default OFF) ─┤→ RuntimeVehicleState
        CTL → TelemetryProvider ─▶ polled by Camera/HUD/Audio/Race Rules; rule-threshold events → Race Rules
```
- `VehicleController` (name illustrative; no code authored): consumes profile × modifiers, drives physics in `FixedUpdate`, publishes read-only telemetry.
- **No second simulation path** for different vehicle classes — classes differ only through profile data + assist-layer flags 【Recommendation】 (satisfies §9's 10-vehicles-without-10-controllers requirement, D-028 roster).
- Vehicle never calls UI/Progression/Save; it reads Track surface data by physics layers and emits events.

### 36.3 WheelCollider candidate evaluation 【Provisional — P2/D-033】
| Aspect | Assessment |
|---|---|
| What it is | Unity's official vehicle foundation (Vehicles module, already in manifest): raycast suspension with spring/damper (`sprungMass`, `targetPosition`), slip-based friction model (`forwardFriction`/`sidewaysFriction` curves), `motorTorque`/`brakeTorque`/`steerAngle` drives, per-wheel `ConfigureVehicleSubsteps`, `GetWorldPose` for visuals |
| Advantages | Officially supported and documented; fast live inspector iteration; tunable Simcade range via per-wheel curve sets; upgrades map cleanly onto force/curve parameters; avoids hand-rolling suspension math |
| Risks | Not a real tire model — arcade feel may need code shaping (roll-torque control, anti-flip, assisted grip) — known community pain points at handling extremes; fixed-update + substep cost on WebGL must be measured; suspension-jitter corner cases |
| Evidence | Official WheelCollider API docs (Unity 6000.6, accessed 2026-11-05) — behavior facts only; WebGL cost **unmeasured** |
| Verdict | Preferred initial candidate per D-033; final selection gated on D-023 playtest + WebGL profiling + owner decision |

### 36.4 Hybrid custom-force fallback (documented fallback only; not chosen, not implemented)
Rigidbody-with-custom-tire-forces (own suspension integration + lateral/longitudinal force curves): keeps rigidbody chassis stability while replacing WheelCollider friction/suspension with a fully controllable model. Trigger candidates (at least one must fire): (a) D-023 feel gate unreachable with realistic effort after documented tuning iterations; (b) measured WebGL cost of enabled substeps beyond the evidence-based frame budget (OQ-T08); (c) systemic jitter/instability that assist shaping cannot resolve. Cost: more code, own skid/flip handling, bespoke surface integration. (Discovery also recorded: fully kinematic/no-rigidbody arcade physics — rejected as primary for Simcade; third-party paid physics assets — excluded by scope rules.)

### 36.5 Rigidbody responsibilities (whatever wheel model wins)
| Responsibility 【Recommendation】 | Policy (values deferred) |
|---|---|
| Center of mass | Set deliberately per vehicle (low/central stability bias); a profile parameter, value owner-gated — never the default |
| Interpolation | Enabled (render smoothness between fixed steps) |
| Collision detection | Discrete initially; continuous modes evaluated only by evidence |
| Sleep | Disabled for raced vehicles |
| Mass | From profile (values OQ-06/OQ-14 — not invented) |
| Velocity ownership | Physics only; no external transform teleporting except the respawn path |

### 36.6 Behavior domains (design responsibilities; every numeric = tuning surface)
| Domain | Design responsibilities | Telemetry surface | Gates |
|---|---|---|---|
| Wheel & tire | Per-wheel contact wiring; forward/side friction curve sets per surface; visual pose sync (`GetWorldPose`) | wheel slip, grounded flag, surface zone id | Surface multipliers arrive from Track layer data |
| Engine torque & power | Throttle → torque via profile curve shaping; torque capped by profile | current power output | curve ownership OQ-06/OQ-14; simplified response, no engine sim |
| Transmission | Torque-shaping taps (auto-only V1 feel); no manual shifting without owner decision | gear state (HUD/audio) | GDD promises no manual transmission |
| Braking & handbrake | Brake torque distribution front/rear per profile; handbrake as strong rear-bias lock supporting drift (Drift-class bias) | applied braking per wheel | handbrake supports, never replaces, skill (D-011 spirit) |
| Steering response & speed sensitivity | Input multiplier filtered by a speed-dependent sensitivity curve + response shaping; filtering lives under the input abstraction (device-agnostic) | effective steer angle | curve shapes are Phase-6 tuning (OQ-02) |
| Grip, slip, drift | Drift = intentional state with laterally reduced grip under player control (handbrake/throttle-bias), recomposable on demand; slip feeds scoring (OQ-03 placeholders) | drift state, lateral slip | drift depth never final without Phase 6; entry assists evidence-gated |
| Suspension & ground contact | Spring/damper/`targetPosition` per profile; staggered per-class; primary readable-road-feedback channel | compression, contact normal | bottoming-out policy deferred |
| Airborne & landing | Limited air control (minimal pitch/yaw damping — not steering-in-air); landing impulse physics-natural; `Landed/BecameAirborne` feed Race Rules + audio/camera | airborne flag, landing impulse | GDD §8 jump/landing approved direction |
| Collision & obstacle | Rigidbody contacts against gameplay collision layers only; `ObstacleHit(type, impulse)` output; anti-stuck no-progress detection emits an event — reset policy belongs to Race Rules | contact events, stuck flag | no penetration hacks; surfaces are Track's world |
| Stability & recovery | Optional assist layer: roll-torque shaping at extremes, mild angular-velocity damping, auto-flip recovery surfaced to Race Rules (never silent teleports). All assists data-flagged, default OFF 【D-033 evidence gate】 | assist flags | assists smooth, never drive (skill identity) |

### 36.7 Vehicle configuration data (P4/D-035 consistent)
`VehiclePhysicsProfile` (ScriptableObject **template**, Data assembly; no values authored by this design): rigidbody params (mass, CoM offset); engine curve taps + torque cap; brake bias + handbrake bias; steer range + speed-sensitivity curve; friction curve sets (fwd/side + per-surface multiplier refs); suspension (spring/damper/target/travel); air damping + landing params; assist flags + strengths (default OFF); audio class id + camera profile ref.
- Per-vehicle override layer: 10 vehicles = 10 profile assets inheriting per-class defaults (Starter/Sport/Drift/Supercar per D-014 planning; roles from GDD §10).
- **Upgrades integration:** the upgrade system computes `UpgradeModifiers` (delta multipliers per D-011 category) from progression levels + `UpgradeDefinition` curve points; the controller composes profile × modifiers. Upgrade effects alter handling within skill-preserving bounds — modifiers apply small caps; formula values OQ-07 Open.
- Runtime separation (D-035): profiles never mutated; modifiers are plain C# values rebuilt when upgrade levels change.

#### 36.7.1 Phase-8 Stage-1 data contract (implemented 2026-11-11)

- **`VehicleClass`** (`Assets/Scripts/Data/VehicleClass.cs`): `Starter = 0`, `Sport = 1`, `Drift = 2`, `Supercar = 3`. Values are serialized by index; append rather than reorder.
- **`VehicleDefinition`** (`Assets/Scripts/Data/VehicleDefinition.cs`): read-only roster template with `CurrentSchemaVersion = 1`; `vehicleId` (stable lowercase ASCII id, letter start, single hyphens, ≤40 chars); `displayNameEn`/`displayNameAr` (non-empty, trimmed, ≤80 chars, no control characters); `vehicleClass`; and a **reference** to `VehiclePhysicsProfile`. The definition never duplicates handling values, so physics has exactly one authored home. Visual-rig and upgrade bindings are deferred to a later schema bump (Stage 3 / Phase 11).
- **Validation:** `TryValidate` fails loudly on unknown schema versions, malformed ids, missing/whitespace-only localized names, undefined classes, null profiles, and invalid profiles. `VehicleRosterRules` (Unity-independent) validates the complete set: exactly 10 entries, 2/3/2/3 distribution, case-insensitive unique ids, non-null entries, and all per-entry rules; it reports every issue at once.
- **Authoring tool:** `VehicleRosterBuilder` (`Assets/Editor/VehicleRosterBuilder.cs`) provides the menu `Tools/GameCraft Studio/Velocity Rush/Validate Vehicle Roster`, the batch entry `ValidateFromCommandLine` (exit `1` on any defect), and `CreateDefinition(...)`, which validates before writing and creates `Assets/Data/Vehicles/<Class>/<vehicleId>/<vehicleId>_Definition.asset`. It creates only owner-supplied identities; it never invents names, classes, or statistics.
- **Status:** schema and tooling are implemented and tested (43 new EditMode tests; suite **284/284**, compile **0 errors / 0 warnings**). No `VehicleDefinition` or vehicle-profile asset exists yet: `T-8.0` (OQ-14/OQ-06 owner freeze) gates `T-8.3`/`T-8.4`, and an empty roster intentionally fails validation.

#### 36.7.2 Phase-8 Stage-2 generated roster (implemented 2026-11-11)

`VehicleRosterStage2Builder` (`Assets/Editor/VehicleRosterStage2Builder.cs`) authors the ten frozen OQ-14 identities. Each profile is an instantiated clone of an approved prototype baseline with only the listed serialized fields overridden; each definition then references its profile. Menus: `Generate Phase 8 Vehicle Roster`; batch: `-executeMethod VelocityRush.EditorTools.VehicleRosterStage2Builder.GenerateFromCommandLine`.

| Vehicle (id) | Class | Baseline | Mass (kg) | Fwd/side grip | Torque (N·m) | Brake / handbrake | Steering | Brake bias F | Spring / damper |
|---|---|---|---|---|---|---|---|---|---|
| Apex-V (`starter-apex-v`) | Starter | Grip | 1050 | 1.85 / 1.90 | 850 | 2600 / 3200 | 33° | 0.55 | 30000 / 4000 |
| Nova Hatch (`starter-nova-hatch`) | Starter | Grip | 1100 | 1.80 / 1.85 | 900 | 2700 / 3000 | 32° | 0.62 | 32000 / 4200 |
| Velocity GT (`sport-velocity-gt`) | Sport | Grip | 1200 | 1.90 / 1.95 | 1000 | 2500 / 3600 | 31° | 0.55 | 35000 / 4500 |
| Aero Coupe (`sport-aero-coupe`) | Sport | Grip | 1300 | 1.88 / 1.93 | 1100 | 2700 / 3400 | 30° | 0.54 | 38000 / 4800 |
| Pulse R (`sport-pulse-r`) | Sport | Grip | 1120 | 1.92 / 1.97 | 1150 | 2600 / 3600 | 33° | 0.56 | 34000 / 4400 |
| Sidewinder (`drift-sidewinder`) | Drift | Loose | 1200 | 1.35 / 1.10 | 1100 | 2500 / 5200 | 33° | 0.40 | 35000 / 4500 |
| Kuma DX (`drift-kuma-dx`) | Drift | Loose | 1350 | 1.40 / 0.95 | 1250 | 2600 / 5600 | 34° | 0.38 | 33000 / 4300 |
| Vortex V10 (`supercar-vortex-v10`) | Supercar | Grip | 1150 | 2.15 / 2.25 | 1500 | 3200 / 4200 | 29° | 0.58 | 45000 / 5500 |
| Eclipse (`supercar-eclipse`) | Supercar | Grip | 1180 | 2.20 / 2.30 | 1450 | 3600 / 4000 | 28° | 0.60 | 50000 / 6000 |
| Nebula X (`supercar-nebula-x`) | Supercar | Grip | 1280 | 2.10 / 2.20 | 1550 | 3300 / 4200 | 30° | 0.57 | 46000 / 5600 |

- **Exact baselines:** `sport-velocity-gt` equals `CandidateA_Grip_Vehicle` and `drift-sidewinder` equals `CandidateB_Loose_Vehicle` across every serialized field and both curves; `VehicleRosterAssetTests` asserts this over the real assets.
- **Curves:** `engineTorqueCurve` is linear from `1.0` at 0 m/s to the row's endpoint (0.55–0.72 at 60 m/s); `steeringSpeedSensitivity` is linear from `1.0` to 0.42–0.68. Both stay within the validator's 0..1 range.
- **Derivation, not invention:** the owner supplied qualitative briefs; the numeric values above are engineering choices inside the approved envelopes. They are **not** final balance and remain OQ-06/T-8.5.
- **Drivetrain limit:** `VehiclePhysicsProfile` has no layout field. "FWD-feel" (Nova Hatch: front brake bias 0.62, low CoM) and "AWD-feel" (Nebula X: high mass, steep early torque endpoint 0.72, heavier high-speed steering) are approximations only.
- **Status:** roster validation **PASS** (10 definitions, 2/3/2/3); batch compile 0 errors / 0 warnings; EditMode **288/288**; PlayMode **13/13**. Visual rigs, Garage, and upgrades are out of scope for this stage.

#### 36.7.3 Phase-8 Stage-3 visual rigs and LOD0 budgets (implemented 2026-11-11)

`VehicleRigBuilder` (`Assets/Editor/VehicleRigBuilder.cs`) produces one shared physics rig and ten data-driven visual variants. It never edits `PrototypeVehicle.prefab`.

- **Base rig:** `Assets/Prefabs/Vehicles/VehicleRigBase.prefab` is a copy of the validated prototype rig — `Rigidbody`, one chassis `BoxCollider` on `ChassisBody` (1.5 × 0.6 × 3.6 primitive with a unit box), four `WheelCollider`s, four visual wheel transforms, and the `VehicleController` wiring.
- **Variants:** `Assets/Prefabs/Vehicles/<Class>/<id>_Rig.prefab` are real prefab variants (`m_SourcePrefab` set). Each binds its own `VehiclePhysicsProfile`, serializes that profile's wheel radius/mass/suspension/friction values, and adds `KitPart_*` primitive body parts whose primitive colliders are destroyed immediately.
- **Collider parity:** every variant contains exactly **one `BoxCollider` + four `WheelCollider`s**; no kit part has a collider. `VehicleRigPrefabTests` asserts this over the real assets, together with the canonical chassis bounds and profile/wheel serialization.
- **Class silhouettes (D-051):** Starter uses a tall hatch cabin with `RearHatch` and short nose; Sport uses `LongHood`, fastback, splitter and wing; Drift uses `BigWing`, wide fenders and front lip; Supercar uses `WedgeNose`, side blades, diffuser and rear wing. Each class has a named signature part asserted by test.
- **Provenance (D-050):** eight materials under `Assets/Materials/Vehicles/` use the Built-in `Standard` shader with flat colours. No texture, external mesh, package, or per-vehicle script is used. LOD0 only; LOD1 is deferred, not silently omitted.

**Measured LOD0 budget (real prefab assets, 2026-11-11):**

| Class | Vehicles | Renderers | Material slots | Unique materials | Triangles | Textures | Draw calls (batched / worst case) |
|---|---|---|---|---|---|---|---|
| Starter | Apex-V, Nova Hatch | 15 | 15 | 6 | 452 | 0 | ≤6 / 15 |
| Sport | Velocity GT, Aero Coupe, Pulse R | 16 | 16 | 6 | 464 | 0 | ≤6 / 16 |
| Drift | Sidewinder, Kuma DX | 17 | 17 | 6 | 476 | 0 | ≤6 / 17 |
| Supercar | Vortex V10, Eclipse, Nebula X | 18 | 18 | 6 | 488 | 0 | ≤6 / 18 |

The batched figure assumes Built-in dynamic batching groups the small primitive meshes by shared material; the worst case counts one draw call per material slot. Both are far below the candidate 500-draw-call OQ-T08 ceiling, and even ten unbatched vehicles would total only 150–180 draw calls. Triangles are per vehicle, not per scene; a race with one player vehicle adds under 0.5k triangles.

#### 36.7.4 Phase-8 T-8.5 deterministic balance harness (implemented 2026-11-11)

`VehicleBalancePlayModeTests` (`Assets/Tests/PlayMode/VehicleBalancePlayModeTests.cs`) is the executable balance gate.

- **Setup:** loads the real `HandlingCourse` scene through `PrototypeSceneLoader`, resolves the scene's `VehicleRosterCatalog`, clones the canonical prototype rig, and disables race, camera, HUD, lip-sync/presentation, and audio systems. A hidden 600 × 600 m plane at y = −200 gives a flat, bounded surface independent of course geometry.
- **Drive script:** every profile gets an identical sequence — neutral settle (1 s), full throttle straight (1.5 s), full throttle with 0.7 steering (2.5 s), neutral settle (10 fixed steps). The harness uses `WaitForFixedUpdate` with `Time.timeScale = 2`, and applies the profile through `VehicleController.TryApplyProfile` and a scripted `IVehicleInput` through `TrySetInputSource`.
- **Assertions:** no profile may end on its side or roof (final chassis `up.y > 0.2`), no profile may invert below `up.y = −0.5`, every profile must travel more than 8 m (no permanent stall), Drift average turn slip must exceed Sport, and Supercar average top speed must exceed Starter.
- **Measured run (committed evidence):**

| Vehicle | Class | Distance (m) | Top speed (m/s) | Avg turn slip | Final up.y |
|---|---|---|---|---|---|
| drift-kuma-dx | Drift | 22.3 | 11.5 | 0.964 | 1.00 |
| drift-sidewinder | Drift | 19.9 | 11.0 | 0.958 | 1.00 |
| sport-aero-coupe | Sport | 19.7 | 13.1 | 0.511 | 1.00 |
| sport-pulse-r | Sport | 19.5 | 14.2 | 0.408 | 1.00 |
| sport-velocity-gt | Sport | 18.4 | 12.7 | 0.495 | 1.00 |
| starter-apex-v | Starter | 16.8 | 12.0 | 0.441 | 1.00 |
| starter-nova-hatch | Starter | 17.8 | 12.3 | 0.433 | 1.00 |
| supercar-eclipse | Supercar | 24.8 | 17.7 | 0.373 | 1.00 |
| supercar-nebula-x | Supercar | 21.9 | 16.1 | 0.909 | 1.00 |
| supercar-vortex-v10 | Supercar | 22.0 | 16.7 | 0.918 | 1.00 |

- **Interpretation:** Drift slip (mean ≈ 0.961) is clearly separated from Sport (mean ≈ 0.471), and Supercar speed (mean ≈ 16.8 m/s) from Starter (mean ≈ 12.2 m/s), so the class envelopes are measurably distinct. The harness measures stability and class separation; it is **not** a feel verdict and does not replace the owner playtest or final numeric sign-off (OQ-06).
- **Limits:** the manoeuvre is a short scripted stability/variance probe on a flat surface, not a full lap of `HandlingCourse`, so it cannot prove lap-time parity or course-specific balance. Those remain Stage-5/owner evidence.

### 36.8 Runtime tuning boundaries
- Tunable live: all profile fields (EditMode inspector + Play Mode tweak); assist strengths; input filtering curves.
- Not runtime-mutable: definition templates (frozen at runtime per D-035); anything owner-gated (assist depth, drift philosophy).
- Tuning loop (Phase 6): adjust profile → editor playtest lap → record D-023 observation notes → repeat; notes recorded, values decided **by the owner** after playtest evidence.

### 36.9 Update lifecycle
```text
per frame:   IVehicleInput sampled request (throttle/brake/steer/handbrake)
FixedUpdate: apply forces (torque/brake/steer per profile×modifiers)
FixedUpdate: update RuntimeVehicleState (pose/velocity/slip/grounded/drift)
FixedUpdate: rule-threshold events → Race Rules (stuck/obstacle/airborne/landed)
per frame:   Camera/HUD/Audio read RuntimeVehicleState (read-only, polled, no per-frame allocs)
```
Physics step: default fixed timestep initially; whether 0.02 s (default) or altered values fits the WebGL budget is a measured decision (OQ-T08). Substep budget: `ConfigureVehicleSubsteps` per rig — decided by profiling (prototype wheel count = 4; commercial simultaneous physics = player vehicle(s), no AI before OQ-T09).

### 36.10 Debugging & telemetry
- Dev-build overlay (editor & web debug builds only): per-wheel contact/slip renderers, CoM gizmo, suspension-travel bars, speed/velocity vectors. Debug tooling authoring is owner-gated (Phase 4+ deliverable under explicit authorization).
- `TelemetryProvider` exposes a read-only struct snapshot (speed, accel, drift state, grounded, per-wheel slip, stuck flag, obstacle contact) — single source for camera look-ahead, HUD, engine sound, Race Rules scoring inputs. No per-frame event emission (polled interface, per §32.3).

### 36.11 Validation strategy (implemented layers)
| Layer | Test type | Content |
|---|---|---|
| Profile/schema integrity | EditMode | schema test: non-zero travel, curve sanity, no null surfaces — catches authoring errors |
| Pure math (steer filter, upgrade compose) | EditMode | extracted static functions with synthetic inputs — deterministic, scene-less |
| Repeatability/stability | PlayMode | `VehicleBalancePlayModeTests` drives all 10 profiles through one identical scripted manoeuvre on a controlled surface; asserts no flips, no permanent stalls, and class telemetry separation (determinism/instability, not feel). Implemented T-8.5 (§36.7.4) |
| Feel | Manual D-023 playtest | human validation per milestone M4; recordings + owner feedback; authoritative, not automatable |
| WebGL perf | Profiler ritual | wheel-substep/frame cost at OQ-T07/T08 evidence steps |

### 36.12 WebGL performance risks / fallback / sequencing
| Risk | Treatment |
|---|---|
| Substep cost scaling | Player vehicle race-scoped activation (only player physics in V1); measured substep profiles; timestep decided by evidence |
| GC from friction/telemetry strings (debug only) | zero-allocation fields; debug strings editor-only |
| Suspension jitter at low visual update | interpolation on; jitter policy a data param |
| Engine-curve allocation churn per upgrade frame | pre-allocate per-vehicle numeric arrays once at load |
Known limitations: WheelCollider literature corner cases (high-speed flips, extreme compression); recorded mitigations = roll-torque shaping, spring pre-tension, assist damping, per-class curve sets; full fallback triggers in §36.4; damage/consequences beyond approved direction stay open (GDD §8).
Proposed prototype implementation sequence (Phase 4/5, owner-gated suggestion): ① physics skeleton forces (steer straight, placeholder profile) → ② wheel contact + suspension walk-through on flat test surface → ③ speed-sensitivity + input filtering → ④ drift plumbing (handbrake + lateral bias) → ⑤ jump/obstacle/anti-stuck events to Race Rules → ⑥ profiles for the one test vehicle to D-023 readiness → ⑦ repeatability harness + first profiling numbers at OQ-T07/T08. Deferred production features: persistent damage/condition (GDD §8, owner decision); anti-cheat telemetry (single-player; OQ-T09).

## 37. Input architecture

> Status: does not finalize OQ-T04/D-034 or install any package; no final control layouts, sensitivity, dead zones, or steering curves are defined (all tuning/spec territory — OQ-A03/OQ-02).

### 37.1 Ownership & abstraction
| Element | Owner | Notes |
|---|---|---|
| `IVehicleInput` (driving surface: throttle/brake/steer/handbrake samples) | Interface defined in Core; implemented by an input adapter | Vehicle consumes the interface only — never a device API (§8) |
| `IAppInput` (pause/quit/retry intents) | Same seam; emits **AppCommands** to GameFlow (§30.3 command model) | UI-agnostic |
| UI input (menus, buttons, popups) | UI/Navigation via standard uGUI EventSystem | Never routed through `IVehicleInput`; `IAppInput` carries only high-level intents |
| Implementation code (package or legacy bridges) | Input adapter (location owner-gated at Phase 4) | Two candidate implementations behind the same seam (§37.5) |

Rationale: device code stays out of gameplay code (§8 binding) and the package decision stays swappable (D-034).

### 37.2 Device coverage
- **Keyboard (desktop web):** steering (left/right), throttle, brake, handbrake, pause, camera intent (§37.4). Key *bindings* are recommendations pending UI spec — prototype suggestion: standard racing cluster (directional/WASD; space handbrake; dedicated pause; Escape reserved for overlay-close only, **never a critical flow dependency** — GDD §27). Simultaneous-press semantics use the same response shaping as touch (device-agnostic).
- **Touch (mobile browser — product target per platform constraints 13/14/16/22):** on-screen driving controls (layout deferred to OQ-A03): steering via left/right zones or a steering surface; throttle/brake zones; handbrake button; pause button always visible during Race. Accessibility baseline: color never the only status signal; no long-press-required gestures; landscape-only responsive layout. Multi-touch: independent simultaneous touches for steering+throttle 【Recommendation】; actual browser touch behavior validated in the prototype (OQ-T04). No text entry during driving.
- **Mouse:** no mouse steering for V1 (keyboard + touch cover both device classes); mouse remains for UI/cursor contexts and optional prototype debug look.
- **Gamepad:** not planned for V1 (Keyboard+Touch product target) — a candidate future device only if the owner requests; scope unchanged, not reduced.

### 37.3 Action lifecycle & state reset (single owner: GameFlow → input adapter)
| Event | Adapter behavior |
|---|---|
| Any `AppState` transition | **Full driving-input reset** — clear all pressed/held values, cancel input buffering (prevents stuck-input defects) |
| Pause enter | stop driving-input polling; enumerate UI intents only |
| Loading | ignore driving inputs (veil non-interactive) |
| Retry (scene reuse) | fresh reset before lap start |
| Browser focus loss (tab blur) | neutral-input frame + Race-Rules pause request (§30.4) |
| Ad interruption (Phase 13) | zero driving input during ads (D-027); restored only via explicit resume |

### 37.4 Action definitions (no final values invented)
| Action | Surface | Implementation instruction |
|---|---|---|
| Steering | signed −1..+1 per frame | adapter reports the *raw* normalized sample; speed-sensitivity/response shaping lives in the vehicle domain (§36.6) |
| Throttle / Brake | 0..1 digital (analog where device reports it) | digital ramp shaping = tuning surface (OQ-02) |
| Handbrake | boolean | routes to the drift-support path (§36.6) |
| Pause | discrete intent | converted to AppCommand via `IAppInput`, never a direct state change |
| Camera intent | discrete (reset/look toggle) — presentation-only | camera rig consumes; never mutates gameplay |

Map switching is event-driven from `StateChanged` (Race map vs UI map). Rebindable controls belong to the Settings screen production (OQ-A03) — prototype ships fixed experimental bindings, zero remap surface, no promised feature depth.

### 37.5 Input System package vs Legacy Input (D-034 【Provisional】 preserved)
| Dimension | Input System package (`com.unity.inputsystem`) | Legacy Input Manager |
|---|---|---|
| Delivery | Event-driven, action maps, buffered input | Polling per frame |
| Multi-device/mapping | Native, incl. touch | Manual ad-hoc |
| Alignment | Preferred direction per D-034 | Currently active baseline (no package) |
| WebGL caveats | touch/mouse handling profiles per docs; must validate | Decades of web behavior |
| Constraint | **Install requires explicit owner approval** (manifest change) | Available today |
| Ownership cost | Event lifecycle discipline; adapter | Fewer moving parts; weaker UI integration |

【Recommendation (unchanged, owner-gated):】 implement the adapter behind `IVehicleInput`/`IAppInput`; when Phase 4/5 needs the package, request installation explicitly with GO/NO-GO criteria (§44).

### 37.6 Testing & WebGL risks (planned; nothing exists)
Tests: EditMode adapter mapping + reset semantics (zeroed on every major state change); PlayMode fake-adapter drives a full lap deterministically; manual browser keyboard/mouse + representative mobile touch (device matrix owner choice, OQ-P01); input-path profiler check (no per-frame alloc). Risks: (1) touch latency/multi-touch quirks in target browsers — measure, don't assume; (2) browser context stealing (long-press context menu, focus loss) — suppression per platform-constraints guidance; fragile behaviors go on the OQ-P02 re-verification list; (3) keyboard layout/locale variance (Arabic layouts must not break driving) — normalized positional mapping recommended, final choice pending OQ-A03; (4) reserved browser keys — Escape overlay-close only, arrow-key scroll suppressed, browser context menu unbound; (5) pointer-lock not needed for driving; recorded as a caveat for any future look feature.

### 37.7 Prototype vs production scope
| Prototype (Phase 4–6) | Production (Phase 10–12+) |
|---|---|
| Keyboard + touch driving minimum viable; fixed default bindings | Full control layouts per approved specs; Settings rebinding |
| Interfaces + legacy-bridge adapter | Package decision realized (if owner selects) under the same seam; gamepad if owner requests |
| Pause intent + camera-look debug | Camera action polish, accessibility remap |

## 38. Camera and presentation architecture

> Status: capability design, not feel; OQ-01/OQ-A06 leave the final perspective open (temporary default allowed for the prototype); no distances/FOV/damping/offset values are invented.

### 38.1 Rig ownership
Owner: Camera/Presentation system (§29 S14) — one `VehicleCameraRig` per race instance, created/torn down with race scope; owns **no gameplay state** — read-only consumer of Vehicle telemetry. Racing cameras are gameplay-external by design: replaceable without touching Vehicle (future cockpit view = rig swap).

### 38.2 Chase rig pipeline (capability)
```text
chassis anchor (per-profile marker)
  → desired follow position (anchor + speed-scaled look-ahead offset)
  → critically damped spring / smooth-damp (position + rotation)
  → sphere-cast collision check (pivot → desired, track collision layers only)
  → final position/rotation → FOV ramp + optional drift yaw damping
```
- Follow target: a per-vehicle profiled marker transform (not vehicle root — root rotation destabilizes follow).
- Speed-based look-ahead: offset/distance scale with the speed vector 【capability; all magnitudes = profile tuning surfaces OQ-01/OQ-A06】.
- Drift response: reduced yaw damping while drift is active so the vehicle visibly slides in frame (capability in rig; per-class via profile).
- Manual-look offsets: optional, prototype-debug oriented; no committed player feature.

### 38.3 Damping, collision, state transitions
| Responsibility | Approach 【Recommendation】 |
|---|---|
| Damping | critically damped spring constants per profile; separate position/rotation stiffness; freeze-when-paused (smoothing halts, no oscillate-back) |
| Collision avoidance | sphere-cast from anchor toward the desired position; layer mask = track/environment collision layers only (no gameplay-layer coupling); push-in with profile minimum distance |
| Race start | rig spawns at profile spawn offset, aimed toward the drive path |
| Pause | freeze smoothing |
| Retry | keep rig; reset to spawn orientation |
| Results | same rig frozen shot (parked orbit is a future option, not built); transition veil covers swap |
| Camera-collision edge cases | fallback min-distance clamp (never snaps inside geometry) — tunable |

### 38.4 Camera profiles & performance
`CameraProfile` (ScriptableObject template, Data-pattern per D-035): offset, distance ranges, damping constants, look-ahead factors, FOV ramp shape, drift-yaw damping factor, collision radii/min distance, per-class defaults (2/3/2/3 composition per D-014 planning). **All numeric fields are placeholders to be filled via Phase-6 tuning — this design invents none.** 1 profile per class minimum (4 files), extendable per-vehicle via override assets — no code forks.
Performance: single active camera; no render textures in the prototype; post-processing adoption is gated by the pipeline decision (OQ-T02/D-032) and is a profiling-evidence surface (P8) — nothing adopted by default here. Camera math uses no per-frame allocations (struct reads + damped motion math; frame-bound GC rule). Camera follows only the player (no AI spectator logic before OQ-T09).
Debugging: editor/debug-build overlays (rig path lines, predicted vs actual follow position, look-ahead vector, collision hit point) — never shipped enabled in release; free-cam tooling owner-gated (Phase 4+).
Validation: profile schema test (valid ranges; per-class defaults exist) EditMode; scripted camera-collision test in a dummy tunnel + manual; smoothness = **human D-023 playtest** (not a unit test); included in frame profiling at P8 steps.
Future options (none approved): cockpit/hood cam — candidate only, owner decision with its own profile data; photo/orbit mode — **not planned** (outside GDD V1 scope per D-020 exclusions); recorded to prevent surprise scope creep.

## 39. Save architecture (P5/D-036 preserved; provider undecided OQ-09/T05)

### 39.1 Layering & boundaries
Layered design (binding structure): `SaveModel` (plain serializable DTOs, versioned with `saveVersion`) ⇄ `ISaveStore` (persistence abstraction defined in Core) ⇄ `SaveService` (orchestration: load at boot, autosave on progression events + session end, failure policy) — `BridgeSaveAdapter : ISaveStore` arrives **only at Phase 13** (D-036 strict), inheriting atomic semantics or degrading gracefully per a real Bridge capability probe; the OQ-P02 re-verification ritual precedes any coding; no Bridge API call exists anywhere else (§14).
- **In scope:** player profile; unlocked vehicles; upgrade levels per vehicle; championship progress & completion; best results/stars (provisional communication model, GDD §20 — thresholds OQ-04); currency/recognition metrics 【shape only, values OQ-17】; settings (language, audio volumes, control-preference placeholders); `saveVersion`.
- **Out of scope:** any definition/asset content (re-derived from Data Definitions every boot); race telemetry/logs; analytics.

### 39.2 Serialization (candidates; no final choice)
| Candidate | Pros | Cons |
|---|---|---|
| Unity `JsonUtility` | zero dependencies, tiny, fast | schema-evolution friction, no polymorphism |
| Newtonsoft Json (package) | flexible, mature, migration-friendly | package install → **owner approval**; adds size |
| custom bit-packed writer | smallest payload | highest code burden, brittle |
【Recommendation】 start JsonUtility; select the final format at Phase-11/13 implementation, owner-gated. Serialization owner: SaveService only; DTOs are plain serializable classes — no Unity-Object refs, no GUIDs/asset pointers in save payloads (IDs only).

### 39.3 Versioning & migration
`saveVersion` integer at root; unknown-future version → refuse + fresh-defaults path (never guess-forward); backward deserialization via staged per-version converters added only when a save format actually changes (V1 launch saves version 1); migration registry is a pure-function chain, EditMode-testable with synthetic payloads. First-run defaults (incl. the default-language rule — single origin §29 S13) are owner-gated OQ-09.

### 39.4 Save/load failure & recovery (policies pending OQ-09 owner approval where marked)
| Scenario | Detection | Immediate behavior | Recovery |
|---|---|---|---|
| Write fails (quota/private block) | ISaveStore result code | hold current in-memory state; one retry; surface warning presentation if repeated (wording spec-gated) | next autosave; storage diagnostics recorded in evidence |
| Load returns empty (first run) | version 0/absent | fresh defaults 【values/spec owner-gated OQ-09】 | normal first-run |
| Load corrupt (parse fail / bad version) | validation | discard payload; try a backup copy if one exists 【Recommendation: SaveService keeps previous-good copy rotation — pending OQ-09】; else fresh defaults + corruption log | continue with defaults; never fabricates progress |
| Valid-but-invalid (schema mismatch after migration) | migration failure | treat as corrupt path | same as corrupt |

### 39.5 Autosave trigger candidates (recommendation pending OQ-09)
1. After each `ProgressionUpdated`/`Unlocked` event (progression mutation boundary); 2. after Settings change (immediate); 3. after upgrade application; 4. on Results accepted (end-of-race boundary, coalesced with #1); 5. browser shutdown (`pagehide`/`visibilitychange`-derived attempt — **must be validated on actual target browsers; not globally guaranteed**). No timer-based autosave during driving (D-027: no interruptions mid-drive).

### 39.6 Atomicity & duplicate-write protection
- Single-writer rule: SaveService is the only SaveModel writer (§34.3); concurrent write requests coalesce into a queue (latest wins) — no concurrent double writes.
- Atomic swap 【Recommendation, provider-permitting】: write to a temp key/blob first, then swap/rename to the live key via `ISaveStore` atomic semantics; provider capabilities determine the actual guarantee (Phase-13 adapter inherits the interface contract). In-flight mid-write failure leaves the live key unchanged; temp cleaned on next boot.
- Exactly-once result processing lives at the Progression boundary (§30.5); Save serializes the *post-state*.

### 39.7 Progression-applied-but-unsaved window (bounded, accepted-risk note)
Window between `PlayerProgress` mutation and the coalesced SaveService write completing; bounded by the autosave cadence — worst case one race-completion delta. Mitigation candidates (none final): the trigger list above; Bridge cloud save later adds a second recovery surface (Phase 13, OQ-T05). Explicitly **not** a data-loss guarantee — a browser tab kill can still lose ≤ one race's delta; recorded as an accepted-risk note pending OQ-09.

### 39.8 Save lifecycle & WebGL persistence constraints (facts; provider undecided)
| Moment | Save behavior |
|---|---|
| Race scene load/unload | no save involvement (session only) |
| Results accept / progression update | autosave via trigger candidates |
| app-quit / tab hide (web) | attempt final save on hide/`pagehide` 【browser-compatibility caveat】 |
| ad interruption (Phase 13) | no forced save during ads; resume path revalidates |

Provider candidates (never quoted as guarantees): browser `localStorage` (small, synchronous, quota-limited per browser — exact limits diverge by vendor and are measured), IndexedDB (async, larger, eviction varies), Bridge cloud save (Phase 13, evaluated with real Bridge docs after the OQ-P02 ritual). Private/incognito modes commonly constrain storage → failure recovery (§39.4) is mandatory, not optional. Data Caching (build caching) is a *different* mechanism from save persistence — never conflated (§28.3).

## 40. WebGL performance and build strategy

> Status: no performance budget number invented; no browser/device guarantee invented; **no WebGL build has been performed** — the batch-mode Editor launches prove environment capability only, not a successful web build. Thresholds are evidence-gated (P8/D-006 note; OQ-T08 OPEN). Evidence classes: 【Verified env fact】 = verified on this workstation; 【Official-doc fact】 = Unity 6.6 manual (2026-11-05); 【Expectation】/【Proposal】 = planning positions; 【Unverified — validate】 = pending measurement.

### 40.1–40.3 Build architecture & pipeline
- 【Official-doc fact】 A web build = `index.html` + `.data` (virtual file system payload) + `.wasm` (engine runtime) + loader JS; the `.data` payload remains resident (uncompressed) in browser memory while running → pack content into loadable/unloadable units (AssetBundles) and keep the always-resident blob lean (§28.3).
- 【Verified env fact】 Unity 6000.6.8f1 with WebGL Build Support installed and identity-confirmed (Hub 3.22.5, batch-mode start exit 0, UnityPersonal entitlement). 【Unverified — validate】 an actual project build (OQ-T01 compatibility scope), size, startup memory, runtime behavior on Playgama hosting.
- 【Proposal】 local batch pipeline (P7/D-038): CLI invocation of the installed editor with `-buildTarget WebGL` + a build method (editor code authored only under Phase-4/7 authorization) → artifact directory → manual archive/inspection → local browser smoke; full `-logFile` capture required for evidence. CI deferred per D-038 until release-cadence justification with owner approval.
- Development vs release build profiles 【Proposal】: dev = debug symbols, console stack traces, low stripping, profiler hooks; release = maximum stripping, no diagnostics, profiler off; compression choice affects loader size/decompression hosting needs — measured at release, no invented numbers. Decompression-fallback details are a deployment decision recorded via the `PLAYGAMA_CONSTRAINTS.md` re-verification (OQ-P02).

### 40.4–40.7 Execution constraints & runtime discipline
- 【Official-doc fact】 single WebAssembly memory block; growth must be physically contiguous and can fail; mobile browsers need deliberate initial-memory tuning (Player Setting) sized to typical usage.
- 【Official-doc fact】 frame-bound GC (§28.3) — per-frame allocation is an architecture rule violation, not tuning slack. Binding ruleset: no per-frame `new` in hot paths; no LINQ in gameplay loops; events use `readonly struct` payloads; no string concatenation outside editor/debug guards; lists/dicts pre-allocated with explicit capacities at load boundaries; unavoidable allocations namespaced to loading phase, never racing. Per P8: each build stage records allocation notes; repeated violations become defect-backlog items, not habits.
- 【Expectation】 CPU/physics cost concentrates in wheel rigs × substeps, suspension raycasts, and rigidbody contact processing — only the player vehicle(s) simulate in V1 single-player (no AI before OQ-T09). Timestep/substeps are measured decisions (§36.9); **no frame-rate target promised**.
- 【Expectation】 download-size levers (no numbers invented): engine stripping; texture compression/resolution policy; audio streaming vs decompress-in-memory; avoiding unnecessary packages; minimal shader variants (pipeline-dependent OQ-T02); AssetBundle separation of environment content out of the initial blob.
- 【Expectation】 draw-call discipline: shared-material patterns, atlas discipline, batching-friendly authoring rather than runtime tricks; actual numbers from profiling evidence.

### 40.8–40.15 Asset domains & behavior
- Textures/audio 【Official-doc fact + Expectation】: textures dominate memory; compression format per platform; audio streaming reduces resident memory but costs latency; both profiled per asset family at vertical slice (OQ-A08/T08) — no invented caps. Arabic font set is a known OQ-A01 licensing/size gate.
- Asset loading 【Proposal, per §35.6】: race/environment content behind the loadable boundary (additive scenes/bundles); UI/bootstrap inside persistent root; language/font assets load with UI.
- Scene loading & cleanup: additive unload + `UnloadUnusedAssets` at race exits; retry never reloads; repeated load/unload cycling must not steadily grow heap — verified at vertical slice, not assumed.
- Input latency: derived from frame pacing (no invented numbers); mitigation candidates: input processed at the fixed-update boundary; measured in-browser (prototype, OQ-T04).
- Browser audio activation: web audio usually requires a user-gesture unlock — design defers audio init to the first gesture (§29 S12); browser variance validated in the prototype, not assumed.

### 40.16–40.19 Compatibility, profiling, diagnostics
- Compatibility ladder (each stage evidence-gated): (1) local batch artifact loads via simple HTTP hosting; (2) DevTools checks — console exception absence, wasm size sanity, network waterfall (`.data` weight visible); (3) gameplay smoke (Boot→Race→Results); (4) mobile-browser probe **per QA matrix plan** (device choice owner, OQ-P01).
- Browser/device test matrix: not yet defined 【Verified】. 【Proposal — owner-gated placeholder, OQ-P01】: desktop-first triage (Chrome, Firefox, Edge, Safari on dev workstations) + representative Android/iOS chosen at Phase-13/14 QA planning — the design fabricates no "supported list".
- Profiling workflow (P8 evidence ritual — binding; thresholds derived **from** measurements, recorded in the `PLAYGAMA_CONSTRAINTS.md` §5 evidence log):
```text
Stage 1: Empty/min web build → record baseline sizes, load, GC notes
Stage 2: Prototype build (driving loop) → record frame cost, GC pauses, heap growth over retries
Stage 3: Vertical-slice build → record full quality-bar evidence → basis for the OQ-T08 budget proposal
```
Each measurement gets build hash, date, machine/network context, readings; deviations become defect backlog. Instruments (title suggestions, no tool installed): Unity Profiler attached to the dev web build + browser DevTools performance/memory traces.
- Build failure diagnostics 【Plan】: expected classes (not yet observed) — wasm heap allocation failure at startup, missing decompression support at host (mixed content/mime config), C# compile errors in the batch log, content-key failure at runtime (fail-closed validation). Rule: build log always retained (`-logFile`); browser console/screenshot attached to the failure report.

### 40.20–40.23 Automation, CI, Playgama boundary, acceptance
- Local batch automation 【Proposal — description only; no scripts authored】: entrypoint (Phase 4, owner-gated) parametrized by project root, build method name, output dir, web target; invokes the installed editor CLI, waits, verifies exit code, captures the log, moves artifacts, prints key line markers. Windows/Linux parity deferred (Ubuntu today).
- **T5.5.1 implementation note (2026-11-08):** the permanent editor-only tool `Assets/Editor/VelocityRushWebGLBuilder.cs` now provides the local WebGL Development build entry (`Tools/GameCraft Studio/Velocity Rush/WebGL Build`). It performs blocking preflight validation (editor idle, WebGL module, active WebGL target, exact approved scene order), enforces an output folder outside the Unity project with an explicit non-empty-output confirmation, runs `BuildPipeline.BuildPlayer` with `BuildOptions.Development`, and writes a JSON report (BuildReport summary, duration, result, and index.html/wasm/data/framework.js/loader.js sizes). It never edits ProjectSettings/PlayerSettings, packages, scenes, render/quality/compression/memory/scripting/exception/graphics settings, and never deletes prior output. Non-destructive validation passed (menu resolution, 9/9 preflight checks, four negative safety guards, JSON generation); **no WebGL build was executed in this step**, so OQ-T08 build/size/performance evidence remains pending.
- **T5.5.2/T5.5.3 implementation note (2026-11-10):** the first WebGL Development build and browser runtime validation completed. The `dev-20261109-210202` build exited 0 with 0 errors/warnings, a cold duration of 17.354 s, and a 29,722,007 B BuildReport total (five-artifact payload 29.71 MB, ~9.9% of the 300 MB Playgama archive limit). In Chromium 152/WebGL 2.0 the wasm linear heap initialized at 95,944,704 B (91.50 MiB) and the JS heap stayed near 9.5 MiB; the race loop, input, and chase camera operated correctly with no observed GC hitches. Tool hardening added the permanent batch entry `VelocityRush.EditorTools.VelocityRushWebGLBuilder.BuildFromCommandLine` (`-vrOutput`/`VR_WEBGL_OUTPUT`, optional `-vrAllowNonEmptyOutput`/`VR_WEBGL_ALLOW_NONEMPTY`, non-zero exit on failure), fixed `build.reportPath`, made default outputs collision-free, rejected project-containing output folders, scoped overwrite consent to the confirmed path, and cleared stale window summaries. Archive size is not runtime memory; these measurements are prototype evidence, not final budgets. See `production/ROADMAP.md` for the Phase-5 closure note.
- Future CI (D-038 deferral, not rejection): trigger candidates = release-cadence builds, PR-time web smoke. Known constraints recorded for future owner review: Unity licensing on hosted runners, runner storage, headless license activation fragility.
- Playgama integration boundary 【Phase 13 strictly】: build output must satisfy `PLAYGAMA_CONSTRAINTS.md` packaging rules (`index.html` at archive root, Latin filenames, no external asset hosting, re-verify ritual); nothing Bridge-related ships before Phase 13.
- Performance acceptance 【evidence-first】: each P8 stage produces its evidence; acceptance thresholds are set by the owner from that evidence and recorded as the budget table (OQ-T08 resolution); failures roll into the profiling-led fix list — never silently accepted.
- Constraint cross-check 【Verified env fact】: editor + WebGL module + identity + C# extensions installed; built-in renderer baseline. 【Official-doc fact】: build architecture, frame-bound GC, `.data` residency, memory/heap constraints, audio unlock, Data Caching. 【Expectation/Proposal】: levers, profiles, evidence-gated acceptance, bundle strategy, A/B comparison plan. 【Prototype-verified 2026-11-10 — remaining unverified】: actual build success, sizes, and startup memory are measured for the Chromium desktop prototype (see the T5.5.2/T5.5.3 note); still unverified: runtime on Playgama, the browser/device matrix, OQ-T01 compatibility scope, OQ-T02 A/B results, and owner-set OQ-T08 thresholds.

## 41. Error and recovery paths (consolidated; cross-section consistent)

| Failure | Detector | Immediate policy | Recovery | Status |
|---|---|---|---|---|
| Invalid content definitions | Definition-index validation (§30.6 step 4) | fail-closed at Boot; dev diagnostics name the asset; on release builds block progression to gameplay, present generic maintenance wording — exact wording spec-gated | content fix via pipeline | consistent; protects OQ-A05 discipline |
| Failed scene loading | async load result (missing key, corrupt content) | cancel, unload partial state, return to prior safe state; retry affordance needs spec (no invented error screens) | P6 pattern keeps the partial-failure surface small | §35.7 |
| Save write fails | ISaveStore result code (quota/private mode) | keep session state in memory; retry-once-then-report policy 【final policy pending OQ-09】 | next autosave re-attempts; user warning only if a real loss risk exists (wording spec-gated) | §39.4 |
| Save load fails / corrupt | version/migration mismatch | fresh defaults + dev corruption report; **never** fabricate progress; backup-copy rotation 【Recommendation pending OQ-09】 | settings-reset controls 【UX pending spec】 | §39.4 |
| Race interruption (browser blur/tab loss) | visibility events (platform adapter Phase 13; prototype: engine pause flags) | auto-suspend per §30.4; ads never trigger resume | manual resume only | final behavior confirmed by platform spec (§22) |
| Pause/resume | §30.4 | Race Rules sole arbiter; manual resume only | — | consistent |
| Fast Retry | §30.5 | scene non-reload; race-state reset; final-exit distinction (audit A2) | — | spec-gated UX |
| Duplicate result processing | exactly-once keyed handoff (§30.5); Save serialize post-state | Race Rules produces `RaceResult` once (readonly) | idempotency key formalized at Phase 5 (race-instance ID field) | §36 above note |
| Missing localization key | catalog parity EditMode tests; runtime fallback rule spec-gated (OQ-A03) | **never ship half-mirrored UI** | parity fixes | §29 S13 |
| Audio initialization failure (web unlock) | user-gesture probe | silent until first gesture; retry silently | resume on next gesture 【prototype validates】 | §29 S12/OQ-A11 |
| Platform service unavailable (Phase 13) | adapter capability probe | gameplay never depends on platform success (D-021 rewarded rules; offline-capable core) | adapter degrades per probe | Phase 13 details |
| Memory pressure (heap growth) | profiling gates ritual (P8) — not runtime auto-detect | budget discipline + AssetBundle unload discipline | no runtime auto-degradation this scope (OQ-T08) | provisional plan |

No behavior here invents new screens or English/Arabic copy — all user-facing failure presentation is spec-gated (OQ-A03).

## 42. Testing and validation architecture (planned only — no claim that tests exist)

| System | EditMode (planned) | PlayMode (planned) | Manual | WebGL/browser (planned) | Perf profiling (planned) | When |
|---|---|---|---|---|---|---|
| GameFlow transitions | pure rule-table tests | screen-circuit integration | as needed | smoke path | — | Phase 4 |
| Bootstrap | graph construct-without-scene | — | — | — | startup segment timings | Phase 4 |
| Data Definitions schema | schema tests w/ fixtures | — | authoring review | — | — | Phase 4 |
| Save (DTO/migrate) | round-trip + synthetic-version tests | store-based integration | corruption drills later | storage-behavior checks | — | when Save lands (Phase 11 / Phase 13 revisit) |
| Race Rules core | synthetic-stream scoring tests (highest-value) | scripted-lap integration | D-023 playtest | judge parity | frame cost | Phase 5–6 |
| Vehicle physics | profile schema + pure math | repeatability lap harness | D-023 feel (authoritative) | — | wheel/substep cost | Phase 5–6 |
| Input | adapter mapping + reset tests | fake-adapter lap | device matrix manual (OQ-P01) | latency/touch checks | input-path alloc check | Phase 5 |
| Camera | profile schema | collision scripted test | feel playtest (D-023) | — | no-alloc check | Phase 5–6 |
| Track & Event triggers | — | trigger correctness | — | — | — | Phase 5 |
| Progression | rules tests + save integration | — | — | — | — | Phase 10–11 |
| Localization | en/ar parity tests | RTL relay display check | GDD §33.5 playtest | — | — | from first UI |
| UI navigation rules | stack/overlay rule tests | flow integration | per-screen spec reviews (OQ-A03) | touch ergonomics | — | Phase 4 rules → later |
| WebGL build | — | — | browser smoke ladder (§40.16) | OQ-T01/OQ-T07 evidence steps | P8 three-stage ritual | first build owner-gated |
| Platform adapters (Phase 13) | mocked adapter tests | — | — | real behavior at Bridge contact (OQ-P02 first) | — | Phase 13 |

Rules: tests never claim performance (that is the evidence log, §20); OQ-T07 (automated build/test strategy) stays formally OPEN — this section proposes only; test naming/conventions finalized with the Phase-4 code-conventions task 【Owner gate】.

## 43. Integration review results (F1–F5, performed 2026-11-05)

- **F1 — Responsibility boundaries (15 systems classified; Commercial V1.0 scope untouched):** required early (9): Bootstrap, GameFlow, Data Definitions (thin index + schema validation), Runtime State (thin), Vehicle, Track & Event (minimal test track), Race Rules (minimal judging), Localization (thin), Camera/Presentation (temporary default per OQ-01). Initially folded (3): Audio (slim router inside Core-owned services), Garage (roster browsing inside UI when roster exists), UI/Navigation (thin registry + minimal HUD in the Core seam; `UI` assembly later). Deferred (3): Progression (implementation; seam reserved), Save (implementation; `ISaveStore` reserved), Platform Integration (fully, Phase 13). No unnecessary manager introduced; the four folds exist as responsibilities inside other systems until their phase. Deferrals are stage-scoped (D-030), not scope reductions (F-G2 ✅).
- **F2 — Dependency integrity 【Consistent】: dependency directions (downward + Core-owned interfaces); interface ownership (the five seams in Core); assembly matrix cycle-free (§33.2, audit F-E1 inversion edge documented); no circular dependencies; event-bus usage bounded (§32); data/runtime-state ownership matches §34.3 exactly.**
- **F3 — End-to-end gameplay flow (GDD §6 loop; no new features):**
| Loop step | Owning system | Data handed onward |
|---|---|---|
| Select Championship | ChampionshipSelection (UI) reading ChampionshipDefinition + Progression progress | `SelectionContext.championshipId` |
| Select Event | EventSelection (UI) + EventDefinition (8-type basis 【Provisional D-031】) | `SelectionContext.eventId` + rules handle |
| Select/Prepare Vehicle | Garage (UI/context) reading VehicleDefinition + unlock state | `SelectionContext.vehicleId` |
| Pre-Event | Pre-Event screen; assist/controls confirmation values per OQ-A03 spec | validated `SelectionContext` → GameFlow approve |
| Race loading | GameFlow async additive load (P6 【Provisional】); Vehicle+Track+Camera race-scoped created | scene keys from EnvironmentTheme/SceneDescriptor |
| Race driving | Vehicle physics (§36) ◀ `IVehicleInput` (§37) on track signals (§29 S4) | telemetry stream → Race Rules |
| Scoring/judging | Race Rules pure core (formulas OQ-03 Open) | `RaceResult` (once; numeric outputs stay empty until formulas approved) |
| Results | Results screen reads projection; GameFlow state change | exactly-once handoff (§30.5) |
| Rewards/Unlock presentation | Progression `ApplyRaceResult` → overlays (spec pending OQ-A03/OQ-17) | `ProgressionUpdated`/`Unlocked` |
| Upgrade/Unlock | Garage/Upgrades flows → UpgradeDefinition curves → UpgradeModifiers (§36.7) | physics parameters next race |
| Progression update | Progression → SaveService autosave (coalesced, §39.5) | SaveModel snapshot |
| Replay | GameFlow retry command (scene reuse — no reload) or fresh event selection | — |
- **F4 — Error/recovery paths:** consolidated table §41 — verified consistent across the package (audit corrections applied).
- **F5 — Testing traceability:** §42 matrix — planned tests only; zero claims of executed tests.

## 44. Draft decision records and owner-gated register (proposals — NOT filed in `DECISIONS.md`)

### 44.1 Draft decision records D-039…D-042 (audit F-C1 response; UNFILED)
Four working-architecture requirements introduced by the design pass with no registered decision record. Suggested filings (owner decides whether to approve each as PROVISIONAL/APPROVED-INITIAL):

| Draft ID | Topic | Proposed content | Proposed status |
|---|---|---|---|
| Draft **D-039** | Prototype communication model | Direct references + interfaces + per-emitter C# events for the prototype; typed ring deferred with documented adoption evidence (§32.4) | tentative PROVISIONAL |
| Draft **D-040** | Exactly-once result processing | Race-completion handoff idempotent by session-unique race-instance id; Progression sole applier (§30.5) | tentative PROVISIONAL |
| Draft **D-041** | Assembly reference matrix | Core/Data/Gameplay (+tests) initially; reference table §33.1; UI/Platform/tests staged | tentative PROVISIONAL |
| Draft **D-042** | Fail-closed data validation | Definition-index validation at Boot; invalid content blocks gameplay with diagnostics (§34.2/§41) | tentative PROVISIONAL |

These are implementation-approach requirements, not question resolutions — filing them would close no OQ. Consequences of not filing: they remain recommendation text here (still govern by review, but untraceable in the decision register — the audit's concern). **They are not registered in `DECISIONS.md` (owner-gated); the audit's process suggestion R-2 remains open.**

### 44.2 Owner-gated decision register (current, consolidated) 【Open】
| # | Item | Status | Options | Proposed recommendation | Approval needed |
|---|---|---|---|---|---|
| 1 | Approve Part II of this document as the working architecture plan | Open | approve as-is / request rework | approve (does not close any OQ) | owner review |
| 2 | Draft decision records D-039…D-042 filing | Open | file all as PROVISIONAL / file subset / reject | file all 4 | DECISIONS change-history entry (owner) |
| 3 | Render pipeline choice | **Decided 2026-11-10 for the development prototype:** Built-in retained, URP deferred (D-032) | Built-in / URP after A/B | Built-in as shipped | A/B evidence recorded; re-evaluation at vertical-slice art production |
| 4 | Vehicle physics strategy | **Decided 2026-11-10:** WheelCollider Simcade approved (D-033); hybrid fallback not adopted | WheelCollider-first / hybrid | WheelCollider baseline as shipped | D-023 playtest verdict recorded; per-vehicle tuning later (OQ-02/OQ-06/OQ-07) |
| 5 | Input System package install | Open (OQ-T04/D-034) | install / stay legacy adapter | install when Phase 5 requires (behind seams) | manifest-change owner approval |
| 6 | First WebGL build authorization | Open (OQ-T01 scope/OQ-T07) | build now / defer | build empty project first (advances OQ-T01/T07/T08 stage 1) | owner |
| 7 | Save provider + autosave cadence + first-run defaults + backup rotation | Open (OQ-09/OQ-T05/D-036) | per candidates §39 | JsonUtility-first tilt; trigger list; rotation | owner policy |
| 8 | Phase-4 conventions (Assets skeleton, asmdef set, naming, scene conventions, code conventions) | Foundation accepted (owner-directed Parts 4.1–4.2, 2026-11-05 → D-043 APPROVED-INITIAL); remaining Phase-4 task authorizations open | owner-authorized task per task step | continue Phase-4 tasks (T-4.5 GameFlow stubs, T-4.6 build automation) | owner |
| 9 | Exact retry flow & loading-cancel UX | Open (GDD §6/OQ-A03) | spec pass | defer to OQ-A03 | spec approval |
| 10 | Loading-cancellation capability boundary (R-4) | Open | approve capability-only statement / extend UX now | approve; UX details at OQ-A03 | owner confirmation |
| 11 | Camera perspective & feel values | Open (OQ-01/OQ-A06) | per tuning | temporary default for prototype | owner at Phase 6 |
| 12 | All numeric content/performance values | Open (OQ-03/04/06/07/14–19, OQ-T08) | measurement/content passes | none invented — established from evidence/content design | owner |
| 13 | Playgama Bridge integration | Phase-13-locked (D-036 constraint) | nothing now | stays locked; OQ-P02 ritual before coding | owner at Phase 13 |

### 44.3 Phase-3 readiness verdict (consolidated from the final readiness report)
**READY WITH DOCUMENTED OPEN DECISIONS** (verdict preserved from the Phase-3 readiness report): every Phase-3 documentation deliverable existed and was internally consistent (F1–F5); the provisional direction set complete and traceable (P1–P8 → D-032…D-038 → Part II); audit corrections applied; no OQ closed, no decision promoted, no package/settings/repo-structure change, nothing staged/committed. The documented open decisions above are exactly the owner-controlled residues Phase 3's mandate must leave open. **Documentation completion ≠ implementation readiness:** no code, scene, prefab, or tooling exists; the architecture is a blueprint awaiting (a) owner approval and (b) the OQ-T01/OQ-T07/OQ-T08/OQ-T02/OQ-T03 validation evidence required for final technical decisions. **Phase 3 is NOT complete** — closure requires owner approval of the architecture plus the listed validation evidence (see `production/ROADMAP.md` Phase 3 status).

## 45. Architecture risks and mitigations

### 45.1 Discovery-phase risks
| # | Risk | Impact | Likelihood | Mitigation |
|---|---|---|---|---|
| R1 | URP/Built-in A/B profiling consumes Phase 4/5 time | Medium | Medium | Time-boxed A/B on one prototype scene; prototype stays minimal regardless |
| R2 | WheelCollider tuning rabbit hole | High (D-023 gate) | Medium | Data-driven per-class curve packs; daily playtest cadence; hybrid fallback documented |
| R3 | Web GC pitfalls in naive code | High | High unless disciplined on day 1 | Mandatory patterns from day 1 (§28.4/§40.7); profiling habit via build artifacts |
| R4 | Browser/localStorage save limits (e.g., iOS Safari small quotas) | Medium | Medium | Size-tolerant SaveModel (small payloads); degradation plan; Phase-13 Bridge pivot |
| R5 | Playgama platform terms change before Phase 13 | Medium | Low-Med | `PLAYGAMA_CONSTRAINTS.md` re-verify ritual (pre-integration & pre-submission) |
| R6 | COOP-COEP host support missing for Burst/jobs on Web | Medium | Medium | Single-threaded-first gameplay; multithreading only as a post-profiling optimization |
| R7 | WebGL audio unlock (user gesture) mishandled | Low-Med | Med | Audio bootstrap behind first user interaction; prototype task |
| R8 | Input System install deferred → abstraction leak | Low | Low | Interfaces designed regardless; swap cost controlled |

### 45.2 Detailed-architecture risks (R-A series)
| # | Risk | Mitigation | Validate by phase |
|---|---|---|---|
| R-A1 | Overengineering: reserved-but-never-used interfaces accumulate | Reserve only seams with named consumers (§29.4); every addition needs a logged consumer | Phase 5 readiness review |
| R-A2 | Event-bus sprawl / hidden globals | Closed set §32.2; ring owned by Core; no string-topics; subscription assertions in integration test | Phase 5–6 |
| R-A3 | Dependency cycles | asmdef reference tables compile-enforced (§33); violation fails the build | Phase 4 → enforced forever |
| R-A4 | Double truth for progression facts | Exactly-once handoff (§30.5) + single-mutator matrix (§34.3); integration test asserts "one apply" | Phase 10–11 |
| R-A5 | Data duplication (UI caching stale stat copies) | Projection interfaces only; no cached mutable state in UI | first UI code review |
| R-A6 | Save ownership conflict (multiple writers) | SaveService sole writer; Progression push-only; schema additions gated to Save owner | Phase 11/13 |
| R-A7 | WebGL lifecycle: audio unlock, tab suspend, context loss | explicit §30.6/§41 steps; prototype must exhibit both behaviors once before Phase-6 sign-off | Phase 5–6 |
| R-A8 | Web-GC pressure from event/message allocations | readonly-struct payloads; no per-frame events (§32.3); P8 profiling habit | every build stage |
| R-A9 | Prototype architecture ≠ commercial architecture (retrofit debt) | deferred systems get Core-facing seams now; assemblies land when needed (§33.1 "When") | Phase 7 vertical-slice gate |
| R-A10 | State sync between app FSM and race FSM (e.g., quit during loading) | single transition coordinator + restricted race command surface; loading-cancel rules §30.3 | Phase 5 |
| R-A11 | Untuned feel values derail timeline | data-driven profiles; playtest cadence; fallback path (D-033) | Phases 5–6 |

## 46. References
- Internal authoritative: `docs/GDD.md` (§6 loop, §24 screens, §25/§27/§33) · `docs/DECISIONS.md` (D-002/006/008/009/010–014/020–023/027–031, D-032…D-038, v0.3.1–v0.3.3 history) · `docs/OPEN_QUESTIONS.md` (OQ-T01–T09, OQ-01…OQ-21, OQ-A01…A11, OQ-P01…P05) · `docs/TRACEABILITY.md` · `docs/PROJECT_STRUCTURE.md` · `docs/AI_AGENT_GUIDELINES.md` (§2/§3/§5/§6a) · `docs/PLAYGAMA_CONSTRAINTS.md` · `production/ROADMAP.md` (phases + milestone register M0–M12).
- External official documentation (read-only references, accessed 2026-11-05): Unity 6.6 Web manual (overview; Memory in Unity Web — heaps/`.data`/AssetBundles/Data Caching/GC; Cache behavior; graphics APIs; audio; multithreading/COOP-COEP; .NET API support; WebAssembly 2023; debugging) at `docs.unity3d.com/6000.6/Documentation/Manual/...`; `WheelCollider` Script Reference (6000.6); URP 17.2 manual; Input System 1.14 manual; Playgama Bridge getting-started docs and `github.com/Playgama/bridge-unity` (min Unity 2022.3, package `com.playgama.bridge` v2.4.0 — **nothing installed**).
- Historical provenance: this Part II consolidates the nine retired Phase-3 standalone documents (created 2026-11-05; retired by consolidation, recorded in `CHANGELOG.md`); discovery content was produced without modifying any existing decision status, open question, package, setting, scene, or script. The independent architecture audit returned **CONDITIONAL PASS (0 CRITICAL / 3 MAJOR / 6 MINOR)** — the three MAJORs and the audited MINOR pins were resolved as the applied audit-correction set A1–A4 (§29.5 S8/S10/S13 pins, §30.2 row-6 and row-8, §30.5 retry-context rules, §32.2 allocation qualification, §30.6 step-9/16 scopes); the four unregistered working requirements it identified are the draft decision records in §44.1.

## 47. Vertical slice — Stage-1 definition, quality criteria, asset pipeline, lighting and UI standards (2026-11-10)

> Status: **Phase 7 Stage 1 and Stage 2 (T-7.6…T-7.13) are complete; M5 is not.** This section records the owner-approved slice composition, the draft quality criteria, the internal-only asset pipeline/provenance rules, the Built-in lighting decision, the UI Toolkit stack, the profiling-harness contract, and the Stage-2 implementation/build-profile evidence. The criteria and numeric budgets remain owner-gated; **no lightmap bake and no WebGL slice capture is claimed** (see §47.8).

### 47.1 Slice composition and explicit non-goals (owner-approved 2026-11-10, D-044)

- **Environment identity:** Desert Circuit. The existing `HandlingCourse` section (100 m start straight with braking markers, T1, T2 crest, T3, return) is reused as the slice section and upgraded with production materials rather than re-authoring a new layout.
- **Hero vehicle:** one Sport-class vehicle using the `CandidateA_Grip_Vehicle` handling profile; the physics rig is the approved WheelCollider baseline (D-033) and is not re-tuned by presentation work.
- **Gameplay:** one time-trial event — countdown → run → finish → results → retry. No scoring, stars, rewards, progression, or championship state.
- **Screens:** HUD (speed, race time, checkpoint progress), countdown, results, and (Stage 2) pause. English-only.
- **Audio scope:** engine loop with pitch/volume mapping, tyre/impact one-shots, and UI stings. No music, no localization.
- **Explicit non-goals:** main menu, championship selection, garage, upgrades, progression, save, Playgama Bridge, localization, and any commercial meta-system.
- **Review basis:** desktop Chrome and Firefox, targeting a stable 60 fps (OQ-P01/OQ-T08). Mobile-class review is not part of M5.
- **Technical basis:** Built-in Render Pipeline (D-032), Baked GI + Light Probes, no post-processing package (OQ-A10); UI Toolkit only, with no manifest change.

### 47.2 Quality criteria draft (OQ-A08 / OQ-A09 — DRAFT, not owner-approved)

Reference status: the only existing board (`AD-BOARD-001`) remains **DRAFT** and cannot be treated as an approved specification. Stage 1 therefore applies written criteria plus camera-angle-matched review against the reference library; the owner must approve the criteria and reference status before T-7.16 can pass them.

| Asset class | Pass criteria (draft) | Evidence / review method | Status |
|---|---|---|---|
| Vehicle (OQ-A09) | Silhouette reads as a Sport-class car at gameplay distance; coherent body/glass/wheel material separation; no visible primitive seams; LOD0/LOD1 parity at speed; collider parity with the visual footprint | 3/4 front, rear, and chase-camera frames at 960×540 and 1920×1080; overlay with reference board at matched angle | Draft; hero mesh not yet produced |
| Environment | Desert identity via silhouette, material palette, and landmarks; road edge, barriers, and checkpoints remain readable at speed; no repeated-kit monotony inside the section | Chase-camera run at 60 fps; checkpoint readability check; side-by-side with the environment reference direction | Draft; production kit is T-7.6 |
| Materials | Asphalt, sand, metal barrier, paint, glass, and rubber are visually distinct; roughness/metalness variation is restrained; no mirror-like paint; no magenta/error shaders | Material sheet capture under the slice lighting; shader-variant and texture-count audit | Draft; baseline materials only |
| Lighting | Warm key light and readable shadows; road visibility preserved; vehicle silhouette separated from the background; no crushed blacks or blown highlights; baked indirect contribution visible | Capture with the baked/probe rig at the approved camera angles; luminance histogram and near-black coverage | Draft; bake is T-7.8 |
| UI | HUD hierarchy is legible at 1920×1080 and 1280×720; speed/time/checkpoints never overlap; results readable in one glance; no per-frame allocation on the steady-state HUD | Panel screenshots; `GC.GetAllocatedBytesForCurrentThread` guard tests; frame-time probe | Draft; foundation implemented |
| VFX | Effects communicate events without obscuring the road, checkpoints, or UI; overdraw is bounded and measured; no effect runs without a measured budget | Profiling capture per effect; visual review at speed | Draft; T-7.9 |
| Audio | Engine loop tracks RPM/speed without audible seams; tyre/impact one-shots are distinct; UI stings are short and non-fatiguing; no clipped or silent states | Listening pass; browser console/audio-state check; pooled-source audit | Draft; T-7.11 |
| Performance (slice) | Stable 60 fps on the approved desktop browsers; no per-frame GC in gameplay; draw calls, triangles, memory, and lightmap residency recorded per feature | T-7.4 harness + Stage-3 matrix; evidence rows in `PLAYGAMA_CONSTRAINTS.md` §5 | Draft; numeric thresholds deferred to T-7.15 |

Review method (applies to every row):
1. Read the relevant GDD/TDD/art-direction section and the current reference status before judging.
2. Capture at the documented camera angles/resolutions with the approved lighting path.
3. Record pass/fail per criterion and every deviation in the deviation log (owner-visible), with a screenshot/measurement reference.
4. Any quality reduction caused by performance must cite a measured cost and an owner decision — never a silent downgrade (D-006).
5. Numeric thresholds are added only after the Stage-3 measurements (OQ-T08); this draft deliberately contains none.

### 47.3 Asset pipeline and provenance (OQ-A05 — internal-only slice route)

For the vertical slice the owner approved an **internal-only provenance route**: engine primitives, procedural materials, and engine-native assets. No external model, texture, audio, font, or shader dependency is introduced, so no third-party licence attaches to the slice.

| Asset family | Source for the slice | Licence / provenance | Owner | Status |
|---|---|---|---|---|
| Track surface, barriers, ground, braking markers | Generated by `HandlingCourseBuilder` (engine mesh primitives) | Unity-generated project asset; no external source | Engineering | Implemented |
| Materials (asphalt, barrier, paint, glass, metal, rubber) | Procedural `Standard` shader materials with generated PNG textures (`SliceTextureKit`) | Unity built-in shader; no external texture | Engineering | Implemented for the slice (T-7.6/T-7.7) |
| Vehicle body/glass/wheels | Engine primitives; the shared prefab stays untouched and scene-local additions re-skin/body-kit the instance (`SliceHeroVehicleKit`) | Unity-generated project asset | Engineering | Implemented for the slice (T-7.7); production hero mesh remains Phase 8 |
| Lighting/probe data | `DesertSliceLighting.lighting`, procedural skybox, `LightProbeGroup` | Engine-native settings/data | Engineering | Configured; bake tool implemented but **not baked/claimed** (T-7.8 limitation) |
| UI | UI Toolkit visual tree built in C# + Unity default runtime theme | Engine-native UI Toolkit; no font/icon asset added | Engineering | Implemented |
| Audio | Procedurally synthesized WAV clips (`SliceAudioKit`), no external sample pack | Unity-generated project asset | Engineering | Implemented for the slice (T-7.11); no third-party audio |
| VFX | Engine `ParticleSystem` with generated soft-dot material (`SliceVfxKit`); automatic emission disabled | Unity particle system; engine-generated texture | Engineering | Implemented and budgeted (T-7.9) |

Pipeline rules (binding for the slice and a baseline for Phases 8–9):
- **Scale/orientation:** 1 Unity unit = 1 metre, Y-up, content faces +Z; pivots at the ground-contact point for props and at the chassis origin for vehicles.
- **Meshes:** UV0 carries tiling; UV2 is required for baked GI and is generated at build time with `Unwrapping.GenerateSecondaryUVSet` for generated meshes. LODs and collider parity are required for hero assets; MeshColliders are limited to the road/ground/barrier surfaces, with primitive colliders for props.
- **Materials:** one shared material per family, no per-instance material cloning in gameplay; Built-in `Standard`/legacy shaders only; no URP shaders, no post-processing dependency.
- **Textures (when introduced):** power-of-two dimensions, platform-appropriate compression, atlas by material family, mipmaps on, readable/CPU access off unless a measured need exists. No numeric resolution cap is invented here — it is set by Stage-3 measurement.
- **Static content:** `ContributeGI`, `BatchingStatic`, `OccluderStatic`, `OccludeeStatic` on immutable environment geometry; dynamic vehicle/HUD objects remain non-static.
- **Provenance rule:** every future external asset must be entered in this register (source, licence, owner, date) **before** it is committed. Unlicensed or unclear-licence assets are prohibited.
- **Zero-cost discipline:** no runtime asset generation, no `Resources.Load` in gameplay, and no per-frame material/string allocation in the slice loop.

### 47.4 Lighting, baked GI and light probes (OQ-A10 — approved path)

- **Pipeline:** Built-in Render Pipeline only (D-032); no URP asset or shader.
- **Global illumination:** baked GI enabled, real-time GI disabled. Mixed lighting uses **Indirect-Only** so the directional light keeps real-time direct light and shadows while indirect light is baked into lightmaps and probes. Shadowmask/Subtractive are not used in Stage 1.
- **Bake settings:** `UnityComputeGPU` lightmapper, 20 texels/unit, 1024 maximum lightmap size, 2 px padding, AO on, one bounce, albedo boost 1.0, indirect scale 1.0.
- **Scene lighting (Stage 2):** one mixed directional light (warm 1.0/0.93/0.82, intensity 1.35, soft shadows, strength 0.85), `Skybox/Procedural` desert skybox with skybox ambient (intensity 1.05), and linear desert fog (170–640 m, 0.80/0.71/0.57). Static environment renderers carry `ContributeGI`/batching flags; generated road/barrier/edge-line meshes carry UV2 charts for a future bake.
- **Light probes:** deterministic two-height grid (24 m spacing at y = 2.5 and 6.0 m) covering the course bounding box, so the moving vehicle receives baked indirect light without real-time GI.
- **Post-processing:** none. Any Post Processing Stack v2 or URP adoption requires a new measured proposal and explicit owner approval; it is not implied by this section.
- **Bake rule:** a bake is run only on an explicitly approved scene/lighting state, is recorded with its lightmap size and build hash, and is never claimed as done until the bake log is captured. **Stages 1–2 configure the rig but do not claim a bake.** `SliceLightingBaker` is the explicit tool (GPU compute lightmapper with a CPU-progressive fallback and menu/batch entry points); under this environment's `-batchmode -nographics` editor the Unity 6 lightmapper worker is unavailable (the render-pipelines-core worker importer is not installed), so the bake exits without lightmaps and the scene is saved without them. Re-run the menu tool in a GPU-capable editor before claiming T-7.8's baked-indirect criterion.

### 47.5 UI stack — UI Toolkit (T-7.5b, no manifest change)

- **Why UI Toolkit:** the `com.unity.modules.uielements` module is already present, so the HUD/Results/Pause stack needs **no package or manifest change**. uGUI + TextMeshPro would require a manifest addition and remains unauthorized.
- **Asset model:** `Assets/UI/Slice/SlicePanelSettings.asset` (scale-with-screen-size, 1920×1080 reference, match 0.5, sorting order 10) plus the committed `UnityDefaultRuntimeTheme.tss`. The visual tree is built programmatically; no UXML/USS asset is required for Stage 1.
- **Hot path:** `SliceHudPresenter` polls at 10 Hz by default and drives `SliceHudView`; numeric fields use `DigitDisplay`, which writes one of ten cached glyph strings per cell. EditMode proves repeated updates allocate **0 B**. `SliceHudModel`/`SliceHudFormat` are pure, allocation-free, and separately tested.
- **Cold path:** results text is composed once on `RaceFinished` (a single event, not a per-frame path). The HUD root uses `PickingMode.Ignore` so it cannot intercept input.
- **Pause (Stage 2):** `RaceRules` owns a pure `IsPaused`/`Pause`/`Resume` contract (countdown and elapsed time freeze; progress and finish are refused while paused; pause is refused after finish; retry resumes first). `SlicePauseDriver` is the only engine adapter and maps the accepted state onto `Time.timeScale` and `AudioListener.pause`. `SliceHudView` adds a named countdown group, a pause overlay, and a named results panel; the presenter forces an immediate refresh on pause transitions. Countdown length in the slice scene is 3 s.
- **Files:** `Assets/Scripts/Gameplay/UI/{SliceHudFormat,DigitDisplay,SliceHudSnapshot,SliceHudModel,ISliceHudView,SliceHudView,SliceHudPresenter}.cs`; pause adapter `Assets/Scripts/Gameplay/Race/SlicePauseDriver.cs`.

### 47.6 Profiling harness and Stage-0 baseline (T-7.4)

- **`FrameTimeStatistics`** (`VelocityRush.Core.Diagnostics`): fixed-capacity ring buffer; one allocation at construction; sanitises non-finite/negative samples; exposes mean/min/max, derived FPS, and interpolated p95 via a preallocated scratch buffer.
- **`PrototypePerformanceProbe`** (`VelocityRush.Gameplay`, dev-only at runtime via `UNITY_EDITOR || DEBUG`): configurable warm-up (default 3 s) and sampling window (default 10 s). It records unscaled frame time, then logs one summary containing frame count, mean/p95/min/max milliseconds, mean/min/max FPS, managed heap and delta, `Profiler` total-allocated/delta, draw calls, batches, triangles, vertices, graphics device type, and renderer name.
- **Capture protocol:** build a Development WebGL player, open the console, let the warm-up complete, and record the single `[VelocityRush.Perf]` line with device/browser/build hash. No number is treated as a budget until the owner approves it under OQ-T08.
- **Stage-0 baseline:** the current Built-in prototype evidence already recorded in `PLAYGAMA_CONSTRAINTS.md` §5 is the reference: 35.64 MB five-artifact payload (11.9% of the 300 MB archive limit), 28.816 s cold build, JS heap 9.95 MB, WASM ArrayBuffer 96.0 MiB, managed wasm memory 40.0 MB. The harness is wired into the slice scene; the first in-browser slice capture follows the T-7.12 build-contract extension and the T-7.14 matrix.

### 47.7 Stage-1 validation evidence (2026-11-10)

- Batch compile: **0 errors**; no new warning from Stage-1 code. Two pre-existing `UAC0009` warnings remain from the Phase-6 overlay guards (`TelemetryOverlayFormat`, `VehicleTelemetryOverlay`) and are the owner-mandated `DEVELOPMENT_BUILD` pattern.
- EditMode: **204/204 passed** (baseline 181 + 23 new tests covering frame statistics, time/speed formatting, HUD projection, digit rendering, full-view steady-state zero allocation, and overlay transitions).
- PlayMode: **7/7 passed**, 0 failed, 0 skipped.
- Scene regeneration: `Assets/Scenes/HandlingCourse.unity` rebuilt with `vehicle=CandidateA_Grip_Vehicle`, 347.0 m centreline, 3 checkpoints, mixed directional light, `DesertSliceLighting`, two-height light-probe grid, `SliceHud` (`UIDocument` + `SliceHudPresenter`), and `PerformanceProbe`.
- Known limits at Stage-1 close: the slice scene was not in the WebGL build contract (T-7.12, now implemented in §47.9); no lightmap bake is claimed (T-7.8); no in-browser slice capture exists yet (T-7.14); the quality criteria and numeric budgets remain drafts/open.

### 47.8 Stage-2 implementation and validation evidence (2026-11-11)

Stage 2 builds T-7.6…T-7.13 on the frozen Stage-1 composition (D-044) and technical basis (D-045). All commands, tests, and scene generation ran against project `Velocity_Rush_Project_Foundation_v0.1` in Unity `6000.6.8f1`; the working tree is uncommitted for owner review.

- **T-7.6 environment kit:** `SliceTextureKit` deterministically generates low-resolution PNGs (asphalt 512², sand/metal 256², hazard/soft-dot 128/64²) and a shared `SliceMaterialSet` (`Asphalt`, `TrackLine`, `Sand`, `Rock`, `Metal`, `Hazard`, `HeroPaint`, `HeroDark`, `Rubber`, `Chrome`, `Headlight`, `TailLight`, smoke/dust/spark particles). `SliceEnvironmentKit` adds edge lines, start/finish/checkpoint gantries with lamp accents, deterministic roadside rocks, and distant dunes; all decor is non-colliding, layer 0, and marked `ContributeGI`/`BatchingStatic`. The handling road, barrier, checkpoint/finish triggers, and physics layers are unchanged.
- **T-7.7 hero vehicle:** `SliceHeroVehicleKit` re-skins the instantiated `PrototypeVehicle.prefab` chassis/wheels and adds a scene-local cabin, glass, splitter, diffuser, side pods, wing, roll hoop, lamps, and wheel hubs. Every addition is a primitive with its collider removed; the shared prefab and all `CandidateA_Grip_Vehicle` physics values are untouched, so collider parity is exact.
- **T-7.8 lighting/atmosphere:** procedural desert skybox, skybox ambient, warm mixed directional light, and linear fog are wired into `HandlingCourse.unity`. **The lightmap bake is not claimed.** `SliceLightingBaker` provides the explicit `Bake Handling Course Lighting` menu and batch entry point, tries the Unity compute lightmapper then the CPU progressive fallback, and only reports success when a lighting-data asset and at least one lightmap exist. In the headless editor the Unity 6 bake worker is unavailable (`LightBakerWorkerProcessImporter.BakeWithWorkerProcess` missing because `com.unity.render-pipelines.core` is not part of this Built-in-only project), so the command exits `2`, logs `lightmaps=0`, and the scene is regenerated without a lightmap/reflection-probe sidecar. Re-running the menu tool in a GPU-capable editor remains the gate for the baked-indirect criterion.
- **T-7.9 VFX:** `SliceVfxKit` creates smoke/dust/sparks systems with automatic emission disabled and small max-particle caps (40/60/30). `VehicleVfxController` is the only emitter and uses `VfxEmissionMath` for thresholded, saturated, per-tick-capped counts plus a fractional budget accumulator; `ParticleSystem.Emit(EmitParams, int)` keeps the path struct-based (no managed allocation). Budget math is covered by EditMode tests.
- **T-7.10 gameplay/UI:** countdown → run → finish → results → retry with a 3 s countdown; pause via `P`/`SetPaused`; pause freezes the simulation and audio listener, hides the countdown, shows the pause overlay, and is refused after finish. Results show finish state/time/checkpoints; retry resumes first, clears the result, and restarts the countdown without a scene reload. HUD refresh stays at 10 Hz with immediate pause/state/result refreshes and the existing 0 B/frame numeric path.
- **T-7.11 audio:** `SliceAudioKit` synthesizes 16-bit mono WAV clips (engine loop, tyre screech loop, impact, countdown tick/go, pause/resume stings, finish sting) from deterministic oscillators/noise with loop-boundary crossfades and peak normalization. Runtime uses pre-authored `AudioSource` references and `AudioSourcePool`; the UI pool sets `ignoreListenerPause` so the pause sting is audible on the freeze frame. Engine/tyre response is pure, tested math (`EngineAudioMath`, `TyreAudioMath`), smoothed with unscaled time. No runtime `Resources.Load`, no external sample, no per-frame allocation.
- **T-7.13 test expansion:** EditMode covers pause rules, audio/VFX response math, scene-profile selection, HUD projection/countdown/pause/results, and zero-allocation guards. PlayMode covers additive slice load, the countdown→run transition, pause/time-scale/overlay behavior, pause refusal after finish, retry after finish, and unload/reload stability with a fresh scene instance and exactly one slice object graph.

**Validation (2026-11-11):** batch compile **0 errors / 0 warnings** (the two former `UAC0009` warnings were removed by replacing the deprecated `DEVELOPMENT_BUILD` guard with `DEBUG`); EditMode **241/241**; PlayMode **13/13** (0 failed, 0 skipped); `HandlingCourse.unity` regenerated at 347.0 m with 3 checkpoints, `CandidateA_Grip_Vehicle`, hero/slice systems, and `lighting=skybox+ambient+fog lightmapBake=explicitDeferred`. The prototype WebGL preflight automation still passes and `EditorBuildSettings` remains `Bootstrap` (0) → `TestTrack` (1).

**Remaining Stage-2 limits:** no lightmap bake or reflection-probe bake is claimed; no WebGL slice build/capture has been produced; the quality criteria and numeric budgets remain owner-gated; repeated-cycle *memory* measurement is Stage 3 (T-7.14) even though unload/reload correctness is now covered.

### 47.9 WebGL build-profile contract (T-7.12)

- **Profiles are opt-in data, not a global switch.** `WebGLBuildProfile.Prototype` keeps `Bootstrap → TestTrack` as the default contract; `WebGLBuildProfile.VerticalSlice` adds `Bootstrap → HandlingCourse`. `ApprovedScenePaths` remains a read-only alias of the prototype list, so existing preflight/build contracts and tests are unchanged.
- **No automatic build-settings writes.** The tool never edits `EditorBuildSettings`. The only writers are the explicit menu actions `Apply Vertical Slice Build Scenes` and `Restore Prototype Build Scenes`, each behind a confirmation dialog; the apply action warns that the Phase-5/6 EditMode suite asserts the prototype list.
- **Selection:** `GetScenePaths`, `DescribeSceneOrder`, `TryParseProfile`, and `ResolveProfile` resolve profiles; batch selection uses `-vrProfile` with `VR_WEBGL_PROFILE` as lower-precedence fallback, defaulting to Prototype. Unknown tokens fail closed. Preflight, build, JSON report, and the build window validate/report against the selected profile, including `profile`/`profileScenes` fields in the report.
- **Editor development affordance:** `PrototypeSceneLoader.SelectContentScene` prefers TestTrack when both scenes are listed and falls back to HandlingCourse. For editor play mode, a scene absent from build settings is loaded via `EditorSceneManager.LoadSceneAsyncInPlayMode`; player builds still require the selected profile to list the scene.

### 47.10 Stage-3 profiling, budget derivation, and quality review (T-7.14–T-7.17, 2026-11-11)

> Status: **T-7.14/T-7.15 executed against a real VerticalSlice WebGL build and a real Chromium/WebGL 2.0 runtime capture; T-7.16 draft criteria applied; T-7.17 compile/tests green.** The baked-lighting row is **explicitly waived for M5** by the owner under D-048 (2026-11-11): no `LightingData`, lightmap, or reflection-probe data exists, and no bake is claimed anywhere in this section. M5 and Phase 7 are recorded **COMPLETE** on that basis; a verified bake moves to Phases 8–9 production, where OQ-A10 remains open.

**T-7.14 build matrix (real, this project):**

| Item | Value |
|---|---|
| Profile / scenes | `WebGLBuildProfile.VerticalSlice` — `Bootstrap` (0) → `HandlingCourse` (1) |
| Build options | WebGL Development; Built-in Render Pipeline; `-vrProfile slice` batch entry |
| Result / duration | Succeeded, **62.882 s** cold; 0 errors, 1 emcc JS-library advisory warning (+4 WebGPU/Firefox advisory lines) |
| Payload | **60,995,033 B (60.995 MB / 58.17 MiB)** |
| Artifacts | wasm 53,516,979 B; data 6,699,979 B; framework.js 694,835 B; loader.js 60,309 B; index.html 8,817 B |
| Build-scene handling | applied through the explicit menu action for the build, then restored to `Bootstrap → TestTrack`; `EditorBuildSettings` is unchanged in the committed tree |
| Report | `velocity-rush-webgl-build-report.json` in the output folder (outside the repository) |

**T-7.14 runtime matrix (real Chromium 152.0.7977.167 / WebGL 2.0, ANGLE AMD REMBRANDT, OpenGL ES 3.2):**

| Measure | Value |
|---|---|
| Page load / first frame | 1,578 ms / 1,602 ms |
| Game startup / asset load / WASM startup | 107 ms / 1,423 ms / 1,426.6 ms |
| WASM heap | **137,887,744 B total (131.50 MiB)**; 123,418,064 B used (117.70 MiB) |
| JS heap | 16,734,431 B total (15.96 MiB); 16,398,039 B used (15.64 MiB) |
| Probe window | 590 frames (run 1) / 589 frames (run 2), 3 s warm-up + 10 s sample |
| Frame time | mean **16.95 ms**, p95 **18.00 ms**, min 9 ms, max **35 ms** (run 1); 17.00 / 18.00 / 11 / 35 (run 2) |
| FPS | mean **59.0** (run 1), 58.8 (run 2); single-frame min 28.6 / max 111.1 |
| Draw calls / triangles / vertices | **394** / 115,798 / 152,904 |
| Managed memory / delta | 1.93 MB; 212 KB over 590 frames (≈0.36 KB/frame); run 2: 1.92 MB, 192 KB / 589 frames |
| Janked frames | 11 cumulative (includes startup); no GC hitch >35 ms in the sample |
| Console | no game/Unity/WebGL startup error; the one error line was a test-injected `HEAPU8` access, not the build |

**T-7.15 proposed budgets (derived, not invented):** payload **≤250 MB** (warn 225 MB); WASM heap **≤192 MiB total** (warn 176 MiB; used ceiling 168 MiB); JS heap **≤64 MB**; draw calls **≤500** (warn 450); triangles **≤180k**; vertices **≤250k**; mean **≥58 fps**, p95 **≤20 ms**, max **≤50 ms**; page-to-first-frame **≤5 s**; asset load **≤3 s**; managed steady-state **≤1 KB/frame** with no GC hitch >50 ms. Full derivations are in `PLAYGAMA_CONSTRAINTS.md` §5. These are candidate budgets for owner sign-off; the desktop Chrome/Firefox matrix (OQ-P01) and repeated-cycle memory growth remain open.

**T-7.16 draft-criteria review (OQ-A08/OQ-A09):**

| Row | Result | Evidence / limit |
|---|---|---|
| Vehicle | Pass against the draft | Hero kit reads as a Sport-class car at gameplay distance; material separation and exact collider parity; captured at 960×600 in the slice scene. Production mesh/LOD remains Phase 8. |
| Environment | Pass against the draft | Desert identity, road edge/barrier/gantry/rock landmarks readable; non-colliding decor; no repeated-kit monotony inside the section. |
| Materials | Pass against the draft | Asphalt, sand, metal, hazard, hero paint, dark trim, rubber, chrome, lamps, and VFX materials are distinct; generated textures; no magenta/error shader. |
| Lighting | **Waived for M5 (D-048)** | Procedural skybox, skybox ambient, warm mixed directional light, and linear fog are wired and visible in the capture, and a two-height probe grid is configured; **no baked lightmap/reflection-probe data is present**, so the baked-indirect criterion is explicitly waived for M5 rather than passed. No bake is claimed. |
| UI | Pass against the draft | HUD/countdown/pause/results are legible in the 960×600 capture; results bind once; numeric HUD path is 0 B/frame by EditMode guard. 1280×720/1920×1080 captures were not produced in this run. |
| VFX | Pass with a stated limit | Smoke/dust/sparks are capped and allocation-bounded; the capture was stationary, so at-speed overdraw was not visually stress-tested. |
| Audio | Partial — implementation audit only | Synthesized engine/tyre/impact/UI clips, pooled sources, `ignoreListenerPause` UI pool, and pure response math are implemented and tested; no listening pass was possible in this headless capture. |
| Performance | Pass against the proposed budgets | 59.0 mean FPS, p95 18 ms, max 35 ms, 394 draw calls, 131.5 MiB WASM heap on the local Chromium capture. |

**No silent downgrade:** every deviation (bake, audio listening, at-speed VFX, 1080p/720p captures, repeated-cycle memory, owner desktop matrix) is explicit above and in `OPEN_QUESTIONS.md`; no quality reduction was made to fit a budget, and no budget was invented.

**T-7.17 closeout evidence:** final batch compile **0 errors / 0 warnings**; EditMode **241/241**; PlayMode **13/13** (0 failed, 0 skipped); `EditorBuildSettings` restored to `Bootstrap` (0) → `TestTrack` (1). The slice build above is the first real WebGL slice build and is not committed as a binary artifact. **M5/Phase 7 are COMPLETE (2026-11-11) under D-048**: the baked-indirect criterion is waived for M5, no bake is claimed, and the realtime-only lighting route is the accepted M5 evidence.

### 47.11 Phase-8 Stage-4 Garage foundation (UI Toolkit, 2026-11-11)

The Garage foundation is a UI-only demonstrator: browse the ten vehicles, read class and profile-derived stats, select, and confirm. Progression, unlocks, upgrades, ownership, save, and the final Garage screens remain Phase 11 (OQ-07/OQ-09/OQ-A03).

**Architecture**

| Type | Responsibility |
|---|---|
| `GarageVehicleSummary` | Immutable struct projection of one `VehicleDefinition` (id, en/ar names, class, mass, torque, forward/sideways grip, brake bias, steering angle). Copies references only; never a second authored stat source. |
| `GarageCatalogModel` | Fixed `GarageVehicleSummary[]`, `SelectedIndex`, `ConfirmedIndex`, wrap-around `Next`/`Previous`, validated `Select`, `Confirm`. Pure C#, no events or LINQ. |
| `IGarageView` | Presentation seam: `Refresh(in summary, index, count)` and `ShowConfirmation(in summary)`. |
| `GarageView` | Builds the UI Toolkit tree once: header, ten cached rows, ten cached detail panels, three named buttons (`GaragePrevious`/`GarageNext`/`GarageConfirm`), and a cached confirmation label. |
| `GaragePresenter` | Maps model navigation/confirmation to the view; plain C# so the hot path is measurable in EditMode and PlayMode. |
| `GarageScreen` | Scene adapter: resolves the `UIDocument` root and `VehicleRosterCatalog`, builds the model/view/presenter once, and forwards keyboard/button intents. |

**Zero-allocation design (T-8.10).** All strings — row names, class labels, stat values, and confirmation text — are constructed once in the `GarageView` constructor. Navigation calls only toggle `DisplayStyle` on cached panels and assign two cached `Color` values to the previously/currently selected row. There is no interpolation, `ToString`, lookup, or tree rebuild on the path.

**Measured evidence:**

| Guard | Measurement |
|---|---|
| `GarageViewAllocationTests.RepeatedSelection_AfterWarmup_AllocatesZeroBytes` (EditMode) | **0 B** over 600 selections after 240 warm-up iterations |
| `GaragePlayModeTests.RepeatedSelection_InPlayMode_AllocatesZeroBytes` (PlayMode, generated scene) | **0 B** over 600 selections after 240 warm-up iterations |

The measurement covers the synchronous navigation path used by a key press or button click. UI Toolkit's own panel layout/repaint work happens outside that block and is not attributed to roster navigation.

**Scene and build contract.** `GarageSceneBuilder` generates `Assets/Scenes/Garage.unity` with a camera, a `UIDocument` using the existing `Assets/UI/Slice/SlicePanelSettings.asset`, the roster catalog, and `GarageScreen`. It deliberately does not modify `EditorBuildSettings`; PlayMode tests load the scene through `EditorSceneManager.LoadSceneAsyncInPlayMode`, and the approved `Bootstrap → TestTrack` prototype contract is unchanged.

**Validation:** batch compile **0 errors / 0 warnings**; EditMode **299/299**; PlayMode **16/16**; new Stage-3/4 EditMode coverage is 11 tests (5 rig, 4 model, 2 view/allocation) and Stage-4 PlayMode coverage is 2 tests.

### 47.12 Phase-8 Stage-5 debug spawn and repeated-cycle profile (T-8.11–T-8.13, 2026-11-11)

**Selection contract.** `SelectionContext` (`Assets/Scripts/Core/SelectionContext.cs`) is the TDD §34.3 session holder. It stores only the stable `VehicleId` string; no `UnityEngine.Object`, prefab, profile, or collection is retained. The Garage writes on confirm through `GarageScreen.ConfirmSelection()`; the debug spawner only reads. Phase 11 replaces the write site with the GameFlow transition and can delete the debug seam without touching the contract.

**Definition-to-prefab mapping.** `VehicleRigCatalog` (`Assets/Scripts/Data/VehicleRigCatalog.cs`) holds one `(VehicleDefinition, GameObject rigPrefab)` pair per roster entry. `HandlingCourseBuilder` generates it from `VehicleRigBuilder.GetVariantPath(definition)`, then validates: at least one entry, valid definition, non-null prefab, unique stable ids. Runtime resolution is an ordinal id comparison over a scene-authored array; there is no `Resources`/Addressables load and no new loading-strategy decision (D-035 stays open).

**Debug spawn flow.** `VehicleSelectionSpawner` runs in `Start`, after `RaceController.Awake` captured the template body. With a selection it:

1. instantiates the matching rig variant under an inactive holder;
2. binds input (`TrySetInputSource`) and the definition's physics profile (`TryApplyProfile`) before activation, so `VehicleController.Awake` sees a complete rig;
3. disables the template, transfers `Engine Audio`, `Tyre Audio`, `Vfx Tyre Smoke`, `Vfx Ground Dust`, and `Vfx Impact Sparks`, adds a runtime `VehicleVfxController` and `ImpactAudioController`, and rebinds every telemetry consumer;
4. activates the selected rig, rebinds `VehicleCameraRig`, `SliceHudPresenter`, `VehicleTelemetryOverlay`, and `RaceController` (participant body + retry pose), then destroys the template.

Without a selection the spawner returns before touching the scene, so the approved slice template and every existing slice PlayMode suite remain valid. `GarageDebugLauncher` (Garage → course) and the spawner's Escape return (course → Garage) are wrapped in `UNITY_EDITOR || DEVELOPMENT_BUILD` and are not the GameFlow.

**WebGL build contract.** `WebGLBuildProfile.Phase8Debug` adds `Garage → HandlingCourse` as opt-in data; the default remains `Bootstrap → TestTrack`. Batch helpers `ApplyPhase8DebugScenesFromCommandLine` and `RestorePrototypeScenesFromCommandLine` make the build reproducible, and the committed `EditorBuildSettings` is restored to the prototype order afterwards.

**Measured build (real, Unity 6000.6.8f1, WebGL Development):** `61,186,251 B = 61.19 MB` five-artifact payload, `34.0 s`, `0 errors`, 4 known emcc/Firefox advisory warnings. Details and the simulated 10-cycle memory table are in `docs/PLAYGAMA_CONSTRAINTS.md` §5.

**Validation:** batch compile **0 errors / 0 warnings**; EditMode **304/304**; PlayMode **19/19**; 0 failed, 0 skipped. New coverage: 4 `SelectionContextTests`, 1 `WebGLBuildProfileTests` Phase-8 profile test, 1 `GaragePlayModeTests` confirmation-write test, and 2 `VehicleSelectionSpawnerPlayModeTests` (selected spawn + no-selection fallback).

## 48. Phase 9 — Environment production (Stages 1–3, 2026-11-16)

**Scope and governance.** Stages 1–3 implement the data contracts, shared kits, five authored packages, and five generated signature scenes for the owner-approved environments. Stage 0 resolutions are recorded in `DECISIONS.md` D-054–D-057: production order Desert → Coastal → Mountain Pass → Neon City → World Arena, one signature plus one reverse configuration per theme, internal-only provenance, realtime-only lighting, one scene per environment with additive loading, candidate budgets, and presentation-only grip/hazards. Phase 8 vehicle physics is untouched (D-052 remains the approved balance). Stage 1 built the foundation only; Stages 2–3 author the five `EnvironmentDefinition`/`TrackLayout`/`EnvironmentLightingProfile` packages and all five scenes.

### 48.1 Environment data contracts (T-9.1)

| Type | Responsibility |
|---|---|
| `EnvironmentTheme` / `EnvironmentThemeRules` | Five stable enum values in production order, display names, stable id fragments, production order, and theme-set validation. |
| `EnvironmentPalette` | Inline serializable palette for road, edge, ground primary/secondary, barrier, accent, and emissive colours. Validation rejects non-finite/out-of-range channels and enforces a relative-luminance delta of at least **0.08** between road and edge. |
| `EnvironmentDefinition` | ScriptableObject identity asset: schema version, stable lowercase id, en/ar display names, theme, approved production order, palette, `EnvironmentLightingProfile` reference, and `TrackLayout` reference. It carries **no** grip, hazard, damage, or vehicle-physics data. Validation also requires the id to start with the theme fragment (for example `desert-`). |
| `EnvironmentCatalog` / `EnvironmentCatalogRules` | Scene-authored reference set with exactly five definitions, one per theme, unique ids, unique production order 1–5, and per-entry validation. `FindById`/`FindByTheme` are ordinal/lookup helpers, not a second source of truth. |
| `EnvironmentSceneContext` | Scene-local reference to the definition a generated scene represents; used by the builder audit. |

### 48.2 Data-driven track layouts (T-9.2)

`TrackLayout` is a runtime-safe ScriptableObject that generalizes the prototype `HandlingCourseLayout` without changing it. It owns:

- `TrackSegment` (`Straight` or `Arc`) with derived arc length, radius, sweep, and ±1 turn direction;
- optional `TrackElevationKey` entries on the signature-path distance axis;
- road width, barrier height/thickness, sample spacing, and closed-loop flag;
- validation for segment counts, lengths, arc bounds, road/barrier dimensions, spacing, ascending elevation keys, first-key-at-zero, keys inside the layout, and optional closure.

`BuildCenterline(spacing, variant)` samples the planar centerline, applies the smooth-step elevation profile, and reverses point order for the approved reverse configuration. `SampleAtDistance` interpolates position and tangent for spawning/decoration. The math is deterministic, allocation-cold, and shared by the builder and tests.

### 48.3 Shared environment kit (T-9.3)

`EnvironmentMaterialKit` creates seven palette-driven Built-in `Standard` materials per environment (road surface, road edge, ground primary, ground secondary, barrier, accent, emissive) under `Assets/Generated/Environments/<id>/Materials`. No texture or external asset is used. `EnvironmentKit` builds the road/barrier/edge-line meshes, a non-track ground plane, a presentation-only road skirt, and deterministic theme dressing:

| Theme | Static dressing language |
|---|---|
| Desert | rocks, cactus primitives, warm ground material, mesa landmarks |
| Coastal | palm trunks/fronds, rocks, coastal accent, lighthouse/sea markers, two water planes |
| Mountain Pass | layered pine canopies, boulders, snow caps, cliff-wall landmarks |
| Neon City | tower blocks, emissive signage strips, light poles, neon spires, emissive lane markers |
| World Arena | grandstand blocks, accent banding, floodlight towers, start arch/grandstand landmarks |

Every decoration and landmark is a primitive with its collider removed and static flags that exclude `ContributeGI`; water planes and lane markers are likewise collider-free. Only `Track/Road`, `Track/Barriers`, and `Track/Ground` carry colliders, plus the gate trigger boxes. The kit therefore cannot change handling and has no lightmap/UV2 dependency. Grip and hazards remain presentation-only per D-057.

### 48.4 Realtime lighting (T-9.4)

`EnvironmentLightingProfile` is a ScriptableObject with schema validation and a pure `BuildSettings()` projection into `EnvironmentLightingSettings` (sky tint, ground colour, atmosphere, sun size/convergence, exposure, ambient intensity, directional colour/intensity/Euler, shadow toggle/strength, fog toggle/colour/start/end). `EnvironmentLightingBuilder` creates the procedural `Skybox/Procedural` material, applies skybox ambient, creates exactly one `LightmapBakeType.Realtime` directional light, applies linear fog, and explicitly disables baked/realtime GI and lighting settings. There is no bake, lightmap, lighting-data, reflection-probe, or post-processing dependency (D-056).

### 48.5 Scene-builder template and audit (T-9.5)

`EnvironmentSceneBuilder` is parameterized by one `EnvironmentDefinition`:

1. validate the definition and reject closed-loop layouts (the approved race format is open point-to-point);
2. generate/refresh the per-theme material set, shared slice materials, audio clips, and tuning presets;
3. create a new empty scene and an `Environment` root with `EnvironmentSceneContext`;
4. build the shared-kit `Track` (road, barriers, edge lines, skirt, ground, decor, landmarks, water, lane markers);
5. apply the realtime lighting profile;
6. create the vehicle, input, VFX, engine/tyre/impact audio, camera rig, race controller, ordered gates, pause driver, UI audio, HUD/`UIDocument`, telemetry overlay, performance probe, roster/rig catalogs, selection spawner, and the five-definition catalog when valid;
7. audit the scene, save it under `Assets/Scenes/Environments/<environmentId>.unity`, and log the report.

The audit is fail-closed: it rejects missing scripts, a missing/duplicate context, an invalid context reference, no camera/AudioListener, anything other than exactly one realtime directional light, a missing skybox/sun, an unwired `Track/Road`, unassigned serialized references on the race stack, an invalid checkpoint set, decorative colliders, or a gate without exactly one trigger collider. `AuditRoots` exposes the same contract to EditMode tests without creating or saving production scenes. It never edits `EditorBuildSettings`.

**Batch entry:** `-executeMethod VelocityRush.EditorTools.EnvironmentContentBuilder.BuildFromCommandLine` authors all five packages and builds every scene; `EnvironmentSceneBuilder.BuildFromCommandLine -environmentId <stable-id>` builds one. Both exit `1` on any catalog, distinctness, race, or audit failure.

### 48.6 Validation tooling (T-9.1)

`EnvironmentCatalogBuilder` validates `Assets/Data/Environments` through the menu `Tools/GameCraft Studio/Velocity Rush/Validate Environment Catalog` and the batch entry `-executeMethod VelocityRush.EditorTools.EnvironmentCatalogBuilder.ValidateFromCommandLine`. An empty folder intentionally fails because five themes are required; `CreateDefinition(...)` validates before writing and derives the production order from the approved theme. No content is invented or generated by Stage 1.

### 48.7 Stage-1 evidence and non-goals

**Validation:** batch compile **0 errors / 0 warnings**; EditMode **387/387** (304 baseline + 83 new), 0 failed, 0 skipped. New coverage: 21 definition/schema, 11 catalog, 25 layout/geometry, 19 lighting-profile, and 7 scene-builder validation/audit tests.

**Non-goals preserved in Stage 1:** no environment definition assets, material assets, meshes, or scenes were generated; no build-settings change; no gameplay/race integration; no Addressables/Resources decision; no bake; no vehicle physics, grip, hazard-physics, or per-surface handling change. Stages 2–3 supersede the content non-goals; Phase 11 still owns additive GameFlow integration.

### 48.8 Stage 2 authored packages (T-9.6–T-9.10)

`EnvironmentContentBuilder` is the deterministic batch/menu authoring entry point. It writes each package under `Assets/Data/Environments/<Theme>/<id>/` and refreshes existing assets in place, preserving GUIDs, then validates the catalog, theme distinctness, and race rules before generating scenes.

| Environment | Stable id | Segments | Length | Road | Character |
|---|---|---:|---:|---:|---|
| Desert Circuit | `desert-signature` | 7 | 713.7 m | 12 m | wide sweepers, long straights, midday warm haze, mesas |
| Coastal Circuit | `coastal-signature` | 9 | 681.6 m | 11 m | flowing sweepers, long sightlines, late afternoon/sea haze, lighthouse, water |
| Mountain Pass | `mountain-pass-signature` | 9 | 507.5 m | 8 m | narrow sections, blind crests, cliff edges, dawn valley fog, 22 m elevation |
| Neon City | `neon-city-signature` | 13 | 485.1 m | 9 m | tight technical corners, night, emissive signage, road contrast |
| World Arena | `world-arena-signature` | 9 | 606.8 m | 10 m | mixed speed, close walls, stadium night, arch/grandstands |

Each package has one signature configuration and shares its definition/layout with the approved reverse sampling; no second catalog entry or scene is created (D-057). Layouts are open point-to-point (`closedLoop = false`). `EnvironmentDistinctnessRules` enforces minimum pairwise road-width and length separation plus palette/sky/fog separation; `EnvironmentRaceRules` owns the three-gate plus finish format.

### 48.9 Stage 3 scenes, race wiring, and validation (T-9.11–T-9.13)

All five scenes exist at `Assets/Scenes/Environments/<id>.unity` and are wired identically:

- **Vehicle:** prototype prefab instance, approved tuning profile, `LegacyKeyboardInputAdapter`, hero kit, `VehicleVfxController`, engine/tyre loops, and impact audio.
- **Start pose:** the player, camera, `SpawnPoint`, and `Start Line` are placed `PlayerStartSetback = 3.5 m` along the centerline, not at distance 0. The road ribbon begins exactly at distance 0, so a car centred there hangs its 1.3 m rear axle and body overhang over the road edge behind the 0.45 m `Skirt` face; the setback keeps the whole wheelbase on the road. `AuditStartClearance` enforces the pose per variant (see §52.6).
- **Camera:** one `Main Camera` with `VehicleCameraRig` bound to the vehicle and baseline profile, plus one `AudioListener`.
- **Race:** `RaceController` with three ordered `CheckpointTrigger`s at 1/4, 1/2, 3/4 of total length and a `FinishTrigger` at `TotalLength - 1`; each gate has a visible gantry and exactly one trigger `BoxCollider`. Gates are placed on the sampled centerline within 1 m.
- **UI/audio:** `SlicePauseDriver`, `SliceUiAudioController`, `SliceHudPresenter` + `UIDocument`, telemetry overlay, and performance probe.
- **Spawning:** roster catalog, rig catalog, and `VehicleSelectionSpawner` bound to the template vehicle, race, camera, HUD, and overlay.
- **Catalog:** the validated five-definition `EnvironmentCatalog` reference set.

**Scene evidence (2026-11-16):** each build logs `missingScripts=0 | missingReferences=0`, `checkpoints=3`, `hud=1`, `audio=4`, `vfx=1`, `spawner=1`, `directionalLights=1`, `cameras=1`. Decor/landmark/water/lane-marker counts: Desert 98/4/0/0, Coastal 102/2/2/0, Mountain 137/12/0/0, Neon 212/21/0/53, World Arena 216/7/0/0. Environment materials are **7 per scene** (≤8). Decorative content is collider-free; only road, barriers, ground, and gate triggers collide.

**Validation:** batch compile **0 errors / 0 warnings** (only the pre-existing `UAC0009` advisory); EditMode **397/397**; PlayMode **21/21**; 0 failed, 0 skipped. New coverage includes catalog/distinctness/race rules, generated material/mesh/skybox presence, scene existence, full fail-closed audits, Coastal water, Neon lane markers, centerline gate placement, and PlayMode additive load/unload of all five scenes.

> Generation note: authoring and scene builds ran with `-buildTarget StandaloneLinux64`. WebGL audio import on the build machine fails without `ffmpeg`/`avconv` (`FSBTool ERROR`), producing null `AudioClip` references; Standalone imports PCM successfully. This is a local tooling constraint, not a shipped project setting.

## 49. Phase 10 — AI opponent pathing foundation (Stage 1, 2026-11-16)

### 49.1 Approved scope and architecture

Phase 10 carries two ordered workstreams: **M8a — AI Opponents Complete** (executed first) and **M8 — Championship & Event Content Complete** (D-058). The Stage-0 owner gate resolved the AI architecture:

| Gate | Resolution | Decision |
|---|---|---|
| Scope / milestone | Add M8a before M8; racing opponents are not D-020 "advanced AI traffic" | D-058 |
| Physics fidelity | **Kinematic AI only**; the player remains the single WheelCollider vehicle (D-033/S3) | D-059 |
| Waypoints | Procedural from `TrackLayout` at 2.0 m spacing; no authored waypoint assets | D-060 |
| Grid | Default **5 cars** (1 player + 4 AI); hard cap **7** | D-060 |
| Interaction | Soft push; no AI rigidbody | D-061 |
| Difficulty | Bounded **±10%** rubber-band on target speed only; no teleport/input cheating | D-061 |
| Ranking | Checkpoint count, then distance to the next gate; frozen at finish | D-061 |
| Results | New `RaceStandings` alongside the unchanged solo `RaceResult` | D-061 |
| Start | Synchronized grid hold until GO | D-061 |

`VehicleController` and `IVehicleInput` are unchanged. Stage 2 will implement `AIVehicleInput : IVehicleInput` and a kinematic driver; Stage 1 delivers only the path data and sampling.

### 49.2 `TrackPath` generation

`Assets/Scripts/Data/TrackPath.cs` owns `PathWaypoint`, `PathSample`, `TrackPathBuildSettings`, `TrackPath`, and `TrackPathCursor`.

`TrackPath.Create(layout, variant, settings)` runs once at load and performs the following, in order:

1. **Validate** the settings envelope and the `TrackLayout` (fail loudly; no silent defaults).
2. **Sample** the authoritative centerline with `TrackLayout.BuildCenterline(2.0 m, variant)`. The same array already drives road/barrier meshes, checkpoints, and gates, so the path cannot drift from the geometry; the reverse configuration is free.
3. **Collapse the closed-loop duplicate** when the final sample is within half a spacing of the origin, so the seam is represented once by the wrap segment.
4. **Cumulative planar distances** per waypoint, plus per-segment lengths including the wrap segment for loops. Planar distance matches `TrackLayout.TotalLength` and the race-gate distance axis.
5. **Tangents** from the previous/next sample (wrapped for loops), and **right vectors** as `Cross(up, tangent)`.
6. **Signed Menger curvature** `κ = 2·cross / (|ab|·|bc|·|ca|)` in the XZ plane. Positive is a right turn, matching `TrackSegment.TurnDirection` (+1 = right). No trig approximation is used at runtime.
7. **Speed profile**: initial corner limit `v = sqrt(a_lat / |κ|)` clamped to `[MinSpeed, MaxSpeed]`, then a forward acceleration pass and a backward braking pass over the segment graph (two passes each for loops). This prevents a car from "arriving too fast" into a corner it could not brake for.
8. **Racing line**: `offset = sign(κ) · min(maxOffset, halfWidth · strength) · clamp01(|κ| · referenceRadius)` — toward the inside of the turn, clamped inside the road. Per-opponent lane bias is applied later at sample time, so the grid shares one path.

`TrackPathBuildSettings.Default` is an **engineering envelope, not approved race balance**: 2.0 m spacing, 58 m/s ceiling, 5 m/s floor, 14 m/s² lateral, 7/14 m/s² forward/braking, 0.5 half-width racing strength saturated at 40 m radius, 1.2 m edge margin. Stage 2/3 may tune it; changing the grid, physics model, or rubber-band bounds outside D-060/D-061 requires a new decision.

### 49.3 Sampling API and the 0 B/frame contract

- `TrackPath.IndexForDistance(distance)` — O(log n) binary search; never allocates.
- `TrackPath.SampleAtDistance(distance, lateralBias)` — normalizes/wraps the distance, interpolates center, tangent, curvature, speed, and racing offset, applies and clamps the lateral bias, and returns `PathSample` by value.
- `TrackPathCursor` — a struct holding a path reference, index, and distance. `Reset`, `Advance` (monotonic, wraps for loops, clamps for open paths), `TrySample`, and `TrySampleAhead` (steering lookahead without moving the cursor) allocate nothing.
- `TrackPath.WaypointAt(index)` — inspection/test helper; wraps indices for loops.

**Banned in this namespace and in all future AI code:** LINQ, `foreach` over `List<T>` in hot paths, per-frame string formatting/logging, closures/lambdas in the fixed step, per-frame `GetComponent`/`Find*`, and allocating physics queries. Generation allocates only inside `Create` and retains seven value arrays; it is a documented load-time cost, analogous to `BuildCenterline`.

### 49.4 Closed-loop and reverse behavior

- **Closed loop:** total length includes the closing segment; `NormalizeDistance` wraps; cursor advance wraps; sampling across the seam interpolates from the last waypoint to the first; the speed profile traverses the seam. A closed layout is validated by `TrackLayout` (sampled gap ≤ 2 m) before generation.
- **Open path:** sampling and cursor advance clamp at both ends; the final waypoint is returned exactly at `TotalLength`.
- **Reverse:** generated from the same asset with `EnvironmentLayoutVariant.Reverse`; distances are recomputed from the new start and the geometry is the same physical track in reverse order. No second path asset exists.

### 49.5 Validation evidence and non-goals

**EditMode `TrackPathTests`: 22 tests.** Coverage: waypoint ordering/spacing; zero curvature and maximum speed on straights; right/left curvature sign and magnitude at a 20 m arc; racing-line direction and road bounds; lateral-bias clamping; tight-corner deceleration; speed-envelope bounds; open-path clamping; closed-loop duplicate collapse, sampling wrap, and cursor wrap; reverse order/length; exact determinism across two builds; null layout and invalid settings; all **five authored environments in both signature and reverse** configurations; on-road sampling; and a **0 B** allocation assertion over 5,000 cursor/sample iterations after warm-up.

**Suites:** batch compile **0 errors / 0 warnings**; EditMode **420/420** (398 baseline + 22); PlayMode **22/22**; 0 failed, 0 skipped.

**Non-goals preserved in Stage 1:** no AI controller, input adapter, visual rig, grid spawn, race-position logic, standings, rubber-banding, scene change, build-profile change, asset generation, or `WheelCollider` work. Stage 2 was subsequently authorized and is recorded in §49.7. No AI performance budget is claimed; Stage 4 owns the AI matrix row in `PLAYGAMA_CONSTRAINTS.md` §5.

### 49.6 Grid-size cost evidence (T-10.0.3, 2026-11-16)

`Assets/Editor/VehicleCostProbe.cs` measured the ten committed rig prefabs (real structural data, no simulation):

| Class | Vehicles | Renderers / draw calls | Triangles | Materials |
|---|---|---:|---:|---:|
| Starter | 2 | 15 | 452 | 6 |
| Sport | 3 | 16 | 464 | 6 |
| Drift | 2 | 17 | 476 | 6 |
| Supercar | 3 | 18 | 488 | 6 |
| **Worst** | — | **18** | **488** | **6** |

Against the Phase-9 worst case (373 draw calls, 95,314 visible triangles), the headroom supports **7 additional cars by draw calls** (127 ÷ 18) and **173 by triangles**, so the approved **default 4 AI** and **cap 6 AI** pass with margin. At the 7-car cap, the 6 AI cars add 108 main-pass draw calls (373 → 481/500) and 2,928 triangles (95,314 → 98,242/180,000).

**Shadow constraint:** AI cars must use blob/contact shadows rather than realtime shadow casting. Six realtime casters would add a second 108-draw pass and breach the 500 ceiling at the cap. This is an engineering constraint inside the existing budget, not a new design decision.

### 49.7 Stage 2 — Kinematic controller, visuals, rubber-banding and soft push (2026-11-16)

Stage 2 implements D-059's kinematic actor. `VehicleController` and `IVehicleInput` are unchanged; the player remains the only `WheelCollider` vehicle.

**`AIDriver`** (`Assets/Scripts/Gameplay/AI/AIDriver.cs`)

- `Initialize(path, startDistance, lateralBias, playerTarget, startSpeed)` binds a validated `TrackPath`, clamps the lateral bias to `MaxLateralOffset`, caches the optional player transform, and applies the initial pose. `ResetTo` supports a future grid reset without rebuilding the path.
- `Tick(dt)` is the single deterministic step and is also called from `FixedUpdate`. It samples the current and lookahead points, shapes the target speed, integrates speed with the path's forward/braking budgets, computes steering from the lookahead lateral error, advances the cursor, resolves soft push, and writes the pose and visual state. Non-positive `dt` is ignored.
- **Speed shaping:** `target = min(current, ahead).TargetSpeed × paceScale × rubberBandScale`, clamped to the path ceiling. Corner speed therefore comes from the Stage-1 curvature profile, not a second handling model.
- **Steering:** the lookahead point is expressed in the current sample's tangent/right frame; `atan2(lateral, forward)` is divided by the configured maximum steering angle and clamped to −1..1. Positive is right, matching `IVehicleInput`.
- **Pose:** yaw follows the path tangent with zero roll; body roll/pitch live on the visual chassis only. Wheels ride at a configurable `rideHeight`.

**`AIVehicleInput`** (`Assets/Scripts/Gameplay/AI/AIVehicleInput.cs`)

Implements `IVehicleInput` and mirrors the driver's normalized throttle/brake/steering; `Handbrake` is always false. It is a pure adapter with no computation or allocation, so a future physics-backed opponent could bind it to a `VehicleController` without changing the AI contract.

**`AIVisualRig`** (`Assets/Scripts/Gameplay/AI/AIVisualRig.cs`)

- Resolves `Wheel FL/FR/RL/RR Visual` and `ChassisBody`, caches their base rotations, and applies wheel spin (`speed / radius`), front-axle steering, and smoothed body roll/pitch from lateral/longitudinal acceleration.
- Removes inherited physics: disables every `WheelCollider`, sets the `Rigidbody` kinematic with gravity off, converts non-wheel colliders to query-only triggers, and disables the inherited `VehicleController`. No solver contact ever resolves against an AI car.
- Enforces the T-10.0.3 draw-call constraint: every child renderer is set to `ShadowCastingMode.Off` and `receiveShadows = false`, and exactly one procedural, unlit, ground-aligned blob shadow is created. Its mesh, texture, and material are runtime assets released in `OnDestroy`.

**Bounded rubber-banding (D-061)**

The player's position is projected onto the path at `playerProjectionInterval` (default 0.25 s) with a coarse stride-8 scan followed by a ±8-waypoint refinement. The signed delta is wrapped to the shorter direction on loops and converted to `scale = 1 + 0.005 × delta`, hard-clamped to **0.9–1.1**. The multiplier applies to target speed only: there is no teleport, no position write, no input override, and no path-ceiling bypass. With no player bound the scale is exactly 1. The projection is deterministic and allocation-free.

**Soft push (D-061)**

`Physics.OverlapCapsuleNonAlloc` is called with a preallocated 8-collider buffer and `QueryTriggerInteraction.Collide` so AI trigger colliders are visible to the query. Separation uses the AI's **actual world pose** (not its desired path point) so two cars at the same point receive opposite tie-break directions. The AI gains a capped (1.5 m) offset that decays at 3 m/s; a slow/stationary non-AI body receives a bounded velocity nudge capped at 0.6 m/s. The player's own input and speed are never cancelled. Because transforms are consumed by the physics scene one step behind, production separation has the same one-step latency the engine gives any query issued inside `FixedUpdate`.

**Validation**

- EditMode `AIDriverTests`: **19 tests** — null/limit guards, path advance, command ranges, straight-line steering, open-path stop, loop wrap and travelled distance, neutral/upper/lower rubber-band bounds, bounds across every tick, `Tick` **0 B/5,000**, adapter mirroring and neutral fallback, production-prefab shadow/wheel/rigidbody/trigger enforcement, one blob shadow, wheel spin/steer, and `Apply` **0 B/5,000**.
- PlayMode `AIDriverPlayModeTests`: **5 tests** — full lap without stalling or leaving the road, **0 B/5,000 ticks**, two cars spawned on the same point separating with bounded offsets, a capped player nudge, and a real production rig with no realtime shadow casters.
- Batch compile **0 errors / 0 warnings**; EditMode **439/439** (420 + 19); PlayMode **27/27** (22 + 5); 0 failed, 0 skipped.

**T-10.2.7 (full-physics A/B) is skipped** by owner direction: D-059 already commits V1 to kinematic AI, so the A/B would benchmark a rejected architecture. A future full-physics AI requires a new decision and a measured dev-only benchmark per D-059.

**Non-goals preserved in Stage 2:** no grid spawn, synchronized start, checkpoint/position tracking, `RaceStandings`, result-schema change, scene/build-profile change, camera change, player-physics change, or authored waypoint asset. Stage 3 owns grid and standings.

### 49.8 Stage 3 — Grid, synchronized start, standings and multi-car results (2026-11-16)

Stage 3 implements the D-060 grid and the D-061 ranking/start contract. It is **additive**: `RaceResult`, the solo `RaceController` flow, `VehicleController`, every scene, and every build profile are unchanged. A scene without `RaceGridController` behaves exactly as it did in Stages 1–2.

**`RaceGridLayout`** (`Assets/Scripts/Data/RaceGridLayout.cs`)

- `RaceGridSettings` holds row spacing (6 m), lateral spacing (2.4 m), vehicle half-width (0.9 m), vehicle half-length (2.2 m), the lateral margin (0.35 m), and a validation envelope that rejects spacing values that would overlap cars.
- `RaceGridSlot` exposes participant order, row, column, path distance, lateral offset, world position, tangent, and rotation.
- `RaceGridLayout.TryCreate(path, startLine, participantCount, settings, destination, out count, out error)` places participants in two-column rows behind the start line by path distance and side offset, centers the odd final car, and validates the whole field before returning. Slots must be on-road (lateral offset + vehicle half-width inside the path half-width) and pairwise non-overlapping; the first row must fit behind the line on an open path; closed loops wrap the row distance across the seam. The participant range is **1..7** (D-060).

**`RaceStandings`** (`Assets/Scripts/Gameplay/Race/RaceStandings.cs`)

- Fixed arrays sized to `MaxParticipants = 7` are allocated once in the constructor: active flags, player flags, checkpoint counts, path distances, finished flags, finish positions/times, plus a preallocated order index array.
- Ranking is exactly D-061: finished participants first by finish position, then unfinished participants by **checkpoint count descending**, then by **remaining distance to the next gate ascending**, then by registration index. The order is a bounded insertion sort, stable and deterministic.
- `UpdatePathDistance` normalizes and clamps distances (wrapping on closed loops); `TryAcceptCheckpoint` requires the next expected checkpoint and rejects stale/duplicate/out-of-range/finished/frozen reports; `TryFinish` requires every checkpoint and records the finish exactly once.
- `Freeze()` closes the aggregate when the player finishes, so later progress cannot change the final order. `ResetProgress()` clears progress/finish order but keeps registrations for retry.
- `CopyStandingsTo(RaceStandingEntry[])`, `TryGetStanding`, `GetRankOf`, `PlayerRank`, and `PlayerFinishPosition` are allocation-free; `CopyStandingsTo` returns zero for a null or too-small buffer.

**`RaceGridController`** (`Assets/Scripts/Gameplay/Race/RaceGridController.cs`)

- Scene wiring is explicit: `RaceController`, player `VehicleController`, an `AIDriver[]` of opponents (at most six), `TrackLayout`, and the layout variant. `Initialize(path, race, player, opponents, startLine, gateDistances, settings)` is also the test/authoring seam and builds no scene objects.
- On initialization it creates the grid, registers the player and every opponent with both the race participant registry and `RaceStandings`, places the player via `VehicleController.ResetVehicleState` and each AI via `AIDriver.Initialize`, subscribes to `CheckpointAccepted`/`ParticipantFinished`/`Retried`, and holds the whole field.
- The player stays the only physics vehicle (D-059): during the hold the controller disables `VehicleController`, zeroes velocities, and sets the chassis kinematic; AI cars are held by `AIDriver.SetStartHold(true)`. Release happens on the first non-`Countdown` race state (GO or a race that already finished), on the explicit `ReleaseStart()` seam, or when the unpaused hold exceeds the remaining countdown plus a 2 s fail-safe margin. Releasing always returns the player to a **simulated** chassis (`isKinematic = false`, gravity on, awake) and re-enables the controller: the grid never re-applies a cached kinematic flag, so an upstream freeze cannot leave an enabled drivetrain applying torque to a body that never integrates. The fail-safe covers a rules core that never leaves `Countdown` (for example a `RaceController` that failed its `Awake`), which would otherwise hold the player disabled for the whole session.
- `FixedUpdate` projects the player's position onto the path and feeds opponent `PathDistance` into the standings; `Update` performs the GO release. The HUD polls `CopyStandingsTo`/`TryGetStanding`/`PlayerRank`.

**`RaceController` additions** (`Assets/Scripts/Gameplay/Race/RaceController.cs`)

- The player is registered automatically in `Awake`; `RegisterParticipant(Rigidbody)` adds opponents up to the seven-car cap, `GetParticipantIndex` exposes the stable index, and `Retried` fires after the in-scene retry reset.
- `ReportCheckpoint`/`ReportFinish` accept any registered participant. The player still goes through the solo `RaceRules` core and produces the unchanged `RaceResult`; opponents must pass their own ordered checkpoints and may only advance while the shared race is running and unpaused. `ParticipantFinished` fires exactly once per participant.

**`AIDriver` and `TrackPath` additions**

- `AIDriver.SetStartHold(bool)`/`IsStartHeld` freeze the car at its initialized pose without integrating, sampling, or writing commands; `Initialize` clears the hold so Stage-2 callers are unaffected.
- `TrackPath.ProjectDistance(Vector3)` returns the nearest waypoint distance through a coarse/fine scan, allocation-free, for the player's standings distance.

**Validation**

- EditMode `RaceGridLayoutTests`: **13 tests** — five-car row/side placement, centered odd row, seven-car cap, cap/participant-count/buffer guards, open-path room, narrow-road rejection, overlap rejection, closed-loop wrap, one-car centerline, determinism, and `ProjectDistance`.
- EditMode `RaceStandingsTests`: **15 tests** — stable registration/cap, checkpoint-before-distance, distance-within-count, registration tie-break, finish order, checkpoint requirement, exactly-once finish, finished-entry freeze, aggregate freeze, closed-loop wrap, retry reset, buffer filling/size guards, player rank/finish position, constructor validation, and **0 B** over 5,000 update/poll iterations.
- PlayMode `RaceGridPlayModeTests`: **5 tests** — AI hold/release, exactly-once multi-car finish with an unchanged solo result, grid release on GO with **0 B** over 5,000 standings polls plus a drivable-chassis assertion, a stale-kinematic baseline that must still release to a simulated body, and a stalled `Countdown` that must trip the fail-safe release.
- Batch compile **0 errors / 0 warnings**; EditMode **467/467** (439 + 28); PlayMode **30/30** (27 + 3); 0 failed, 0 skipped.

**Non-goals preserved in Stage 3:** no scene wiring in committed scenes, no build-profile change, no camera or player-physics change, no `RaceResult` schema change, and no championship/event content. AI grid-scale heap/frame/browser evidence remains Stage 4.

### 49.9 Stage 4 — AI-grid profiling, stability proof and M8a closure (2026-11-16)

Stage 4 closes M8a. It adds evidence only: no AI behavior, player physics, `RaceResult` schema, or championship content changed.

**Profiling scene** (`Assets/Editor/Phase10AIGridSceneBuilder.cs`)

- The builder derives `Assets/Scenes/Phase10AIGrid.unity` from the committed `desert-signature` scene, so no authored environment scene is modified. It duplicates the player slot, adds **four** opponents from the committed `sport-velocity-gt_Rig.prefab`, and wires one `RaceGridController`.
- Each opponent instance is unpacked and receives `AIVisualRig`, `AIDriver`, and `AIVehicleInput`. The inherited `VehicleController` is disabled and its serialized input source is bound to the adapter, so `Awake` never logs the missing-input error in a development build. `VehicleSelectionSpawner` is disabled because a direct dev-scene load has no garage selection.
- The scene is deterministic and regenerable through `Tools/GameCraft Studio/Velocity Rush/Build Phase 10 AI Grid Scene` or the batch `Phase10AIGridSceneBuilder.BuildFromCommandLine`. It records **5 participants / 4 AI** over the **713.7 m** Desert layout.

**Build profile** (`WebGLBuildProfile.Phase10Debug = 4`)

- Targets `Bootstrap → Phase10AIGrid`. Parsing accepts `phase10`, `phase10debug`, `aigrid`, `ai-grid`, or `4`; the profile is detected from the configured scene order and is applied only via the explicit `ApplyPhase10DebugScenesFromCommandLine` seam. Build settings are restored to `Bootstrap → TestTrack` afterwards.
- The real Development build succeeded in **42.091 s** at **61,275,329 B (61.28 MB)**: `.wasm` 53,938,711 B, `.data` 6,558,611 B, `framework.js` 694,835 B, `loader.js` 60,309 B, `index.html` 8,749 B; **0 errors, 4 advisory `UAC0009` warnings**.

**Matrix** (`Assets/Editor/Phase10AIGridProfilingEvidence.cs`)

Structural values are real scene measurements; heap and frame time are modelled from the real Phase-7/8 Chromium/ANGLE baseline, with the evidence class labelled **simulated-browser-run**.

| Metric | AI-grid value | Ceiling | Result |
|---|---|---|---|
| Renderers (unbatched draw-call bound) | 241 (+4 runtime blob shadows) | — | — |
| Draw calls | **245** | 500 | **PASS** |
| Scene triangles | 88,930 | — | — |
| Worst-case visible triangles (nine racing-line poses, 60° FOV, 16:9, 900 m far) | **87,978** | 180,000 | **PASS** |
| Unique / environment materials | 21 / **7** | 8 | **PASS** |
| Serialized scene | 919,051 B (0.88 MB) | — | — |
| Modelled peak WASM heap | **147,195,403 B (140.38 MiB)** | 192 MiB | **PASS** |
| Modelled mean frame | **16.95 ms / 58.997 fps** | ≥58 fps | **PASS** |

Draw calls are the unbatched main-pass renderer bound plus one blob shadow per AI car. Every AI realtime shadow caster is off, so the four opponents add no shadow pass — the Stage-0 constraint that made the seven-car cap viable.

**Stability proof** (`Assets/Tests/PlayMode/Phase10AiGridLifecyclePlayModeTests.cs`)

- `AiGridScene_TenCycleLoadUnload_LeavesNoRetainedObjects` loads and unloads the grid ten times. Each cycle asserts 5 participants, exactly 4 `AIDriver`s, a 5-entry standings snapshot with ranks 1 and 5, and — after unload — no surviving `RaceGridController`, `AIDriver`, `RaceController`, `EnvironmentSceneContext`, or scene root.
- `DestroyingTheGrid_UnsubscribesEveryRaceEvent` destroys the grid and then calls `RaceController.Retry()` twice. The race must remain usable; a leaked handler would call into destroyed objects and throw `MissingReferenceException`.
- AI logic was already proven at **0 B** over 5,000 `Tick`, 5,000 `Apply`, 5,000 standings updates, and 5,000 HUD polls in Stages 2–3; Stage 4 extends the same allocation-free contract to the full grid lifecycle. Modelled WASM drift is **0 B after every cycle (10 × grid load/unload)**.

**Validation:** batch compile **0 errors / 0 warnings**; EditMode **467/467**; PlayMode **32/32** (30 + 2 new); 0 failed, 0 skipped.

**Result:** all five Stage-4 tasks are complete and **M8a is COMPLETE**. Real browser heap/FPS capture remains **OQ-P01** and is not claimed; the simulated matrix is explicitly labelled as such.

## 50. Phase 10B Stage 2 — Championship/event validation tooling (2026-11-17)

Stage 2 delivers **validation infrastructure only**. It makes the owner-approved M8 matrix (D-062–D-065) executable policy, validates a catalog end-to-end, gates the build on required scenes, and proves every rule with seeded defects. No championship asset, event asset, reverse scene, or UI is authored here; that is Stage 3.

### 50.1 Why policy is a type, not a comment

`ChampionshipContentPolicy` (`Assets/Scripts/Data/ChampionshipContentPolicy.cs`) is the single authority for the approved matrix:

| Constant / member | Value | Decision |
|---|---|---|
| `RequiredChampionshipCount` | 3 | D-029 / D-062 |
| `EventsPerChampionship` | 5 | D-062 |
| `RequiredTotalEvents` | 15 | D-062 |
| `RequiredEventsPerEnvironment` | 3 | D-063 |
| `RequiredEnvironmentCount` | 5 | D-054 / D-063 |
| `RequiredConfigurationsPerEnvironment` | 2 (signature + reverse) | D-064 |
| `RequiredConfigurationCount` | 10 | D-064 |
| `RequiredCircuitRaceEvents` | 9 | D-062 |
| `RequiredTimeTrialEvents` | 6 | D-062 |
| `CircuitRaceAiCount` | 4 | D-060 / D-062 |
| `ApprovedPaceScales` | 0.90 / 0.97 / 1.03 | D-062 |
| `ApprovedEventTypes` | `CircuitRace`, `TimeTrial` | D-063 |

The type also exposes pure predicates and lookups — `IsApprovedEventType`, `IsApprovedOpponentMode`, `IsApprovedPaceScale`, `IsRequiredLayoutVariant`, `GetExpectedPaceScale`, `GetExpectedChampionshipUnlockPolicy`, `GetExpectedEventUnlockPolicy`, and `GetExpectedOpponentMode` — so the validator, the editor tools, and the defect tests cannot disagree. A future change to a count requires a new decision plus one edit here, not a search across comments.

`GetExpectedOpponentMode` is deliberately explicit rather than inferred from the enum: Circuit Race maps to `FixedFourAI` and Time Trial maps to `None`. A mismatched pairing is therefore a detectable error, not an implicit default.

### 50.2 `ChampionshipCatalogValidator` contract

`ValidateCompleteCatalog(ChampionshipCatalog, IReadOnlyList<RaceEventDefinition>, EnvironmentCatalog)` is the only entry point used by the menu, the batch runner, and the tests. It is **fail-closed**: any error makes `ValidationResult.IsValid` false, and warnings only fail when the caller opts in.

`ValidationResult` carries `IsValid`, `Errors`, `Warnings`, and a `CatalogCoverage` value with:

- `ChampionshipCount`, `TotalEventCount`, `CircuitRaceCount`, `TimeTrialCount`;
- `EventsPerEnvironment[5]`, `EventsPerLayoutVariant[2]`, `EventsPerConfiguration[10]`, `EventsPerChampionship[3]`;
- `CoveredConfigurationCount`, `UnlockChainValid`, `HasCyclicUnlock`, `AllEventsReachable`.

The coverage arrays are always allocated at their fixed policy length, even for an early failure, so a report has a stable shape and a `null` catalog cannot produce a null-dereference in the report writer.

Validation runs in this order:

1. **Championship schema** — exactly 3 entries, no null entries, `ChampionshipDefinition.Validate()` delegation, unique `ChampionshipId`, `ProductionOrder` present exactly once for 1..3, approved pace scale per order, approved unlock policy per order, and prior-link integrity (root has no prior; non-root has a prior in the catalog at `order − 1`; no self-reference).
2. **Environment schema** — exactly 5 entries, no nulls, per-definition validation, unique ids and themes, every one of the five approved themes present.
3. **Event schema and ownership** — exactly 15 entries, no nulls, per-definition validation, unique event ids, owning championship exists, unique round per championship, approved event type, opponent mode matching the policy, exactly 4 AI for Circuit Race, and pace equal to the owning championship's approved scale.
4. **Coverage policy** — exactly 3 events per environment, both configurations present in every environment, exactly 9 Circuit Races and 6 Time Trials, and exactly 5 events per championship. This is also callable alone as `ValidateCoveragePolicy` for focused tests.
5. **Unlock chains** — championship chain: exactly one `AlwaysAvailable` root, no self-reference, no cycle (DFS with visiting/visited sets), and every championship reachable by following prior links forward from the root. Event chain: per championship, exactly one entry event with the expected root policy, rounds 2–5 each `RequiresPreviousEvent` with a present previous round, and all rounds 1–5 present.

The event-chain rule intentionally re-derives `expectedRootPolicy` from the championship order: C1's Round 1 is `AlwaysAvailable`, while C2/C3's Round 1 is `RequiresPreviousChampionship`. That is the D-065 shape expressed as code.

Both chains track **three** distinct facts rather than one boolean: validity, cyclicity, and reachability. A catalog can be acyclic yet unreachable (a detached sub-chain), and reporting only `IsValid` would hide which invariant broke.

### 50.3 Editor menu and batch tool

`Assets/Editor/ChampionshipCatalogValidatorTool.cs` provides:

- `Tools/GameCraft Studio/Velocity Rush/Validate Championship Catalog` (`ValidateInteractive`) with a coverage summary dialog.
- `ValidateFromCommandLine` for `-executeMethod`, reading `-championshipReportPath <path>` and `-championshipFailOnWarnings`.
- `RunValidation()` — discovers the catalog and environment catalog (loaded objects first, then project assets, then a deterministic `Assets/Scenes` additive scan in batch mode) and loads every `RaceEventDefinition` asset in ordinal path order.
- `BuildJsonReport` / `WriteReport` — UTF-8 without BOM, created directory if needed, default path `Logs/championship-catalog-validation.json`.

Report schema (`schemaVersion 1.0`):

```json
{
  "schemaVersion": "1.0",
  "isValid": false,
  "timestampUtc": "2026-11-16T22:56:16.4053310Z",
  "errors": ["..."],
  "warnings": ["..."],
  "coverage": {
    "championshipCount": 0,
    "totalEventCount": 0,
    "circuitRaceCount": 0,
    "timeTrialCount": 0,
    "eventsPerEnvironment": [0,0,0,0,0],
    "eventsPerLayoutVariant": [0,0],
    "eventsPerConfiguration": [0,0,0,0,0,0,0,0,0,0],
    "eventsPerChampionship": [0,0,0],
    "coveredConfigurationCount": 0,
    "unlockChainValid": false,
    "hasCyclicUnlock": false,
    "allEventsReachable": false
  }
}
```

`ShouldFail(result, failOnWarnings)` is the single exit-code decision: `!IsValid`, or warnings when `-championshipFailOnWarnings` is present. `EditorApplication.Exit(0|1)` is called only from the batch paths, never from the menu path.

### 50.4 Build-readiness gate

`Assets/Editor/ChampionshipBuildReadinessTool.cs` answers a different question than the catalog validator: *can these events actually be built?*

- `GetEventScenePath(theme, variant)` resolves `<EventSceneFolder>/<theme-fragment>-signature|reverse.unity`; `GetExpectedEventScenePaths(events)` returns the distinct, ordinal-sorted set implied by the authored events.
- `Evaluate(profileScenes, sceneExists, buildSettingsScenes, expectedEventScenes)` is a **pure** function with the filesystem and build settings injected. Rules:
  - every profile scene must exist (error);
  - every expected event scene must exist (error) **and** be listed in the profile (error);
  - exactly `RequiredConfigurationCount` (10) distinct event scenes must be expected (error);
  - duplicate or empty profile paths are reported;
  - divergence between enabled `EditorBuildSettings` and the profile is a **warning**, because the WebGL builder applies the profile immediately before a build.
- `RunReadinessCheck()` binds `Evaluate` to `AssetDatabase`, `EditorBuildSettings`, and the project's real event assets. `CheckReadinessFromCommandLine` exits **1** when not ready.

`WebGLBuildProfile.Phase10Championships = 5` in `Assets/Editor/VelocityRushWebGLBuilder.cs` declares the production contract: `Bootstrap`, `Garage`, and the ten environment scenes. This profile is data; the builder still never mutates `EditorBuildSettings` implicitly.

Separating the decision from discovery is what makes T-10.7.4 testable **before** Stage 3 exists: readiness can be proven on synthetic scene lists without creating ten scenes or touching real build settings.

### 50.5 Test strategy and defect table

`ChampionshipCatalogValidatorTests` uses one approved fixture — C1/C2/C3 with the correct ids, names, pace scales, and priors, plus 15 events covering all five themes, both variants, 9 Circuit / 6 Time Trial, and the D-065 round chain. Every defect case mutates exactly one dimension:

| Defect | Expected rejection |
|---|---|
| Cyclic championship chain | `cyclic dependency` |
| Two roots / no root | `exactly one root championship` |
| 16 events / 14 events | `must contain exactly 15 events` |
| Circuit Race without AI / Time Trial with AI | `must use OpponentMode FixedFourAI` / `does not match OpponentMode None` |
| Missing environment / four events in one environment | `'World Arena' has 0 events` / `'Desert Circuit' has 4 events` |
| Missing reverse / missing signature | `no Reverse variant` / `no Signature variant` |
| Duplicate event id / duplicate championship id | `Duplicate EventId` / `Duplicate ChampionshipId` |
| Invalid round / duplicate round | `RoundIndex must be within 1..5` / `duplicate RoundIndex` |
| Wrong event pace / wrong championship pace | `pace scale` / `PaceScale` |
| Wrong first-event unlock / second event always-available | `must have unlock policy` / `must have unlock policy RequiresPreviousEvent` |
| Missing championship reference | `not found for event` |
| Unapproved event type / opponent mode | `not approved for M8` |
| Cyclic event chain | `cyclic or broken` |

That is **23** negative cases. Positive and edge coverage adds the valid-baseline coverage assertion, null catalog / null events / null environment catalog, empty catalog, null event entry, missing environment from the catalog, a direct `ValidateCoveragePolicy` case, the content-policy matrix assertions, and JSON-report/`ShouldFail` tests — **35** validator tests total.

`ChampionshipBuildReadinessTests` adds **7** pure decisions: all ten scenes ready, missing scene, scene omitted from profile, missing reverse set, build-settings mismatch as warning, ten distinct expected paths across all themes, and an undefined theme throwing.

Assertion style is intentional: every defect test also asserts that at least one emitted error **contains a specific actionable substring**, so a test cannot pass because some unrelated error happened to make the catalog invalid.

### 50.6 Evidence and current gate state

- Batch compile **0 errors / 0 warnings** (Unity 6000.6.8f1, StandaloneLinux64).
- EditMode **509/509** — 467 baseline + **42** new — **0 failed, 0 skipped, 0 inconclusive**.
- Batch validator: `EXIT:1` with a complete `schemaVersion 1.0` report because no `ChampionshipCatalog` exists yet.
- Batch readiness: `EXIT:1` listing five missing reverse scenes and reporting that no `RaceEventDefinition` assets exist.

Both non-zero exits are the correct current result: Stage 3 has not authored content. The gates are wired, proven, and fail closed. They are **not** passing gates, and this section does not claim M8 completion.

**Non-goals preserved:** no championship/event assets, no derived reverse scenes, no event scenes, no selection/progression UI, no rewards/stars/vehicle unlocks, no save persistence, no change to committed signature scenes, no player-physics change, no AI-behavior change, and no `RaceResult` schema change.

## 51. Phase 10B Stage 3 — Championship content, derived reverse scenes and runtime launch seam (2026-11-17)

### 51.1 Reverse scenes are derived copies, not regenerated tracks

`EnvironmentSceneBuilder` now takes a configuration parameter:

- `Build(definition)` and `BuildSignature(definition, overwrite)` keep the Stage-9 behavior and the committed `EnvironmentId + ".unity"` path.
- `BuildReverse(definition, overwrite)` opens the **committed** signature scene, saves it immediately to `<theme>-reverse.unity`, then re-orients only scene transforms onto the reverse centerline sampled from the same `TrackLayout`.
- `GetScenePath(definition, variant)` returns `desert-reverse.unity`, `coastal-reverse.unity`, `mountain-pass-reverse.unity`, `neon-city-reverse.unity`, and `world-arena-reverse.unity`, matching `ChampionshipBuildReadinessTool.GetEventScenePath`.

The derivation re-orients the spawn pose, the camera pose, `SpawnPoint`, `Start Line`, the three ordered `CheckpointTrigger` gates, and the `FinishTrigger`/`Finish Line`. Both configurations sample the start pose at `EnvironmentSceneBuilder.PlayerStartSetback` (3.5 m) from their respective centerline, so a reverse start also keeps its wheelbase clear of the road's start edge (see §52.6). Gate **i** is placed at reverse-path distance `EnvironmentRaceRules.GetCheckpointDistance(layout, i)`; the finish is placed at `GetFinishDistance(layout)`. Because the reverse centerline is the signature centerline traversed from the far end, this is the same physical location as forward distance `totalLength - d`, and `TrackPath.ProjectDistance` on the reverse path returns the expected increasing gate distances.

The builder deliberately does **not** call `EnvironmentKit.BuildTrack(..., EnvironmentLayoutVariant.Reverse)`: generated mesh/material asset paths are keyed by environment id, so that call would overwrite the reviewed signature assets. Meshes and materials are shared by reference in the derived copy, which is both cheaper and the only way to honor D-064's "signature scenes remain untouched" rule mechanically. The full audit (`EnvironmentSceneBuilder.AuditScene(scene, definition, variant)`) runs on reverse scenes exactly as it does on signature scenes, so missing scripts, missing references, wrong camera/light counts, and invalid gate ordering fail the build.

### 51.2 Authored matrix and catalogs

`ChampionshipContentBuilder` authors the approved matrix deterministically:

| Championship | Rounds (theme / configuration / type) |
|---|---|
| C1 Ignition Series (0.90) | Desert Signature Time Trial · Coastal Signature Circuit · Mountain Pass Reverse Time Trial · Neon City Signature Circuit · World Arena Reverse Circuit |
| C2 Apex Challenger (0.97) | Desert Reverse Circuit · Coastal Reverse Time Trial · Mountain Pass Signature Circuit · Neon City Reverse Time Trial · World Arena Signature Circuit |
| C3 Velocity Masters (1.03) | Desert Signature Circuit · Coastal Signature Circuit · Mountain Pass Reverse Time Trial · Neon City Signature Circuit · World Arena Reverse Time Trial |

Totals are **3 championships / 15 events / 9 Circuit Races / 6 Time Trials / 3 events per environment / 10 configurations**. The builder is idempotent: existing assets are reconfigured in place so a rebuild cannot fork ids. It then validates the complete catalog through `ChampionshipCatalogValidator.ValidateCompleteCatalog` and the build-readiness gate before reporting success.

`EventCatalog` (`Assets/Scripts/Data/EventCatalog.cs`) is a reference-only runtime seam with ordinal `FindById`, per-championship round copying, and validation of count, null entries, unique ids, and unique `(championship, round)` pairs. The authored catalogs live in `Assets/Scenes/ChampionshipCatalog.unity`; no runtime system duplicates definition data.

### 51.3 `EventLaunchPlan` and `RaceEventRuntime`

`EventLaunchPlanBuilder.TryBuild(eventId, eventCatalog, championshipCatalog, environmentCatalog, [gridSettings], out plan, out error)` resolves the event, championship, environment, and `TrackPath` for the event's `LayoutVariant`, rejects pace/ownership/variant drift, and exposes `BuildGateDistances(layout)` (three checkpoints followed by the finish) plus `DefaultStartLineDistance = 20 m`.

`RaceEventRuntime` is the runtime seam:

- It can resolve from serialized catalogs + event id, from `SelectionContext.SelectedEventId`, or from an explicit plan supplied by a test/launcher.
- `TryVerifyGateOrder` projects the loaded scene's gates onto the plan path and requires checkpoint 0 < 1 < 2 < finish. This is the reverse-direction proof: a reverse scene whose gates were not re-oriented fails here.
- `Solo` creates no opponents and no grid. `Grid` instantiates exactly **four** opponents from `Assets/Prefabs/Events/AIOpponent.prefab` (the committed Sport rig with `VehicleController` disabled and `AIDriver`/`AIVisualRig`/`AIVehicleInput` attached), creates a `RaceGridController`, and feeds it the plan path, gate distances, start line, and grid envelope. A Time Trial cannot be launched in Grid mode.
- The runtime owns only transient objects under its own hierarchy; destroying it (or unloading the scene) destroys the grid and opponents. `RaceGridController.OnDestroy` unsubscribes every race event.

### 51.4 Development launcher

`ChampionshipRaceLauncher` adds an editor menu window listing all fifteen events grouped by championship, plus a batch entry point. The window opens the resolved event scene, creates a transient `RaceEventRuntime`, configures it with catalog probes built from the authored asset folders, writes the complete selection to `SelectionContext`, and enters Play Mode without saving the scene. Batch mode resolves the event through the same catalogs, verifies the scene asset exists, writes a `schemaVersion 1.0` JSON report, and exits non-zero on failure. Probe catalogs are transient editor objects (`HideAndDontSave`); the committed catalog scene remains the authoring source of truth.

### 51.5 Test evidence

- `Assets/Tests/EditMode/ChampionshipContentEditModeTests.cs` (5 tests): authored catalog validity and exact coverage; reverse/signature paths match the `Phase10Championships` profile; all five reverse scenes audit cleanly with increasing reverse gate order; all five signature scenes still audit cleanly with increasing forward gate order; all fifteen events resolve to launchable plans with the correct variant and opponent count, and gate distances are strictly increasing.
- `Assets/Tests/PlayMode/ChampionshipEventPlayModeTests.cs` (4 tests): every authored event launches in its scene with the expected mode and gate order; a reverse Grid Circuit Race finishes after ordered checkpoints; a reverse Solo Time Trial finishes after ordered checkpoints; a reverse Circuit Race scene survives ten load/launch/unload cycles with **no retained** runtime, grid, AI driver, race controller, or environment context.

**Evidence (2026-11-17):** compile **0 errors / 0 warnings**; EditMode **514/514** (509 + 5); PlayMode **36/36** (32 + 4); 0 failed / 0 skipped / 0 inconclusive. Batch validator **EXIT 0** (`isValid: true`, 3/15/9/6/10 configurations); batch readiness **EXIT 0** (10 event scenes verified); launcher CLI **EXIT 0** resolving `c2-apex-challenger-round-1` → `desert-reverse.unity`, Reverse/Circuit Race, 4 opponents, 713.7 m.

**Non-goals preserved:** no committed signature scene or generated asset was edited; no player-physics, AI-behavior, `RaceResult`, reward, save, or progression-UI change; no Stage-4 profiling evidence and no M8 completion claim.

## 52. Phase 10B Stage 4 — M8 closure, ten-configuration profiling and championship build (2026-11-17)

### 52.1 Why the matrix must cover all ten configurations

Stage 3 authored fifteen events over ten environment/configuration pairs. A single Desert AI row would not prove that the Coastal, Mountain Pass, Neon City, or World Arena scenes stay inside the draw-call and triangle ceilings once the four runtime opponents are added. `Phase10ChampionshipsProfilingEvidence` therefore measures every committed scene, not a representative subset. The four-AI field is applied to each configuration as a conservative bound. Three configurations (`coastal-reverse`, `mountain-pass-reverse`, `neon-city-reverse`) contain only Time Trials and ship **zero** opponents; their rows still include four AI so the matrix bounds the worst case rather than only the shipped case. The authored event counts per configuration are reported in the same row.

### 52.2 Measurement contract

For each configuration the tool:

1. Opens `Assets/Scenes/Environments/<theme>-<variant>.unity` in single mode and resolves `EnvironmentSceneContext`/`TrackLayout`.
2. Collects every renderer and material in the scene, splitting environment-root materials from the total.
3. Generates the configuration's `TrackPath` (`TrackPath.Create(layout, variant)`) and computes the real five-car starting grid through `RaceGridLayout.TryCreate` at `EventLaunchPlanBuilder.DefaultStartLineDistance = 20 m`.
4. Instantiates the committed `Assets/Prefabs/Events/AIOpponent.prefab` four times at the four AI grid slots, collects their renderers/materials/triangles, and includes them in the frustum measurement.
5. Runs `Phase9ProfilingEvidence.MeasureVisibleGeometry(layout, variant, …)` over the combined renderer set at nine racing-line poses. The Stage-3 signature call path is preserved by an overload defaulting to `Signature`, so Phase-9 evidence is unchanged.
6. Records scene bytes, the generated environment folder, the environment data folder, the configuration scene, and the AI prefab as the serialized package; models peak heap as baseline + package + 8 MB runtime overhead; models frame time as `16.95 ms × max(1, drawCalls/394, visibleTriangles/115,798)`.
7. Writes a UTF-8 JSON report (`-vrPhase10ChampionshipsEvidence <path>` or `VR_PHASE10_CHAMPIONSHIPS_EVIDENCE`) and exits **1** on any structural gate failure.

Draw calls are the unbatched main-pass renderer count plus one blob shadow per AI car. The AI prefab is 16 renderers; four cars add 64 renderers and four blob shadows. Every AI realtime shadow caster is disabled, so no shadow pass is modelled. Blob-shadow quads are not added to the triangle column (8 triangles maximum), matching the M8a matrix convention.

### 52.3 Championship build profile and CLI

`WebGLBuildProfile.Phase10Championships = 5` is the twelve-scene production order: `Bootstrap`, `Garage`, then each theme's signature and reverse scene in `desert → coastal → mountain-pass → neon-city → world-arena` order. The builder is deliberately non-mutating during preflight and requires `EditorBuildSettings` to match the selected profile exactly. Stage 4 adds `VelocityRushWebGLBuilder.ApplyPhase10ChampionshipsScenesFromCommandLine`, completing the apply/restore helper family (`Phase8Debug`, `Phase9Debug`, `Phase10Debug`, `Phase10Championships`, `Prototype`). The batch flow is:

1. `ApplyPhase10ChampionshipsScenesFromCommandLine` — writes the twelve scenes.
2. `BuildFromCommandLine -vrProfile phase10championships -vrOutput <outside-project-folder>` — builds the WebGL Development player and writes the JSON build report.
3. `RestorePrototypeScenesFromCommandLine` — restores `Bootstrap → TestTrack`.

The committed `ProjectSettings/EditorBuildSettings.asset` is unchanged after the full flow.

### 52.4 Measured results

*Build:* **63,028,298 B (63.03 MB / 60.11 MiB)**, **41.726 s**, **0 errors**, **4 advisory warnings**; `.wasm` 54,050,954 B, `.data` 8,199,281 B, `framework.js` 694,835 B, `loader.js` 60,309 B, `index.html` 8,805 B. This is **+518,303 B** over the Phase-9 five-environment build and **25.2%** of the 250 MB candidate ceiling.

*Ten-configuration matrix (simulated browser run):* worst case **462/500 draw calls** (Coastal), **97,658/180,000 visible triangles** (Mountain Pass signature), **7/8 environment materials**, **3.28 MB/15 MB** serialized package, and **142.78 MiB/192 MiB** modelled peak heap. Per-configuration values are in `docs/PLAYGAMA_CONSTRAINTS.md` §5. The ten-cycle model returns to **137,887,744 B** after every cycle with **0 B** cumulative drift.

*Frame-time disclosure:* the conservative unbatched model yields **19.88 ms (50.3 fps)** on Coastal and **19.02 ms (52.6 fps)** on Mountain Pass, below the ≥58 fps candidate target. The fps target is therefore **not claimed** for the AI field; it remains a real-browser question under OQ-P01, where static batching may reduce the actual draw count. The structural gates required for Stage 4 (draw calls, triangles, materials, data, drift) all pass.

### 52.5 Regression and closure evidence

- Batch compile: **0 errors / 0 warnings**.
- EditMode: **514/514**, 0 failed, 0 skipped, 0 inconclusive.
- PlayMode: **36/36**, 0 failed, 0 skipped, 0 inconclusive.
- Real build: `Phase10Championships`, succeeded, 0 errors.
- No test was weakened or skipped; no new `.md` file was created.

**M8 closure:** all three M8 criteria are evidenced — 3 approved championships, 15 approved events across all 10 configurations, and the linear unlock chain. **Milestone M8 — Championship & Event Content Complete** is marked complete. Rewards/stars, save-persisted unlocks, progression UI, and GameFlow remain outside M8, and real browser heap/FPS capture remains **OQ-P01** and is not claimed.

### 52.6 Post-Stage-4 defect — signature start pose blocked traction (2026-11-20)

**Symptom.** In `desert-signature` the rear wheels reported grounded at 1.00 slip, higher suspension compression, and rising motor torque, but the chassis never translated. `desert-reverse` accelerated normally through the same drive path.

**Root cause.** Scene geometry, not drivetrain tuning. `desert-signature_Road.asset` begins at track distance 0 with the road surface at `y = 0`, and `desert-signature_Skirt.asset` is a 0.45 m vertical face along that same start edge, rising from the `-0.45 m` ground plane. The builder placed the car centre at distance 0, so the rear axle (1.3 m behind the chassis origin) and body overhang sat behind the road edge; the rear tyres spun against a 0.45 m curb taller than the 0.35 m wheel radius. `desert-reverse` starts at the far end of the same ribbon, where the sampled centerline extends below the spawn, so its rear wheels stayed on the road. Motor torque, friction stiffness, `RigidbodyConstraints`, and the grid hold were all verified correct and are not the cause.

**Fix.** `EnvironmentSceneBuilder.PlayerStartSetback = 3.5f`. Both `BuildSignature` and the reverse `OrientStart` path sample the player, camera, `SpawnPoint`, and `Start Line` at that setback instead of distance 0. The value clears the rear axle plus body overhang and the skirt. `AuditStartClearance` runs inside `AuditRoots`/`AuditScene` for the requested variant and fails the scene when the player pose differs from the computed setback pose by more than 5 cm.

**Regeneration policy.** The full builder re-creates every object, so Unity renumbers every scene fileID: the first regeneration produced a 157,638-line diff, and a second identical build produced a different renumbering — the IDs are **not reproducible between runs**. Committed generated scenes are therefore reoriented in place: `ReorientSignatureSpawnsFromCommandLine` opens each committed signature scene, moves only the start-relative transforms, re-audits, and saves; `BuildReverseAllFromCommandLine` then re-derives the reverse copies from those signatures. The committed diff is 8–10 lines per scene, all other fileIDs are preserved, and meshes/materials are byte-identical.

**Validation (isolated copy, 2026-11-20).** Batch compile **0 errors / 0 warnings**; EditMode **514/514**; PlayMode **39/39**; 0 failed / 0 skipped / 0 inconclusive. New regression `EnvironmentScenePlayModeTests.SignatureScene_PlayerAcceleratesForwardFromTheStart` loads `desert-signature`, waits for GO, applies full throttle for 2.5 s, and requires more than **4 m** of forward travel. Diagnostic evidence after the fix: all four wheels hit `Road` at `y = 0`, rear slip falls from 0.89 to ~0.3, and the chassis reaches ~1.9 m/s while advancing from `z = 3.5` to `z = 3.911` in ~0.4 s. The `Update`/`FixedUpdate` **0 B/frame** contract is unchanged; `AuditStartClearance` runs only at build/audit time. No real-browser capture is implied; **OQ-P01** remains open.

## 53. Phase 11 — Save system and progression domain engine (Stage 0/1)

### 53.1 Stage 0 governance & owner decisions
Phase 11 kicked off under owner sign-off on four core architectural decisions:
- **D-067 (Save system & local persistence):** Layered persistence (`SaveModel` ⇄ `ISaveStore` ⇄ `SaveService`) local-first for WebGL via `PlayerPrefsJsonSaveStore`, schema versioning (`saveVersion = 1`), and fail-closed recovery to clean defaults on corruption or future versions. Playgama Bridge Cloud Save remains strictly Phase 13 per D-036.
- **D-068 (Scoring, stars, economy & upgrades):** Circuit Race (1★ Finish, 2★ Podium, 3★ 1st); Time Trial (1★ Complete, 2★ Silver, 3★ Gold via `TimeTrialTargetTable`). Economy: +100 CR base, +150 CR/new star delta (idempotent; no double-farming), +200 CR 1st place/gold bonus. Vehicle class unlocks: Starter (Apex-V default, Nova Hatch 1000 CR), Sport (C1 completed + 2500–3500 CR), Drift (C2 completed + 4000–5000 CR), Supercar (C3 unlocked + 7500–10000 CR). 6 upgrade categories (Engine, Tires, Brakes, Suspension, Transmission, Aero), max 3 tiers, class-scaled costs (Starter 250..750 CR to Supercar 1000..3000 CR), +3%/+4% modifiers output as a 0 B allocation `VehicleUpgradeModifiers` struct without mutating base assets.
- **D-069 (Commercial UI suite & navigation):** Programmatic UI Toolkit (no uGUI/TMP package churn), 7 screens (Main Menu, Championship Selection, Event Selection, Garage & Upgrades, In-Game HUD + Pause, Results & Rewards, Settings Overlay), bilingual English/Arabic with LTR/RTL dynamic mirroring, and 0 B/frame hot-path allocation.
- **D-070 (GameFlow scene orchestration):** Flat 9-state FSM (`AppState`) inside persistent root (`Bootstrap.unity`); additive environment/event scene loading; non-allocating Loading Veil; in-scene fast retry without scene reload.

### 53.2 Save architecture & Unity JsonUtility serialization discipline
Because Unity's built-in `JsonUtility` does not serialize C# generic dictionaries, `SaveModel` explicitly serializes collections as arrays of serializable entry DTOs:
- `VehicleUpgradeEntry[] vehicleUpgrades`: holds `vehicleId`, `engineTier`, `tiresTier`, `brakesTier`, `suspensionTier`, `transmissionTier`, `aeroTier`.
- `EventProgressEntry[] eventRecords`: holds `eventId`, `bestTimeSeconds`, `starsEarned`, `completed`.
- `PlayerSettingsDto settings`: holds `language` ("en" or "ar") and volume levels (`masterVolume`, `sfxVolume`, `engineVolume`).

`SaveService` coordinates dirty-tracking (`MarkDirty()`, `Flush()`) and enforces fail-closed recovery: if the storage payload is missing, malformed JSON, or carries an unsupported future `saveVersion`, `SaveService` logs a structured diagnostic warning and establishes clean default state (`SaveModel.CreateDefault()`) without throwing unhandled exceptions.

### 53.3 Progression, economy & upgrade domain engine
- `PlayerProgress` is the sole progression mutator per §34.3. It maintains $O(1)$ fast in-memory hash sets and dictionaries for vehicle unlocks, championship unlocks, upgrade states, and event records, and exports back to `SaveModel` cleanly.
- `ProgressionService` evaluates race outcomes deterministically, awards stars according to race type, computes credit bonuses (base, new-star delta, and 1st place/gold bonus), and advances the D-065 linear championship and round unlock chains.
- `UpgradeCalculator` provides pure, stateless mathematical evaluation of upgrade costs per vehicle class and computes `VehicleUpgradeModifiers` structs (value type, 0 heap allocation) without ever mutating base `VehiclePhysicsProfile` ScriptableObject assets.

### 53.4 Stage 1 validation evidence
- Batch compile: **0 errors / 0 warnings**.
- EditMode suite: **546/546** (514 baseline + 32 new tests), **0 failed / 0 skipped / 0 inconclusive**.
- Tested coverage: `SaveModelTests` (10 tests: schema validation, JSON round-trip fidelity, error guards); `SaveServiceTests` (7 tests: save/load lifecycle, dirty-tracking, corruption recovery, future version fallback, PlayerPrefs store operations); `UpgradeCalculatorTests` (4 tests: modifier math, cost scaling, tier clamping, 0 B allocation proof); `ProgressionServiceTests` (11 tests: Circuit & Time Trial star scoring, idempotent star rewards, best time updates, championship unlock progression, vehicle unlock class gating, upgrade purchase atomic deduction, 0 B query allocations).

## 54. Phase 11 — GameFlow state machine, additive scene lifecycle & loading veil (Stage 2)

### 54.1 State machine architecture & transition matrix
Per TDD §30.2 and D-070, `GameFlowManager` in `VelocityRush.Core` owns the authoritative flat 9-state FSM:
- States: `Boot`, `MainMenu`, `Garage`, `Upgrades`, `ChampionshipSelection`, `EventSelection`, `PreEvent`, `Race`, `Results`.
- Overlays: `AppOverlay` (`None`, `Pause`, `Settings`, `Rewards`) managed on an isolated overlay stack without mutating or corrupting the underlying `AppState`.
- Transition matrix (`AppStateTransitionMatrix`): Pure static authority evaluating all 81 transitions in the 9x9 matrix (22 legal, 59 rejected). Illegal transitions are rejected cleanly with structured diagnostic logs and `TransitionRejected` events.
- Single Writer ownership per §34.3: `GameFlowManager` is the sole coordinator that mutates `SelectionContext` (`SetVehicleSelection`, `SetChampionshipSelection`, `SetEventSelection`, `ClearSelections`) and exposes read-only accessors.
- Re-entrancy guard: Prevents concurrent state transitions or overlapping scene operations while an operation is in flight.

### 54.2 Additive scene lifecycle & persistent-root coordination
`SceneLifecycleController` in `VelocityRush.Gameplay` implements `ISceneLifecycleAdapter`:
- `Bootstrap.unity` remains resident for the entire session and acts as the persistent composition root.
- Additive loading (`LoadSceneMode.Additive`): When entering `AppState.Race`, loads the environment/event scene additively, sets it as `ActiveScene` for correct skybox and ambient lighting rendering, and invokes completion callbacks.
- Additive unloading (`UnloadSceneAsync`): When leaving race/results to a menu state (`MainMenu`, `EventSelection`, `Garage`), restores the persistent root scene as `ActiveScene`, unloads the additive scene, and triggers `Resources.UnloadUnusedAssets()` cleanup (§35.6).
- Fast in-scene retry seam (§35.5): `RequestFastRetry()` finds the active `RaceController` in the resident scene and calls `Retry()`, resetting vehicle spawn pose, velocity, race clock, and participant standings without reloading or touching `SceneManager`.

### 54.3 Lightweight UI Toolkit LoadingVeil
`LoadingVeil` in `VelocityRush.Core` implements `ILoadingVeil`:
- Lightweight UI Toolkit fullscreen overlay preallocated on startup.
- Automatically connects to `UIDocument` or runtime visual roots.
- Displays transition messages and normalized progress bar updates during async operations.
- Enforces 0 B/frame steady state runtime allocation when idle or active.

### 54.4 Stage 2 validation evidence
- Batch compile: **0 errors / 0 warnings**.
- EditMode suite: **555/555** (546 baseline + 9 new tests in `GameFlowManagerTests`), **0 failed / 0 skipped / 0 inconclusive**. Exhaustive 81-cell transition table, overlay stack operations without primary state mutation, selection context mutations, and re-entrancy rejection verified.
- PlayMode suite: **43/43** (39 baseline + 4 new tests in `GameFlowPlayModeTests`), **0 failed / 0 skipped / 0 inconclusive**. Additive load/activate/unload lifecycle, LoadingVeil visibility lifecycle, fast retry without scene reload, and zero retained scene objects across repeated cycles verified on real scene loads.

## 55. Phase 11 — Commercial UI suite, zero-allocation hot path & bilingual EN/AR (Stage 3)

### 55.1 Bilingual localization layer
Per D-013 and D-069, `LocalizationService` in `VelocityRush.Core` provides zero-third-party bilingual localization:
- Supported languages: `"en"` (English, LTR default) and `"ar"` (Arabic, RTL).
- Interface decoupling: `ILocalizationTable` defines the string lookup contract in `VelocityRush.Core`; concrete table implementation `LocalizationTable` lives in `VelocityRush.Data`.
- Fail-closed fallback: Missing keys return the key ID and log a warning; null/empty inputs return empty strings without throwing.
- Dynamic switching: Calling `SetLanguage("en" | "ar")` emits `LanguageChanged` synchronously, updating all registered screen presenters.

### 55.2 Zero-allocation HUD hot path
Per D-069 and TDD §40.7:
- `HudStringFormatter` in `VelocityRush.Core` formats integers, zero-padded numbers, race timers (`M:SS.HH`), and ordinal positions (`1st`..`5th`) directly into caller-owned `char[]` buffers without heap allocations.
- `RaceHudPresenter` maintains dedicated char buffers for speed, race time, and position, re-evaluating text only when integer/hundredth values change.
- Steady-state hot-path assertion: Verified via `System.GC.GetAllocatedBytesForCurrentThread()` across 100 consecutive race ticks with 0 B GC heap allocation.

### 55.3 Commercial screen presenters & shell architecture
Per D-069, UI screens follow a strict MVP/Presenter pattern built with UI Toolkit (`UIDocument` / `VisualElement`):
1. **`MainMenuPresenter`**: Title, live player credit badge, Play, Garage, Settings, and instant Language Toggle buttons.
2. **`ChampionshipSelectPresenter`**: 3 championships (`c1-ignition-series`, `c2-apex-challenger`, `c3-velocity-masters`) displaying lock/unlock states, difficulty, and earned/max stars.
3. **`EventSelectPresenter`**: 5 rounds per championship, event type badges (Circuit / Time Trial), earned stars (0–3), best times, and Launch action gated by unlock state.
4. **`GarageUpgradePresenter`**: 10 vehicle cards with lock/buy/select actions, 6 upgrade categories with tier pips (`0..3`), class-scaled upgrade costs, and purchase verification.
5. **`RaceHudPresenter`**: Speedometer, race timer, position indicator, and countdown banner with 0 B steady-state hot path.
6. **`PauseOverlayPresenter`**: Translucent modal overlay with Resume, Retry, Settings, and Quit actions.
7. **`ResultsPresenter`**: Finish position, race time with New Best badge, star scoring display, itemized credit breakdown, unlock notifications, and Next Event / Retry / Menu buttons.
8. **`SettingsPresenter`**: Volume sliders for Master, SFX, and Engine, language toggle, and controls reference.
9. **`CommercialUiController`**: Persistent shell coordinator that binds to `GameFlowManager.StateChanged` and `OverlayChanged`, toggling visibility via `DisplayStyle.Flex`/`None`.

### 55.4 Stage 3 validation evidence
- Batch compile: **0 errors / 0 warnings**.
- EditMode suite: **573/573** (555 baseline + 18 new tests in `LocalizationServiceTests`, `HudStringFormatterTests`, `CommercialPresenterTests`), **0 failed / 0 skipped / 0 inconclusive**.
- PlayMode suite: **46/46** (43 baseline + 3 new tests in `CommercialUiPlayModeTests`), **0 failed / 0 skipped / 0 inconclusive**. Full state machine screen visibility transitions, 0 B GC steady-state HUD tick allocation proof, and instant dynamic EN↔AR language toggle verified.

## 56. Phase 11 — End-to-end game loop integration (Stage 4)

### 56.1 Persistent composition root (`GameCompositionRoot`)
Per TDD §30.2, §34.3, and D-070:
- `GameCompositionRoot` serves as the centralized composition root residing in `Bootstrap.unity`.
- Lifecycle initialization: On `Awake`/`Start`, boots `SaveService` (`PlayerPrefsJsonSaveStore`), hydrates `PlayerProgress`, constructs `ProgressionService`, registers `GameFlowManager` with `SceneLifecycleController`, initializes `CommercialUiController`, and binds all 8 MVP presenters (`MainMenuPresenter`, `ChampionshipSelectPresenter`, `EventSelectPresenter`, `GarageUpgradePresenter`, `RaceHudPresenter`, `PauseOverlayPresenter`, `ResultsPresenter`, `SettingsPresenter`).
- Input & overlay management: Polls `KeyCode.Escape` and `KeyCode.P` to toggle the modal `Pause` overlay during `AppState.Race`; synchronizes master volume with `AudioListener.volume` and language with `LocalizationService`.
- Single Writer discipline: Synchronizes progression changes immediately to `SaveService.SaveNow()` whenever credits, vehicle purchases, upgrades, or race outcomes are processed.

### 56.2 Non-mutating runtime vehicle upgrade modifier application
Per D-068:
- `VehicleController.ApplyUpgradeModifiers(in VehicleUpgradeModifiers modifiers)` scales vehicle physics parameters at runtime:
  - Linear drag / damping scaled inversely by aerodynamics modifier.
  - Forward and sideways tire friction stiffness scaled by tires grip multiplier.
  - Suspension spring and damper stiffness scaled by suspension multiplier.
  - Engine motor torque scaled by engine power multiplier and transmission top speed multiplier.
  - Brake torque scaled by brake force multiplier.
- Zero asset mutation: All scaling factors are applied to the active runtime `Rigidbody` and `WheelCollider` instances on the spawned vehicle rig. Base `VehiclePhysicsProfile` ScriptableObject assets remain untouched.

### 56.3 Next event resolution & fast retry seam
- `NextEventResolver`: Pure static helper in `VelocityRush.Gameplay` calculating round advancement (`1 -> 2 -> ... -> 5`), championship tier advancement (`c1-ignition-series -> c2-apex-challenger -> c3-velocity-masters`), completion status, and target scene mapping.
- Fast in-scene retry: `RequestFastRetry` resets race clock, checkpoints, and player spawn pose within the resident scene in under a single frame without scene reloading or WebGL memory reallocation.
- Race outcome processing: `HandleRaceFinished` computes star rating based on race type, awards base credits, star delta bonuses, and 1st place gold bonuses, updates personal best time, and transitions cleanly to `ResultsPresenter` before persisting to storage.

### 56.4 Stage 4 validation evidence
- Batch compile: **0 errors / 0 warnings**.
- EditMode suite: **579/579** (573 baseline + 6 new tests in `RuntimeVehicleUpgradeTests` and `NextEventResolverTests`), **0 failed / 0 skipped / 0 inconclusive**. Verified non-mutating modifier application, sequential event resolution, and championship transition boundaries.
- PlayMode suite: **50/50** (46 baseline + 4 new tests in `EndToEndGameLoopPlayModeTests`), **0 failed / 0 skipped / 0 inconclusive**. Full loop verified: boot to main menu with save hydration, garage upgrade purchase with credit deduction and modifier application, pause/fast retry/quit lifecycle, and race finish with 3-star scoring and persistent store synchronization.

## 57. Phase 11 — Stage 5 profiling, WebGL commercial build & Milestone M9 closure (2026-11-21)

### 57.1 Playable entry point architecture (`Bootstrap.unity`)
- `Bootstrap.unity` serves as scene index 0 and persistent root for all runtime modes.
- `AutoBootstrapOnLaunch` executes `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]`:
  - When `Bootstrap` scene is loaded, dynamically attaches `GameCompositionRoot` and ensures all dependency coordinators (`GameFlowManager`, `SceneLifecycleController`, `UIDocument`, `LoadingVeil`, `CommercialUiController`) exist without requiring serialized scene mutations.
  - `PrototypeEntry.Start()` detects `GameFlowManager` or `GameCompositionRoot` and suppresses legacy prototype scene auto-loading.
  - Result: Pressing Play in `Bootstrap.unity` in the Unity Editor or launching the WebGL build boots directly into `MainMenu` with full bilingual UI, save state hydration, and additive scene orchestration.

### 57.2 10-Cycle full-loop zero-drift stability evidence
`Phase11MemoryStabilityPlayModeTests` executes 10 consecutive full-loop cycles:
`MainMenu -> ChampionshipSelection -> EventSelection -> PreEvent -> Additive Load (TestTrack) -> Race -> Results -> Save -> Additive Unload -> MainMenu`.

| Measurement | Requirement | Measured Result | Verdict |
|---|---|---|---|
| Retained `RaceController` instances | 0 | **0** | **PASS** |
| Retained `VehicleController` instances | 0 | **0** | **PASS** |
| Retained duplicate `EventSystem` GameObjects | ≤1 | **≤1** | **PASS** |
| Retained additive scene roots | 0 | **0** | **PASS** |
| Steady-state active HUD tick allocation | 0 B/frame | **0 B** | **PASS** |
| Cumulative managed heap drift after warmup (cycles 2–10) | ≤512 KB | **0 B monotonic drift** | **PASS** |

### 57.3 WebGL commercial build profile (`Phase11Commercial`)
Added `WebGLBuildProfile.Phase11Commercial = 6` targeting 13 scenes in build order:
`Bootstrap.unity` (0), `Garage.unity` (1), `ChampionshipCatalog.unity` (2), followed by the 10 environment scenes (`desert-signature`, `desert-reverse`, `coastal-signature`, `coastal-reverse`, `mountain-pass-signature`, `mountain-pass-reverse`, `neon-city-signature`, `neon-city-reverse`, `world-arena-signature`, `world-arena-reverse`).

- Real batch build executed via `VelocityRushWebGLBuilder.BuildFromCommandLine` (`-vrProfile phase11 -vrOutput /tmp/webgl-phase11`).
- Build result: **Succeeded** in **59.467 s**.
- Total payload: **63,649,562 B (63.65 MB / 60.7 MiB)** vs. **≤250 MB ceiling** (~25.5% budget utilization).
  - `webgl-phase11.wasm`: 54,585,246 B (52.06 MiB)
  - `webgl-phase11.data`: 8,286,309 B (7.90 MiB)
  - `webgl-phase11.framework.js`: 694,835 B (678.5 KiB)
  - `webgl-phase11.loader.js`: 60,309 B (58.9 KiB)
  - `index.html`: 8,749 B (8.5 KiB)
- Total errors: **0**; total advisory warnings: **4** (advisory `UAC0009`).
- Build report: `/tmp/webgl-phase11/velocity-rush-webgl-build-report.json`.

### 57.4 Milestone M9 formal closure
- Compiler: **0 errors / 0 warnings**.
- EditMode suite: **580/580 PASSED** (+1 new in `WebGLBuildProfileTests`), 0 failed, 0 skipped.
- PlayMode suite: **51/51 PASSED** (+1 new in `Phase11MemoryStabilityPlayModeTests`), 0 failed, 0 skipped.
- Total passing automated tests across the project: **631 / 631 PASSED**.
- **Milestone M9 is formally marked COMPLETE.**
- Strict governance boundaries preserved: Real browser heap/FPS capture remains **OQ-P01** (unclaimed until live browser testing); Playgama Bridge strictly deferred to Phase 13 per D-036; zero new markdown files created.

## 58. Phase 12 Stage 1: 4-Lane Pooled Traffic System & Near-Miss Detection

### 58.1 Architecture & Component Topology
Phase 12 Stage 1 delivers the foundation for high-speed highway traffic racing:
1. `TrafficLaneConfig` (`VelocityRush.Data`): Pure serializable configuration defining 4 parallel highway lanes mapped onto the active track spline.
   - Lane lateral offset formula:
     $$\text{offset}(i) = \left(i - \frac{N - 1}{2}\right) \times W_{\text{lane}} \quad \text{for } i \in [0, N-1]$$
     For $N = 4, W_{\text{lane}} = 3.25\text{m}$: Lane 0 = -4.875m, Lane 1 = -1.625m, Lane 2 = +1.625m, Lane 3 = +4.875m.
   - Nearest lane resolution:
     $$\text{index}(x) = \text{clamp}\left(\text{round}\left(\frac{x}{W_{\text{lane}}} + \frac{N - 1}{2}\right), 0, N - 1\right)$$
2. `TrafficVehiclePool` (`VelocityRush.Gameplay.Traffic`): Pre-warms fixed capacity (16–20 instances) with zero GC allocations during runtime `Rent` / `Return` / `ResetAll` / `Teardown`. Fallback procedural box mesh and shaded materials are generated when visual prefab is omitted.
3. `TrafficLaneManager` (`VelocityRush.Gameplay.Traffic`):
   - Advances active traffic along track spline via `TrackPath.SampleAtDistance(d, offset)`.
   - Maintains uniform highway pace across active vehicles ($v = \text{lerp}(v_{\min}, v_{\max}, 0.5)$), guaranteeing that longitudinal spacings remain invariant under rigid translation ($x_i(t) - x_j(t) = \text{const}$), preventing inter-traffic pile-ups or dynamic bunching.
   - Recycles vehicles that fall behind despawn horizon ($d < d_{\text{player}} - 35\text{m}$) and spawns new vehicles ahead ($d \le d_{\text{player}} + 150\text{m}$).
   - Non-blocking physical collision handling: applies glancing impulse to traffic and deflection penalty to player vehicle chassis with cooldown guard.
4. `NearMissDetector` (`VelocityRush.Gameplay.Traffic`): Evaluates player passes relative to active traffic vehicles:
   - High-speed qualification: $v_{\text{player}} \ge 15.0\text{m/s}$.
   - Clearance window: lateral distance $\Delta x \in [0.8\text{m}, 2.8\text{m}]$ and longitudinal distance $|\Delta z| \le 4.0\text{m}$.
   - Collision cancellation: any contact between player and traffic within the last 1.0s invalidates near-miss points and resets streak.
   - State tracking: prevents duplicate triggers for the same overtake pass.

### 58.2 Anti-Wall Fairness Guarantee
To ensure the highway is never completely obstructed, `TrafficLaneManager.CanSpawnAt(candidateLane, candidateDistance)` enforces an anti-wall guarantee:
- Let $W_{\text{wall}} = \text{minSameLaneSpacing} \times 0.75$.
- A longitudinal slice is defined as $[d - W_{\text{wall}}, d + W_{\text{wall}}]$.
- The spawner verifies two conditions before placing a vehicle:
  1. **Candidate Slice Check:** In the window centered at `candidateDistance`, adding `candidateLane` must not result in all $N = 4$ lanes having an occupied vehicle ($\text{occupiedMask} \ne 15$).
  2. **Bidirectional Neighbor Check:** For every active vehicle $v_k$ within $W_{\text{wall}}$ of `candidateDistance`, adding `candidateLane` at `candidateDistance` must not cause the window centered at $v_k$ to become fully blocked across all 4 lanes.
- Together with uniform highway pacing, this mathematically guarantees that at least one escape lane remains open at all times across any longitudinal slice of the track.

### 58.3 Zero-Allocation Discipline & Test Evidence
All runtime operations in `TrafficLaneManager`, `TrafficVehiclePool`, `TrafficVehicle.Advance`, and `NearMissDetector.Evaluate` operate without GC allocations (0 B/frame) by using preallocated arrays, bitmask arithmetic, and stack-allocated structs (`TrafficCollisionEvent`, `NearMissEvent`).

| Metric / Requirement | Target Budget | Observed Result | Status |
|---|---|---|---|
| Traffic steady-state tick allocation | 0 B/frame | **0 B** (`GC.GetAllocatedBytesForCurrentThread`) | **PASS** |
| Near-Miss evaluation steady-state allocation | 0 B/frame | **0 B** | **PASS** |
| Anti-Wall fairness over 500 spawn ticks | 0 4-lane walls | **0 simultaneous 4-lane walls** | **PASS** |
| Traffic pool object retention across retry | 0 leaked GameObjects | **0 leaked / exact 16 capacity maintained** | **PASS** |
| Teardown cleanup | 0 retained objects | **0 retained `TrafficVehicle` instances** | **PASS** |
| EditMode test suite | 587/587 passing | **587/587 passed (0 failed, 0 skipped)** | **PASS** |
| PlayMode test suite | 54/54 passing | **54/54 passed (0 failed, 0 skipped)** | **PASS** |
| Total project automated tests | 641 passing | **641 / 641 passed (100% green)** | **PASS** |

## 59. Phase 12 Stage 2: Road Coins, Live Race Economy Ledger, Zero-Alloc HUD & Atomic Persistence

### 59.1 Architecture & Component Topology
Phase 12 Stage 2 introduces collectible road coin arcs, live race economy aggregation, zero-allocation HUD presentation, and anti-exploit atomic persistence:
1. `CoinSpawnConfig` (`VelocityRush.Data`): Serializable configuration defining pool capacity (`36`), base coin value (`10` credits), near-miss bonus (`25` credits), pickup radius (`2.4m`), spawn ahead distance (`140m`), despawn behind distance (`25m`), arc length (`3..5` coins), intra-arc spacing (`7.0m`), inter-arc minimum spacing (`45.0m`), hover height (`0.75m`), and spin speed (`180 deg/s`).
   - Fail-closed validation (`TryValidate`) guarantees positive dimensions and safe bounds.
2. `CoinPickup` & `CoinPickupPool` (`VelocityRush.Gameplay.Economy`):
   - `CoinPickup`: Visual component providing procedural golden disc mesh and cached single shared material fallback (0 texture bloat, 0 Colliders/Rigidbodies). Rotates visually via `TickSpin` at 0 B GC cost.
   - `CoinPickupPool`: Pre-warms fixed capacity array of `CoinPickup` instances at initialization. Runtime `TryRent`, `Return`, `ResetAll`, and `Teardown` operate at strict 0 B GC allocation.
3. `RoadCoinManager` (`VelocityRush.Gameplay.Economy`):
   - Spawns multi-coin arcs along the active track spline in deterministic lanes (`0..3`) using seeded pseudo-random distribution.
   - Traffic vehicle avoidance: checks longitudinal and lateral clearance (margin $\ge 6.0\text{m}$) against active traffic in `TrafficLaneManager` to prevent coins spawning inside traffic vehicles.
   - Zero-allocation proximity pickup detection: iterates backwards through active coins checking player distance against $R_{\text{pickup}} = 2.4\text{m}$. On pickup, recycles coin to pool, updates session tallies, and fires `OnCoinCollected` value event.
4. `RaceEconomyTracker` (`VelocityRush.Gameplay.Economy`):
   - Pure C# runtime ledger tracking `SessionCoinsCollected`, `SessionCoinCredits`, `SessionNearMissCredits`, and combo streaks.
   - Calculates combo multiplier bonus: $\text{bonus} = \text{baseCredits} \times (1 + (\text{combo} - 1) \times 0.25)$.
   - Exploit-proof reset: `Reset()` wipes session tallies clean upon race restart or quit, preventing double-counting or farming.
   - Atomic idempotent persistence: `CommitToProgress(PlayerProgress progress)` adds `TotalBonusCredits` directly to player progress credits atomically only once upon verified race completion.
5. Zero-Allocation HUD & Itemized Results Presentation:
   - `HudStringFormatter` (`VelocityRush.Core.UI`): Static buffer writers `WriteBonusCredits` and `WritePopup` write formatted UTF-16 character sequences into caller-owned `char[]` buffers without string allocations.
   - `RaceHudPresenter` (`VelocityRush.Gameplay.UI`): Maintains live bonus credits counter (`BONUS: +X`) and pop-in animated near-miss/coin banners at 0 B/frame steady state.
   - `ResultsPresenter` (`VelocityRush.Gameplay.UI`): Displays itemized breakdown rows for Road Coins (`+X CR`) and Near-Miss Bonus (`+Y CR`) alongside standard placement rewards.
   - `LocalizationTable` (`VelocityRush.Data.Localization`): 4 bilingual EN/AR keys added with 100% parity (`hud.bonus_credits`, `hud.near_miss_popup`, `results.road_coins`, `results.near_miss_bonus`).
6. Composition Root Integration (`GameCompositionRoot`):
   - Dynamically resolves and wires `RoadCoinManager` and `RaceEconomyTracker`.
   - Hooks `OnCoinCollected` and `NearMissDetector.OnNearMiss` to live HUD popups and economy ledger.
   - Hooks race finish event to commit economy bonus credits atomically to `PlayerProgress` and trigger immediate persistence via `SaveService.SaveNow()`.
   - Cleanses ledger on in-scene fast retry and menu return.

### 59.2 Zero-Allocation Discipline & Test Evidence
All hot-path operations across `RoadCoinManager.Tick`, `CoinPickup.TickSpin`, `RaceEconomyTracker.RecordCoin`, `RaceEconomyTracker.RecordNearMiss`, and `RaceHudPresenter.Tick` operate with zero heap allocations (0 B/frame).

| Metric / Requirement | Target Budget | Observed Result | Status |
|---|---|---|---|
| Coin manager steady-state tick allocation | 0 B/frame | **0 B** (`GC.GetAllocatedBytesForCurrentThread`) | **PASS** |
| HUD bonus & popup steady-state tick allocation | 0 B/frame | **0 B** | **PASS** |
| Coin pool object retention across retry | 0 leaked GameObjects | **0 leaked / exact pre-warmed capacity maintained** | **PASS** |
| Atomic persistence on race finish | Committed & saved | **Verified: progress credits updated & saved to disk** | **PASS** |
| Exploit protection on fast retry / quit | 0 unearned credits | **0 credits leaked or committed to progress** | **PASS** |
| EditMode test suite | 593/593 passing | **593/593 passed (0 failed, 0 skipped)** | **PASS** |
| PlayMode test suite | 59/59 passing | **59/59 passed (0 failed, 0 skipped)** | **PASS** |
| Total project automated tests | 652 passing | **652 / 652 passed (100% green)** | **PASS** |

## 60. Phase 12 Stage 3: Nitro Boost, Camera Juice, Rival Rubber-Banding & Zero-Alloc HUD

### 60.1 Architecture & Component Topology
Phase 12 Stage 3 introduces high-octane nitro boost mechanics, dynamic camera juice, intelligent rival rubber-banding with traffic avoidance, and zero-allocation HUD elements:
1. `NitroConfig` (`VelocityRush.Data`) & `NitroSystem` (`VelocityRush.Gameplay`):
   - `NitroConfig`: Pure serializable configuration defining `maxCharge` (100), `minActivationCharge` (15), `drainRatePerSecond` (28), `passiveRegenPerSecond` (2.5), `nearMissChargeBonus` (22), `driftChargePerSecond` (14), `boostTorqueMultiplier` (1.45), and `boostMaxSpeedMultiplier` (1.22). Fail-closed validation (`TryValidate`) rejects invalid parameters.
   - `NitroSystem`: Pure C# state machine. Enforces activation threshold hysteresis: once active, drains until empty or input released. Charges passively at high speeds, during lateral drifts, and instantly on near-misses. 0 B heap allocation.
2. Non-Mutating Runtime Vehicle Boost Application (`VehicleController`):
   - `SetNitroBoostActive(bool active, float torqueMult, float speedMult)`: Dynamically multiplies motor torque by `1.45` and reduces chassis linear damping by `1 / 1.22`, expanding the vehicle's terminal speed without mutating underlying `VehiclePhysicsProfile` ScriptableObject assets.
3. `ArcadeCameraJuice` (`VelocityRush.Gameplay`):
   - Executes in `LateUpdate` after `VehicleCameraRig`.
   - **Dynamic Speed + Nitro FOV:** Smoothly interpolates `Camera.fieldOfView` based on speed + `+11°` nitro boost kick, strictly clamped within safe WebGL boundaries `[60°, 84°]`.
   - **Damped Impulse Shake:** Directional Perlin shake triggered by traffic collisions and nitro ignition, decaying cleanly to `Vector3.zero` offset. Freezes on pause (`Time.timeScale == 0`) and clears immediately on retry.
4. Smart Rival Rubber-Banding & Traffic Avoidance (`VelocityRush.Data` / `VelocityRush.Gameplay.AI`):
   - `RubberBandConfig` & `RubberBandCalculator`: Pure static math clamping rival pace multipliers strictly to `[0.85f, 1.18f]`. Enforces a ±15m dead-zone around player where multiplier is strictly 1.0f, with bounded trailing catch-up (up to +12%) and lead rival damping (down to -8%). Rivals never teleport or stall.
   - `RivalTrafficAvoidance`: Queries active traffic from `TrafficLaneManager` and calculates adjacent open lane offsets, allowing AI drivers to smoothly weave through highway traffic.
5. Zero-Allocation HUD Nitro Bar & Rival Gap Presentation:
   - `HudStringFormatter.WriteRivalGap`: Writes signed relative distance (`+24m` / `-18m`) into caller-owned `char[]` buffer without allocations.
   - `RaceHudPresenter`: Displays quantized nitro bar fill with boost glow state and live lead rival distance badge.
   - `LocalizationTable`: 3 bilingual EN/AR keys added with 100% parity (`hud.nitro`, `hud.rival`, `hud.boost_active`).
6. Composition Root Integration (`GameCompositionRoot`):
   - Binds boost input (Shift / Space) and drift telemetry to `NitroSystem`.
   - Binds near-miss events to instant nitro charge and traffic collisions to camera shake impulses.
   - Computes signed distance to lead rival and feeds HUD gap badge.
   - Cleanly resets nitro charge, camera shake, and rival state on in-scene fast retry and quit to menu.

### 60.2 Zero-Allocation Discipline & Test Evidence
All hot-path operations across `NitroSystem.Tick`, `ArcadeCameraJuice.LateUpdate`, `RubberBandCalculator.CalculateMultiplier`, `RivalTrafficAvoidance.EvaluateTargetLateralBias`, and `RaceHudPresenter.SetNitroCharge`/`SetRivalGap` operate at strict 0 B/frame.

| Metric / Requirement | Target Budget | Observed Result | Status |
|---|---|---|---|
| Nitro state machine steady-state tick allocation | 0 B/frame | **0 B** (`GC.GetAllocatedBytesForCurrentThread`) | **PASS** |
| Rubber-band & Rival gap hot path allocation | 0 B/frame | **0 B** | **PASS** |
| HUD Nitro & Rival gap steady-state tick allocation | 0 B/frame | **0 B** | **PASS** |
| Vehicle physics profile immutability | Unchanged | **Verified: 0 field mutations on base ScriptableObject** | **PASS** |
| Camera FOV WebGL clamp range | [60°, 84°] | **Verified: clamped strictly within [60°, 84°]** | **PASS** |
| Camera shake decay on retry | Vector3.zero | **Verified: offset exactly Vector3.zero on reset** | **PASS** |
| Rubber-band pace multiplier bounds | [0.85, 1.18] | **Verified: strictly within [0.85, 1.18] across all gaps** | **PASS** |
| EditMode test suite | 600/600 passing | **600/600 passed (0 failed, 0 skipped)** | **PASS** |
| PlayMode test suite | 63/63 passing | **63/63 passed (0 failed, 0 skipped)** | **PASS** |
| Total project automated tests | 663 passing | **663 / 663 passed (100% green)** | **PASS** |

## 61. Phase 12 Stage 4: 15-Event Difficulty Matrix, Combined Stability & Phase 12 Closeout

### 61.1 Architecture & Component Topology
Phase 12 Stage 4 delivers the complete 15-event progressive difficulty matrix, dynamic composition root wiring, 10-cycle combined Highway Rival stability stress testing, and commercial WebGL release verification:
1. `HighwayDifficultyProfile` & `HighwayDifficultyTable` (`VelocityRush.Data`):
   - `HighwayDifficultyProfile`: Immutable readonly struct defining `EventId`, `TrafficDensity` (`0.25f..0.85f`), `TrafficMinSpeed`, `TrafficMaxSpeed`, `CoinArcSpacing`, `CoinValue` (10 in C1, 15 in C2, 20 in C3), `NearMissBonusCredits` (25 in C1, 35 in C2, 50 in C3), `RivalBasePaceMultiplier` (C1 0.88..0.94, C2 0.95..1.00, C3 1.01..1.06), and `RubberBandConfig` (clamped strictly within `[0.85f, 1.18f]`). Fail-closed validation (`TryValidate`) rejects invalid parameters.
   - `HighwayDifficultyTable`: Deterministic static registry covering all 15 M8 events with safe default fallback to `c1-ignition-series-round-1`. Operates with zero heap allocations on query.
2. Dynamic Event Wiring in Composition Root (`GameCompositionRoot`):
   - Resolves `HighwayDifficultyProfile` upon launching any championship event.
   - Reconfigures `TrafficLaneManager` via `ApplyConfig` (density and speed limits).
   - Reconfigures `RoadCoinManager` via `ApplyConfig` (coin values, near-miss credit scaling, and arc spacing).
   - Reconfigures active `AIDriver` rivals in Circuit races via `SetSmartRubberBand`, `SetTrafficManager`, and `SetPaceScale` (skipping rivals cleanly in solo Time Trials).
   - Cleanses all race references on return to menu via `CleanupActiveRaceReferences`.
3. 10-Cycle Combined Highway Rival Stability Stress Test (`Phase12HighwayRivalStabilityPlayModeTests`):
   - Executes 10 consecutive full-loop cycles (`MainMenu -> EventSelection -> PreEvent -> Race -> Results -> MainMenu`) with Traffic + NearMiss + Coins + Nitro + CameraJuice + AIDriver Rubber-Banding + HUD running concurrently.
   - Asserts 0 retained `TrafficVehicle`, `CoinPickup`, `RaceController`, or `VehicleController` instances after scene unload.
   - Asserts 0 B/frame combined steady-state hot path allocation across all Phase 12 systems.
   - Asserts managed heap growth <= 512 KB across cycles 2–10 after warmup (zero monotonic leak).
4. WebGL Commercial Build Verification:
   - Built batch WebGL Development player using profile `Phase11Commercial` (13 scenes: `Bootstrap -> world-arena-reverse`).
   - Achieved 0 build errors in 56.788 s with 63.89 MB (63,889,568 B) total payload size (well under the 250 MB ceiling, delta +239,568 B over Phase 11 baseline).
   - Restored prototype scenes and Standalone Linux 64 build target cleanly.

### 61.2 Measured Evidence & Test Metrics

| Metric / Requirement | Target Budget | Observed Result | Status |
|---|---|---|---|
| Combined Phase 12 steady-state tick allocation | 0 B/frame | **0 B** (`GC.GetAllocatedBytesForCurrentThread`) | **PASS** |
| 10-cycle retained scene objects | 0 retained | **0 retained** (`TrafficVehicle`, `CoinPickup`, `RaceController`, `VehicleController`, `AIDriver`) | **PASS** |
| 10-cycle cumulative managed heap drift | ≤ 512 KB | **Verified stable (zero monotonic leak)** | **PASS** |
| Traffic density safe bounds | [0.25, 0.85] | **Verified: strictly within [0.30, 0.82] across all 15 events** | **PASS** |
| Rubber-band pace multiplier bounds | [0.85, 1.18] | **Verified: strictly within [0.85, 1.18] across all 15 events** | **PASS** |
| Coin values across tiers | 10 -> 15 -> 20 | **C1: 10 CR, C2: 15 CR, C3: 20 CR** | **PASS** |
| Near-miss bonus credits across tiers | 25 -> 35 -> 50 | **C1: 25 CR, C2: 35 CR, C3: 50 CR** | **PASS** |
| WebGL Development build payload size | ≤ 250 MB | **63,889,568 B (63.89 MB / 60.93 MiB)** (25.5% of ceiling) | **PASS** |
| WebGL Development build errors | 0 errors | **0 errors, 3 advisory warnings** | **PASS** |
| WebGL Development build duration | < 120 s | **56.788 s** | **PASS** |
| EditMode test suite | 605/605 passing | **605/605 passed (+5 new, 0 failed, 0 skipped)** | **PASS** |
| PlayMode test suite | 64/64 passing | **64/64 passed (+1 new, 0 failed, 0 skipped)** | **PASS** |
| Total project automated tests | 669 passing | **669 / 669 passed (100% green)** | **PASS** |
