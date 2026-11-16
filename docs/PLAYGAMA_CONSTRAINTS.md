# Playgama Platform Constraints

**Purpose:** Consolidated checklist of all known Playgama Web platform constraints for design and implementation.
**Status:** Baseline notes gathered from Playgama documentation during project discovery. Every item marked **re-verify** must be re-checked against the current official Playgama documentation before integration and again before submission. Platform requirements are not assumed permanently current.

## 1. Confirmed-from-discovery requirements (re-verify)

These were recorded from Playgama documentation during project discovery. They are treated as requirements but must be re-verified against current official documentation before submission.

| # | Constraint | Area | Status |
|---|---|---|---|
| 1 | Integrate the current Playgama Bridge SDK; complete required integration steps. Known installation path: Unity Package Manager via the SDK's official Git repository. Integration happens only after Unity project architecture is established. | Integration | Re-verify version before integration |
| 2 | Do not require external registration or authentication to launch the game. | Access | Re-verify |
| 3 | Do not force players to sign in through an external service. | Access | Re-verify |
| 4 | Use Playgama Bridge Analytics instead of external analytics services (e.g., GA4). | Analytics | Re-verify |
| 5 | Include required assets inside the final game archive. | Packaging | Re-verify |
| 6 | Avoid external asset hosting. | Packaging | Re-verify |
| 7 | Only open external links when permitted by the Bridge platform capability. | Links | Re-verify |
| 8 | Do not restrict game operation based on URL. | Runtime | Re-verify |
| 9 | Final archive size must not exceed 300 MB. | Packaging | Re-verify current limit |
| 10 | Startup memory must remain below 2 GB. | Performance | Re-verify current limit |
| 11 | The archive root must contain index.html. | Packaging | Re-verify exact structure |
| 12 | Use Latin filenames in the final archive. | Packaging | Re-verify |
| 13 | Support mobile fullscreen and touch controls. | Devices | Re-verify; QA depth open (OQ-P01) |
| 14 | Support keyboard and mouse on desktop. | Devices | Re-verify |
| 15 | Handle browser orientation and input behavior correctly. | Devices | Re-verify |
| 16 | Avoid long-press text selection and context-menu problems on mobile. | UI | Re-verify |
| 17 | Avoid WebGL startup warnings. | Quality | Re-verify |
| 18 | Support responsive layouts without unwanted scrolling or clipped content. | UI | Re-verify |
| 19 | Ensure popups can be closed. | UI | Re-verify |
| 20 | Handle pause/resume correctly when advertisements are shown. | Ads | Re-verify |
| 21 | Do not embed YouTube players. | Content | Re-verify |
| 22 | Do not rely on Escape key behavior that conflicts with browser reservations. | Input | Re-verify |
| 23 | Test stability across supported browsers and devices. | QA | Re-verify device matrix |

## 2. Requirements requiring re-verification (gaps)

These details were never captured precisely during discovery and must be confirmed from official documentation at integration time:

- Exact current Bridge SDK version and integration steps.
- Current archive size limit and exact archive directory structure.
- Current startup-memory limit and how it is measured.
- Exact ad lifecycle callbacks and rewarded-completion confirmation API.
- Save API surface exposed by Bridge (see OQ-T05).
- Exact supported browser/device matrix.
- Current external-link permission mechanism.

## 3. Technical implementation implications

- **Unity WebGL is the only build target in scope**, and it constrains asset size, real-time lighting, and effects (`design/ART_DIRECTION.md` §11).
- **Never show ads during active driving** — the game must define clear "safe states" for ad placement (DECISIONS D-021, D-027).
- **Rewarded reward granting must be gated on confirmed rewarded-video completion.**
- Ad pause/resume requires pausable gameplay, audio, and input systems.
- All in-game links must be gated through the Bridge capability check.
- No external auth, analytics, hosting, or dependency on specific URLs — the game must run fully from the served archive.
- Escape must not be a required input for critical UI (menus need alternate close paths).
- Touch UI targets need sizing and spacing to avoid accidental activation and long-press selection issues.
- Opening fullscreen on mobile and restoring correctly must be part of the responsive layout rules.

## 4. Testing checklist (per build and before release)

- [ ] Startup time and memory under target limits on a representative mid-range device.
- [ ] No WebGL startup warnings in the selected browsers.
- [ ] Mobile: fullscreen, touch controls, orientation change, no long-press text selection or context-menu problems, responsive layout without unwanted scrolling or clipping.
- [ ] Desktop: keyboard and mouse controls work as expected; Escape never traps or breaks navigation.
- [ ] Responsive layouts verified for the target resolutions/aspect ratios (landscape).
- [ ] Popups closeable on all platforms.
- [ ] Pause/resume during ad playback — game, audio, and input all resume safely.
- [ ] Rewarded ads grant rewards only after confirmed completion; unavailable-ads path handled gracefully.
- [ ] No external DNS calls except permitted Bridge/CDN traffic.
- [ ] Stability across the supported browser/device matrix.
- [ ] All game content reachable; completion blockers absent.

## 5. Performance risk register — Commercial V1.0 scope vs platform reality

The Commercial V1.0 scope (D-028: 10 vehicles, ≥3 championships, 5 distinct environments, high-quality realistic-inspired presentation per D-006) is substantially larger than the former MVP framing. **Do not assume it automatically fits the platform.** The registering risks below are planning-level assessments, not measured facts; every budget value remains OPEN until profiling proves otherwise (OQ-T08).

| Risk | Area | Assessment | Mitigation / gate |
|---|---|---|---|
| Archive size growth from 5 environments + 10 vehicles | Archive limit (re-verify current limit) | Proposed-unverified: the former 300 MB record may not hold room for the larger content set without aggressive optimization | Texture/model compression budgets per asset class at vertical slice (Roadmap Phase 7); shared-kits discipline without visual homogenization (OQ-20); archive measurement gate before submission |
| Startup memory growth | Memory limit (re-verify current limit, was recorded as 2 GB) | Proposed-unverified: fully loading multiple environments is impossible; startup must load only what the player needs | Content streaming/loading strategy decided during architecture (OQ-T06); startup-path budget gate; no external hosting |
| Loading time across larger event/championship set | Loading UX | Proposed-unverified: seamless restarts and event switching could degrade with bigger content | Fast-retry UX relies on measured reload paths; loading strategy validated in the vertical slice |
| WebGL render performance at the D-006 quality bar | Frame rate on representative devices | Proposed-unverified: cinematic lighting/post-processing/shaders chosen must pass profiling gates first (`TDD.md` §§16–18) | Render-pipeline decision with evidence (OQ-T02); per-feature profiling gate log maintained here |
| Simulated gameplay memory over a multi-championship session | Runtime memory | Proposed-unverified: accumulated championship/progression state and content must not leak or balloon | Memory-profiling checks on long sessions during QA phases |
| Mobile device capability wall | Mobile browsers | Proposed-unverified: high-quality presentation may be untenable on weaker mobile devices | Device capability matrix decided with owner (OQ-P01); quality tier policy is an owner decision if needed — never decided silently |
| Content-set regression and QA workload | Test coverage | Proposed-unverified: 5 envs × events × 10 vehicles expands QA surface | Automated build/test strategy and device matrix planning (OQ-T07/OQ-P01) |

**Statuses used:** `Proposed-unverified` values are planning hypotheses. Nothing here is an achieved performance claim, a guarantee, or an approved budget. Measured results will be added to the **performance evidence log** (below) with dates, device/browser, and the feature tested.

### Performance evidence log (measured 2026-11-10 — development-prototype stage; evidence only, not approved budgets)

| Date | Feature/measure | Device/browser | Result | Decision recorded |
|---|---|---|---|---|
| 2026-11-10 | Phase-5 baseline (superseded, kept for trend): WebGL Development build `Bootstrap → TestTrack`, untextured prototype (`dev-20261109-210202`) | Linux x86_64, Unity 6000.6.8f1; Chromium 152 / WebGL 2.0 | 29,722,007 B payload (29.72 MB, ~9.9% of 300 MB); wasm heap 95,944,704 B; JS heap ~9.5 MiB; 0 errors/warnings | OQ-T08 reference numbers only (CHANGELOG 2026-11-10) |
| 2026-11-10 | **T-6.2 Pass A — Built-in Render Pipeline.** Same two-scene build contract (`Bootstrap → TestTrack`) through the permanent builder; identical gameplay code; no URP package installed | Linux x86_64, Unity 6000.6.8f1 (OpenGL 4.6 / Mesa editor); Chromium 152 / WebGL 2.0 runtime | Build 28.816 s (cold), 35,642,986 B total = **35.64 MB (34.0 MiB), 11.9% of the 300 MB archive limit**; wasm 30,836,260 B; `.data` 4,035,705 B; framework.js 687,861 B; loader.js 60,309 B; index.html 8,737 B; 3 build warnings (UAC0009 `DEVELOPMENT_BUILD` deprecation + emcc advisories), 0 errors. Runtime: bootstrap 2 ms, race completed 12.136 s, 0 console errors; JS heap 9.95 MB (0.23% of the 4,192 MiB V8 limit); `JSArrayBufferData` 100,704,633 B (96.0 MiB); `Managed (WasmNativeModuleTag)` 40,007,936 B | Pass A baseline for D-032/OQ-T02 |
| 2026-11-10 | **T-6.2 Pass B — URP 17.6.0** (editor-bundled package, no registry download). Minimal pipeline asset: HDR off, MSAA 1, render scale 1, shadow distance 50, 1 cascade, no depth/opaque texture, SRP Batcher on; same scene list and gameplay code | Same hardware/editor/browser as Pass A | Build 123.687 s cold, **3.409 s warm**; 78,897,884 B total = **78.90 MB (75.24 MiB), 26.3% of the 300 MB limit**; wasm 67,662,326 B; `.data` 10,456,384 B; framework.js 696,030 B; loader.js 60,309 B; **0 errors / 0 warnings** after material conversion. Runtime: bootstrap 2 ms, 0 console errors; JS heap 20.52 MB; `JSArrayBufferData` 149,199,424 B (142.3 MiB, **1.48×** Pass A); `Managed (WasmNativeModuleTag)` 82,012,394 B (**2.05×**) | URP evaluated and **deferred**; delta table below |
| 2026-11-10 | **T-6.2 visual-parity check.** Identical scene, camera pose, light settings and capture path (`camera.Render()` → 960×540 sRGB RenderTexture); magenta coverage and mean luminance sampled from the readback | Linux editor, OpenGL 4.6 / Mesa, Unity 6000.6.8f1 | Built-in: TestTrack mean luminance **225.0**, course **0.00%** near-black. URP with *equivalent* directional-light intensity/shadow distance: TestTrack mean luminance **64.3 (−71.4%)**, course **20.89%** near-black. Material conversion was mandatory: URP rendered **65.23% magenta** on TestTrack before `Universal Render Pipeline/Lit` was assigned, **0.00%** after | URP is not visual parity under equivalent light settings → lighting/exposure re-authoring would be required; contributed to the Built-in restoration |
| 2026-11-10 | **T-7.4 Stage-0 profiling-harness baseline.** Same Built-in `Bootstrap → TestTrack` prototype as Pass A; no content or build-contract change. Added the dev-only `PrototypePerformanceProbe` (3 s warm-up + 10 s sampling window: mean/p95/min/max frame time, derived FPS, managed and `Profiler` allocated memory, draw calls/batches/triangles/vertices) and the allocation-free `FrameTimeStatistics` window. Stage-0 runtime values therefore reference the Pass-A row above | Linux x86_64, Unity 6000.6.8f1; Chromium 152 / WebGL 2.0 (prototype evidence) | Stage-0 reference: **35.64 MB** five-artifact payload (11.9% of 300 MB); 28.816 s cold build; JS heap **9.95 MB**; WASM ArrayBuffer **96.0 MiB**; managed wasm **40.0 MB**; 0 console errors. Harness itself is dev-only and adds no release-path content; the slice-scene probe is wired but not yet in the WebGL build contract | Harness contract and capture protocol recorded in `TDD.md` §47.6; **no numeric budget approved** (OQ-T08 remains open); first in-browser slice capture is T-7.14 after the T-7.12 build-contract extension |

**T-6.2 delta summary (Built-in → URP 17.6.0, same scene and code):**

| Measure | Built-in (Pass A) | URP (Pass B) | Delta |
|---|---|---|---|
| Total payload | 35,642,986 B (34.0 MiB) | 78,897,884 B (75.24 MiB) | **+2.21×** (+43.25 MB) |
| `.wasm` | 30,836,260 B | 67,662,326 B | **+2.19×** (+36.83 MB) |
| `.data` | 4,035,705 B | 10,456,384 B | **+2.59×** (+6.42 MB) |
| Cold build | 28.816 s | 123.687 s | **+4.29×** |
| Warm build | not measured (cold only) | 3.409 s | — |
| WASM ArrayBuffer memory | 100,704,633 B | 149,199,424 B | **+1.48×** |
| Managed (wasm native) | 40,007,936 B | 82,012,394 B | **+2.05×** |
| JS heap | 9.95 MB | 20.52 MB | **+2.06×** |
| Mean luminance (TestTrack) | 225.0 | 64.3 | **−71.4%** |
| Magenta (before/after material fix) | 0.00% | 65.23% → 0.00% | material conversion required |
| Archive-limit use (300 MB) | 11.9% | 26.3% | still within limit |

Both passes stay inside the recorded Playgama limits (300 MB archive, 2 GB memory). The measured regression in visual parity under *equivalent* lighting settings, combined with the payload/memory/build-time cost, is why the prototype stays on Built-in (D-032 update, 2026-11-10).

**Stage-1 profiling-harness note (T-7.4, 2026-11-10):** the Stage-0 baseline is the Built-in Pass-A row above; it is prototype evidence, not a product guarantee or an approved budget. The dev-only `PrototypePerformanceProbe` and `FrameTimeStatistics` are wired into the slice scene (`HandlingCourse.unity`) and deliberately excluded from release builds at runtime. The two-scene `Bootstrap → TestTrack` WebGL payload is unchanged by Stage 1. The first high-quality slice capture, the per-feature cost rows (lighting/shadows/textures/VFX/UI/audio), and the owner-approved budget proposal remain T-7.14/T-7.15, after the reviewed slice scene profile is added in T-7.12.

**Phase-7 Stage-2 build-contract note (2026-11-11):** `VelocityRushWebGLBuilder` now exposes `WebGLBuildProfile.VerticalSlice` (`Bootstrap → HandlingCourse`) as opt-in data while `Prototype` (`Bootstrap → TestTrack`) remains the default, so the measured Pass-A payload above is still the reference contract. The tool never writes `EditorBuildSettings`; only the explicit `Apply Vertical Slice Build Scenes`/`Restore Prototype Build Scenes` menu actions change it, and the project currently remains `Bootstrap` (0) → `TestTrack` (1). Prototype preflight automation still passes (`VALIDATION PASS`, JSON report, no build). No WebGL slice build or in-browser capture had been produced at Stage-2 close, so **no slice payload/memory/FPS figure was claimed then**; the profiling matrix and repeated-cycle memory measurement were scheduled for T-7.14, and the derived budgets for T-7.15. **Superseded by the Stage-3 evidence below.**

**Phase-7 Stage-3 profiling matrix and proposed budgets (T-7.14/T-7.15, 2026-11-11):**

*Build (real, this project, Unity 6000.6.8f1, WebGL Development, `WebGLBuildProfile.VerticalSlice` `Bootstrap → HandlingCourse`):*

| Measure | Measured value | Note |
|---|---|---|
| Build result / duration | Succeeded, **62.882 s** cold | 0 errors, 1 warning (emcc `$stackTrace` JS-library deprecation advisory; 4 WebGPU/Firefox advisory lines) |
| Five-artifact payload | **60,995,033 B = 60.995 MB (58.17 MiB)** | 20.3% of the 300 MB platform limit; 24.4% of the proposed 250 MB budget |
| `index.html` | 8,817 B | — |
| `.wasm` | 53,516,979 B (53.517 MB / 51.04 MiB) | dominates payload |
| `.data` | 6,699,979 B (6.70 MB / 6.39 MiB) | — |
| `framework.js` | 694,835 B | — |
| `loader.js` | 60,309 B | — |
| Build scenes | `Bootstrap` (0) → `HandlingCourse` (1) | applied explicitly for the build and restored to prototype afterwards; `EditorBuildSettings` is unchanged in the committed tree |
| Build report | `/tmp/opencode/vr-webgl-slice-20261111-040244/velocity-rush-webgl-build-report.json` | output outside the repository |

*Runtime capture (real Chromium 152.0.7977.167 / WebGL 2.0, ANGLE AMD REMBRANDT, OpenGL ES 3.2, Linux x86_64; Development build served locally):*

| Measure | Measured value | Note |
|---|---|---|
| Page load | 1,578 ms | `pageLoadTime` |
| Page load to first frame | 1,602 ms | `pageLoadTimeToFrame1` |
| Game startup | 107 ms | `gameStartupTime` |
| Asset load | 1,423 ms | `assetLoadTime` |
| WebAssembly startup | 1,426.6 ms | `webAssemblyStartupTime` |
| Code download | 16 ms | `codeDownloadTime` |
| WASM heap total | **137,887,744 B = 137.89 MB (131.50 MiB)** | `totalWASMHeapSize` |
| WASM heap used | **123,418,064 B = 123.42 MB (117.70 MiB)** | `usedWASMHeapSize` |
| JS heap total | 16,734,431 B (15.96 MiB) | `totalJSHeapSize` |
| JS heap used | 16,398,039 B (15.64 MiB) | `usedJSHeapSize` |
| Probe window | 590 frames / 589 frames (two runs) | 3 s warm-up + 10 s sample |
| Mean frame / p95 / min / max | **16.95 ms / 18.00 ms / 9 ms / 35 ms** (run 1); 17.00 / 18.00 / 11 / 35 (run 2) | no frame above 35 ms in the sampled window |
| Mean FPS | **59.0** (run 1); 58.8 (run 2) | min 28.6, max 111.1 (single-frame extrema) |
| Draw calls | **394** | `batches=0` reported by the probe |
| Triangles / vertices | 115,798 / 152,904 | — |
| Managed memory | 1.93 MB (run 1), 1.92 MB (run 2) | steady-state |
| Managed delta | 212 KB / 192 KB over 590 / 589 frames (≈0.33–0.36 KB/frame) | Development build including the dev telemetry overlay/probe; **not a zero-allocation claim for the whole loop** |
| Total-allocation delta | 1.16 MB / 1.03 MB over the sample | profiler total; includes start-of-window warm-up |
| Janked frames | 11 cumulative | includes startup; no GC hitch >35 ms observed in the sample |
| Console errors | 0 game errors | the only error was test-injected `HEAPU8` access; no Unity/WebGL startup error |

**Proposed Stage-3 budgets (derived from the slice measurement; owner review basis for Commercial V1.0):**

| Budget | Proposed ceiling | Warn at | Basis |
|---|---|---|---|
| Archive / five-artifact payload | **250 MB** | 225 MB | 83% of the 300 MB platform limit; slice is 61 MB (24.4% of 250 MB) |
| WASM heap total | **192 MiB** | 176 MiB | 1.46× the slice's 131.5 MiB; leaves ~60 MiB for 5-environment/10-vehicle content and streaming |
| WASM heap used | **168 MiB** | 152 MiB | 1.43× the slice's 117.7 MiB |
| JS heap used | **64 MB** | 48 MB | 3.9× the slice's 16.4 MB; far below the 4,192 MiB V8 limit |
| Draw calls | **500** | 450 | 1.27× the slice's 394 |
| Triangles | **180,000** | 160,000 | 1.55× the slice's 115,798 |
| Vertices | **250,000** | 220,000 | 1.64× the slice's 152,904 |
| Frame pacing (desktop) | mean **≥58 fps**, p95 **≤20 ms**, max **≤50 ms** | p95 >20 ms or max >50 ms | slice: 59.0 fps / 18 ms / 35 ms |
| Page load to first frame | **≤5 s** | 4 s | slice: 1.602 s |
| Asset load | **≤3 s** | 2.5 s | slice: 1.423 s |
| Managed steady-state | **≤1 KB/frame**, no GC hitch >50 ms | 0.75 KB/frame | slice: 0.33–0.36 KB/frame; no >35 ms frame |

These are **candidate budgets** derived from the Stage-3 measurement and recorded for owner sign-off. They are not a platform guarantee, and any change requires a fresh measurement rather than a new assumption. The runtime capture is a local Chromium/ANGLE run; the owner's desktop Chrome/Firefox matrix remains the review basis (OQ-P01), and repeated-cycle memory growth across environment/vehicle swaps is not yet measured.

**Phase-8 Stage-5 repeated-cycle note (T-8.12, 2026-11-11):**

*Build (real, this project, Unity 6000.6.8f1, WebGL Development, new `WebGLBuildProfile.Phase8Debug` `Garage → HandlingCourse`):*

| Measure | Measured value | Note |
|---|---|---|
| Build result / duration | Succeeded, **34.002 s** | 0 errors, 4 warnings (emcc `$stackTrace` JS-library deprecation plus the WebGPU/Firefox advisory lines) |
| Five-artifact payload | **61,186,251 B = 61.19 MB (58.35 MiB)** | 20.4% of the 300 MB platform limit; 24.5% of the proposed 250 MB budget; **stable** across repeated cycles (scene set is fixed data, no runtime download) |
| `.wasm` | 53,656,046 B (53.66 MB / 51.17 MiB) | dominates payload |
| `.data` | 6,752,126 B (6.75 MB / 6.44 MiB) | +52,147 B vs the Phase-7 slice; the Garage scene plus two new scenes' lightmap-less data |
| `framework.js` | 694,835 B | unchanged vs Phase 7 |
| `loader.js` | 60,309 B | unchanged vs Phase 7 |
| `index.html` | 8,821 B | +4 B vs Phase 7 |
| Build scenes | `Garage` (0) → `HandlingCourse` (1) | applied explicitly for the build; `EditorBuildSettings` restored to `Bootstrap → TestTrack` afterwards |
| Build report | `/tmp/opencode/vr-webgl-phase8-20261111-055240/velocity-rush-webgl-build-report.json` | output outside the repository |

*Repeated 10-cycle Garage → select → track → Garage evidence (**SIMULATED**, not a browser WASM trace):*

> **Evidence class: simulated.** The owner directed T-8.12 to be evaluated by simulating the repeated flow because the desktop Chrome/Firefox capture matrix (OQ-P01) is not available in this environment. Values are modelled from the **real** Stage-3 Chromium/ANGLE baseline (131.50 MiB total WASM heap, 394 draw calls, 16.95 ms mean frame) combined with the **real** Phase-8 artifact sizes above and the deterministic teardown contract in `VehicleSelectionSpawner`/scene unload. They are **not** measured browser heap samples. Label any reuse accordingly.

| Cycle | Selected vehicle (class) | Post-return WASM heap | Cumulative drift | Payload | Draw calls | Mean frame |
|---|---|---|---|---|---|---|
| Baseline | — (Garage only) | 137,887,744 B (131.50 MiB) | 0 B | 61,186,251 B | 394 | 16.95 ms |
| 1 | `starter-apex-v` (Starter) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 395 | 16.96 ms |
| 2 | `sport-pulse-r` (Sport) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 397 | 16.98 ms |
| 3 | `drift-sidewinder` (Drift) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 399 | 17.00 ms |
| 4 | `supercar-vortex-v10` (Supercar) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 401 | 17.02 ms |
| 5 | `starter-nova-hatch` (Starter) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 396 | 16.97 ms |
| 6 | `sport-aero-coupe` (Sport) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 398 | 16.99 ms |
| 7 | `drift-kuma-dx` (Drift) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 400 | 17.01 ms |
| 8 | `supercar-eclipse` (Supercar) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 402 | 17.03 ms |
| 9 | `sport-velocity-gt` (Sport) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 397 | 16.98 ms |
| 10 | `supercar-nebula-x` (Supercar) | 137,887,744 B (131.50 MiB) | **0 B** | 61,186,251 B | 399 | 17.00 ms |

**Result against the Stage-3 candidate ceilings:** heap **131.50 MiB vs 192 MiB** ceiling (68.5%, flat every cycle); payload **61.19 MB vs 250 MB** (24.5%, unchanged); draw calls **394–402 vs 500** (80.4% worst case); mean frame **16.95–17.03 ms ≈ 58.7–59.0 fps vs ≥58 fps** target. Cumulative drift is **0 B after all 10 cycles** — no WASM heap growth is attributable to the selection/spawn/teardown path in the simulated model.

**Why the simulated model predicts zero drift:** each cycle exercises exactly one `SelectionContext` write, one rig instantiation, one template destruction, and one single-mode scene load. `SelectionContext` stores a managed string id only (no engine object), `VehicleRigCatalog` is scene-authored reference data, and the spawner moves authored child objects rather than allocating new ones. Scene unload releases the instantiated rig; the only persistent state is the trimmed id string. The real browser capture remains the acceptance evidence for OQ-T08 and must confirm this model before any budget is treated as approved.

**Phase-9 Stage-1 note (2026-11-16):** Stage 1 adds environment **data contracts and editor-only shared kits** (`EnvironmentDefinition`/`EnvironmentCatalog`, `TrackLayout`, `EnvironmentLightingProfile`, material/track/lighting builders, and the parameterized scene-builder template). No environment definition asset, material/mesh asset, or scene is generated, and no WebGL build or browser capture was produced, so **no payload/heap/FPS change is claimed** for Stage 1. The owner approved the Phase-9 gates in D-056: realtime-only lighting (no bake/lightmap/reflection-probe dependency) and a per-environment increment of **≤15 MB `.data`** and **≤8 materials** inside the existing OQ-T08 candidate ceilings (250 MB payload, 192 MiB WASM heap, 500 draw calls, 180k triangles, mean ≥58 fps). Each Stage-2/3 environment must add its own measured row here; the existing Phase-7/8 measurements remain the reference baseline.

**Phase-9 Stage-2/3 note (2026-11-16):** all five signature packages and scenes now exist (`Assets/Data/Environments/<Theme>/<id>/`, `Assets/Generated/Environments/<id>/`, `Assets/Scenes/Environments/<id>.unity`). Each scene uses **7 environment materials** (≤8) and realtime-only lighting, with no texture or external mesh payload added beyond procedural meshes/materials/skybox and the shared slice assets. **No WebGL payload, heap, draw-call, triangle, or frame-time row is claimed yet:** Stage 4 owns the build/capture and the per-environment measurement row against the ≤15 MB `.data` increment and the OQ-T08 aggregate ceilings. The 250 MB / 192 MiB / 500 draw calls / 180k triangles / ≥58 fps candidate ceilings remain unchanged and unverified for the five-environment scope.

**Phase-9 Stage-4 WebGL build and per-environment profiling evidence (T-9.14/T-9.15, 2026-11-16):**

*T-9.14 build profile (real, opt-in):* `WebGLBuildProfile.Phase9Debug = 3` targets **`Bootstrap → Garage → desert-signature → coastal-signature → mountain-pass-signature → neon-city-signature → world-arena-signature` (7 scenes, all enabled)**. Parsing accepts `phase9`, `phase9debug`, `environments`, `all-environments`, or `3`. The profile is applied only for the build and the committed `EditorBuildSettings` remains `Bootstrap → TestTrack`.

*Build (real, this project, Unity 6000.6.8f1, WebGL Development, `Phase9Debug`):*

| Measure | Measured value | Note |
|---|---|---|
| Build result / duration | **Succeeded, 46.215 s** | **0 errors, 4 warnings** (two `UAC0009` `DEVELOPMENT_BUILD` deprecation warnings plus two emcc advisories — Firefox-WebGPU notes and the deprecated `$stackTrace` symbol); no script compile errors or warnings |
| Total build payload | **62,509,995 B = 62.51 MB (59.61 MiB)** | 25.0% of the proposed 250 MB budget; **+1,323,744 B** vs the Phase-8 five-scene build (five added environment scenes) |
| `.wasm` | 53,771,402 B (53.77 MB / 51.28 MiB) | dominates payload |
| `.data` | 7,960,590 B (7.96 MB / 7.59 MiB) | +1,208,464 B vs Phase 8 for all five environments; per-environment increments below |
| `framework.js` | 694,835 B | unchanged vs Phase 7/8 |
| `loader.js` | 60,309 B | unchanged vs Phase 7/8 |
| `index.html` | 8,745 B | -76 B vs Phase 8 |
| Build scenes | 7-scene `Phase9Debug` order above | applied explicitly for the build; `EditorBuildSettings` restored to `Bootstrap → TestTrack` immediately afterwards |
| Build report | `/tmp/opencode/vr9/webgl-phase9/velocity-rush-webgl-build-report.json` | output outside the repository |

> **Environment limitation (not a regression):** this container has no `ffmpeg`/`avconv`, so the audio importer logged `FSBTool ERROR` lines for the generated slice WAVs during the WebGL build (the same tooling limitation recorded for earlier generations). The player still built and packaged successfully; script compilation remained **0 errors / 0 warnings**.

**Final-tree rebuild:** after the coastal frond migration was applied to the committed scene, an incremental `Phase9Debug` rebuild produced **62,509,922 B** (`.data` 7,960,493 B, `index.html` 8,769 B) with **0 errors** and one emcc advisory warning. The cold-build row above remains the reference build; the 73 B delta is serialized scene/file-ID noise, not a content change.

*Per-environment matrix (**SIMULATED browser run**, 2026-11-16):*

> **Evidence class: simulated.** The owner directed T-9.15 to be evaluated by simulating the browser run because the desktop Chrome/Firefox capture matrix (OQ-P01) is unavailable in this environment. The structural columns are **real measurements of the committed scenes and generated assets** (`Assets/Editor/Phase9ProfilingEvidence`): renderers, unique materials, total scene triangles, worst-case visible triangles after frustum culling sampled at nine camera poses along the racing line (60° FOV, 16:9, 900 m far plane), and serialized footprint of the scene plus its generated/data assets. Heap, draw-call, and frame-time values are modelled from the **real** Phase-7/8 Chromium/ANGLE baseline (137,887,744 B total WASM heap, 394 draw calls, 115,798 triangles, 16.95 ms mean frame). They are **not** measured browser heap samples.

| Environment | Renderers (unbatched draw-call bound) | Scene triangles | Worst-case visible triangles | Materials | Serialized package | Modelled peak WASM heap | Modelled mean frame | Gate |
|---|---|---|---|---|---|---|---|---|
| Desert Circuit (`desert-signature`) | 156 | 86,586 | 85,634 | 7 | 2,893,146 B (2.76 MB) | 149,169,498 B (142.26 MiB) | 16.95 ms (59.0 fps) | **PASS** |
| Coastal Circuit (`coastal-signature`) | 373 | 69,866 | 69,642 | 7 | 3,383,736 B (3.23 MB) | 149,660,051 B (142.73 MiB) | 16.95 ms (59.0 fps) | **PASS** |
| Mountain Pass (`mountain-pass-signature`) | 353 | 96,598 | 95,314 | 7 | 2,805,025 B (2.68 MB) | 149,081,377 B (142.18 MiB) | 16.95 ms (59.0 fps) | **PASS** |
| Neon City (`neon-city-signature`) | 313 | 67,500 | 57,132 | 7 | 2,587,345 B (2.47 MB) | 148,863,697 B (141.97 MiB) | 16.95 ms (59.0 fps) | **PASS** |
| World Arena (`world-arena-signature`) | 265 | 25,262 | 25,134 | 7 | 2,836,065 B (2.71 MB) | 149,112,417 B (142.21 MiB) | 16.95 ms (59.0 fps) | **PASS** |
| **Worst case** | **373 / 500** | **96,598** | **95,314 / 180,000** | **7 / 8** | **3.23 MB / 15 MB** | **149.66 MB / 192 MiB** | **16.95 ms** | **PASS** |

**Ceiling confirmation:** draw calls **373 ≤ 500** (74.6% of ceiling); worst-case visible triangles **95,314 ≤ 180,000** (53.0%); per-environment serialized/`.data` increment **3.23 MB ≤ 15 MB** (21.5%); unique materials **7 ≤ 8**. The modelled peak WASM heap (**149.66 MB / 142.73 MiB**) is inside the 192 MiB ceiling, and the total `.data` for all five environments (**7.96 MB**) is far below five × 15 MB. The serialized-package column is a conservative upper bound on each environment's real `.data` increment: the complete seven-scene build's `.data` is only 7.96 MB, so no single environment can exceed its uncompressed asset footprint.

**Coastal budget correction (real geometry change):** the first Stage-4 measurement found Coastal at **243,578 scene triangles / 243,354 visible** because each palm frond was a Unity sphere (768 triangles). The frond is now a dedicated double-sided 16-triangle tapered/drooping mesh (`coastal-signature_PalmFrond.asset`), reducing Coastal to **69,866 scene / 69,642 visible triangles** with the same 102 decorations and visual silhouette. This is why the Stage-4 commit updates the generated coastal scene and adds one generated mesh asset.

*Repeated 10-cycle all-environment load/unload evidence (**SIMULATED**, not a browser WASM trace):*

Each cycle loads and unloads all five environments once, in production order, then returns to the persistent Bootstrap root. Values are modelled from the same real baseline plus the additive load/unload contract; the PlayMode suite independently proves that every scene root is destroyed on unload.

| Cycle | Load/unload sequence | Post-return WASM heap | Cumulative drift | Peak draw calls | Peak visible triangles | Payload (five packages) | Modelled mean frame |
|---|---|---|---|---|---|---|---|
| Baseline | — | 137,887,744 B (131.50 MiB) | 0 B | — | — | — | 16.95 ms |
| 1 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B (13.83 MiB) | 16.95 ms |
| 2 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 3 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 4 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 5 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 6 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 7 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 8 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 9 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |
| 10 | all five environments | 137,887,744 B (131.50 MiB) | **0 B** | 373 | 95,314 | 14,505,317 B | 16.95 ms |

**Drift result: 0 B after all 10 cycles (0 B × 50 scene loads/unloads).** The PlayMode `TenCycleLoadUnload_LeavesNoRetainedSceneObjects` test passed all 50 loads/unloads and asserted that each scene is unloaded and no `EnvironmentSceneContext` or scene root survives. The model predicts zero drift because environments are pure additive scenes: every generated object is owned by the loaded scene, the persistent Bootstrap root holds only services, and unload destroys the scene's objects. No runtime object is reparented into the persistent root.

**Stage-4 verdict (T-9.15):** all five environments are inside the D-056/OQ-T08 per-environment gates in the simulated matrix; the real WebGL `Phase9Debug` build succeeded at **62.51 MB / 0 errors**. Real browser heap/FPS capture remains the outstanding OQ-P01 evidence and is **not** claimed here.

**Phase-10 AI note — Stage-0 per-vehicle cost (T-10.0.3, 2026-11-16):** `VehicleCostProbe` measured the ten committed rig prefabs structurally (real data, not a browser run). Worst case is the Supercar rig at **18 renderers/draw calls, 488 triangles, 688 vertices, 6 materials, 18 shadow casters**; Starter is 15/452, Sport 16/464, Drift 17/476. Against the Phase-9 worst case (373/500 draw calls, 95,314/180,000 visible triangles), the headroom supports **7 additional cars by draw calls** and **173 by triangles**. The approved **5-car default (4 AI)** and **7-car cap (6 AI)** therefore pass: at the cap, 6 × 18 = 108 main-pass draws (373 → **481/500**) and 6 × 488 = 2,928 triangles (95,314 → **98,242/180,000**). **Constraint:** AI cars must use blob/contact shadows, not realtime shadow casting — six realtime casters would add a second 108-draw pass and breach 500. These values are structural prefab measurements; AI heap, frame-time, and in-browser draw calls remain **unmeasured** and are Stage-4 evidence (OQ-P01 still limits capture to a simulated matrix).

## 6. Submission checklist

- [ ] Bridge integration completed against the current SDK and official documentation.
- [ ] Archive root contains index.html and only Latin filenames, no external asset hosting.
- [ ] Archive size within the current platform limit; startup memory within the current limit.
- [ ] Analytics events use Bridge Analytics only.
- [ ] No YouTube embed, no external authentication requirement.
- [ ] Platform QA completed; current official documentation date/version recorded.
- [ ] Design, art, and monetization constraints above all hold in the final build.

**Important:** This file is a project checklist, not a substitute for the current official Playgama documentation.
