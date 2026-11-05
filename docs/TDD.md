# Technical Design Document — Velocity Rush

**Version:** 1.0-planning (Commercial V1.0)  
**Status:** Technical planning for the Commercial V1.0 quality bar (DECISIONS D-006/D-028). **Architecture not yet approved.** Every architecture item is `Proposed` or `Open` unless explicitly approved in `DECISIONS.md`. Do not infer final architecture from this document.

## 1. Purpose
Define how Commercial V1.0 — a high-quality, realistic-inspired 3D driving game — will be implemented in Unity/C# for Playgama Web, and the **planning process** by which unknown architecture will be decided. High visual quality must be planned together with browser performance: never assume an arbitrary desktop visual feature is deliverable in WebGL; document tradeoffs and enforce profiling gates.

## 2. Approved technical direction

| Area | Decision | Source |
|---|---|---|
| Engine | Unity (final stable LTS version open — OQ-T01) with WebGL Build Support verified at setup | DECISIONS D-008, `OPEN_QUESTIONS.md` |
| Language | C# | DECISIONS D-008 |
| Product target | Commercial V1.0 (prototype/vertical-slice are development stages, D-030) | DECISIONS D-028/D-030 |
| Visual quality bar | High-quality realistic-inspired 3D presentation, planned with WebGL performance limits | DECISIONS D-006 |
| Target distribution | Playgama Web (Unity WebGL) | DECISIONS D-009 |
| Runtime platform constraints | WebGL/browser limits per `PLAYGAMA_CONSTRAINTS.md` | Platform requirements |
| Localization | Arabic (RTL) and English (LTR) | DECISIONS D-013 |
| SDK dependency | Playgama Bridge SDK (Unity Package Manager via official Git repo) — integrated only at the planned phase, not during documentation | `PLAYGAMA_CONSTRAINTS.md` #1 |

## 3. Unity Web platform & WebGL Build Support (constraints, not solutions)
- Unity WebGL output must satisfy Playgama packaging rules: `index.html` at archive root, Latin filenames, no external asset hosting, archive-size and startup-memory limits (`PLAYGAMA_CONSTRAINTS.md` §§1, 6).
- WebGL Build Support module compatibility verified **at environment setup** (OQ-T01) before architecture planning begins.
- Avoid WebGL startup warnings; no YouTube embedding; no external analytics; shared browser keys (e.g., Escape) never critical to flow.
- Loading time, mobile fullscreen/rotation, and touch quality are first-class requirements of a commercial web product.

## 4. Playgama Bridge integration plan (Proposed, phase-gated — Roadmap Phase 13)
- **Boundaries to validate against the actual SDK:**
  - Ads: rewarded/interstitial invocation, pause/resume hooks, reward grants gated on confirmed rewarded completion (D-021/D-027).
  - Analytics: Bridge Analytics only (D-022); taxonomy open (OQ-P04).
  - Save: per the save API the current SDK provides (OQ-T05/OQ-09).
- Verify current SDK capabilities from official documentation before integration (OQ-P02). SDK is not installed during documentation phases.

## 5. High-level system boundaries (Proposed — subject to prototype validation)
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
Organization is defined **inside the Unity project** after creation. Nothing here is approved: no namespace layout, no folder-level C# structure, no pattern (MVC/MVVM/ECS/service locator) has been chosen. Deciding inputs: input strategy (OQ-T04), vehicle-physics strategy (OQ-T03), UI/localization approach, and content-scaling needs for 10 vehicles / 5 environments.

## 7. Scene management and content scaling (Open)
- Expected scenes include at minimum: driving/test scene(s), UI scene(s), platform bootstrap; a growing commercial content set raises additive-loading and memory questions (OQ-T06).
- Fast-retry UX (`GDD.md` §6) may imply a low-friction scene reload path — validate early.
- Scene strategy must scale to five environments without five-fold loading costs — likely via shared runtime systems and environment content packages (proposed; unvalidated).

## 8. Input abstraction (Open — OQ-T04)
- Desktop: keyboard + mouse; mobile: touch (Playgama constraints 13/14/16/22).
- Input must be abstracted from the vehicle controller so touch adaptation does not break keyboard tuning; device-detection and layout rules per `GDD.md` §27.

## 9. Vehicle system and physics architecture considerations (Open — OQ-T02/OQ-T03/OQ-14)
Expected responsibilities, not an implementation plan:
- Steering, throttle/brake response, drift state, jump/landing state, contact/obstacle state; telemetry output for scoring.
- Per-vehicle/per-class handling differentiation must be data-driven enough for 10 vehicles to tune without code forks (proposed rationale for a data-driven vehicle configuration approach; strategy itself open).
- Strategy choice (native vs package-based simulation) deferred to technical discovery after the Unity project exists.

## 10. Camera system responsibilities (Open — OQ-01/OQ-A06)
- Readable driving first, cinematic presentation second; perspective default, FOV, smoothing/shake pending driving-feel validation.

## 11. UI architecture (Open, constraint-driven)
- Full commercial screen set (`design/ui-ux/README.md`): championship navigation for ≥3 championships, event selection, garage, upgrades, pre-event, HUD, pause, results, settings, and reward/unlock presentation.
- Must support: safe-state orchestration for ads, pause/resume, popup closeability, responsive landscape layouts, RTL/LTR mirroring, persistent language preference. Restoration strategy after large UI flows must not leak state.

## 12. Localization and RTL/LTR (Open, language = Arabic/English)
- All UI strings localized from a catalog; never hard-coded.
- RTL mirroring driven by language state; Arabic uses neutral copy.
- Fonts verified for commercial license + Arabic/English coverage + size (OQ-A01; `design/brand/README.md`).
- Concrete Unity localization strategy: open until the Unity project exists.

## 13. Save and progression architecture (Open — OQ-09/OQ-T05)
- Expected shape: language/settings, best results, stars, unlocks, upgrade levels, championship progress — sized for the commercial save set, not a prototype subset.
- Direct-file vs Bridge-backed persistence, versioning, migration defaults: decided against the actual Bridge API during Phase 13 planning.

## 14. Ads and analytics integration boundaries (Open, direction approved D-021/D-022)
- Gameplay code must not depend on the SDK directly; platform integration is isolated in one layer (§5).
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

## 18. Visual effects strategy (Proposed, validation-gated)
- Effects (tire smoke, sparks, speed lines, dust, atmosphere) must communicate events without obscuring the road; overdraw/particle cost constrained per WebGL test data.
- Five distinct environments will not share one effect suite blindly; per-environment atmosphere must pass individual cost checks (OQ-20).

## 19. Audio system (Open — OQ-A11)
- Separate audio boundary owning vehicle sound differentiation (engine sound per class/vehicle), surface feedback, UI feedback, and music.
- Must pause/resume correctly with ads and browser tab behavior; license-clean sources only; acceptable memory footprint on web targets.

## 20. Performance constraints, profiling gates, loading and memory management (binding, values open — OQ-T08)
- Platform win conditions: archive ≤ 300 MB (re-verify), startup memory < 2 GB (re-verify), acceptable loading time, smooth driving on representative devices, no startup warnings — see `PLAYGAMA_CONSTRAINTS.md` and its performance risk register.
- **Profiling gates rule:** no performance-sensitive feature (shader, lighting mode, VFX, uncompressed texture, audio stream) becomes standard before a representative WebGL profiling measurement documents its cost (recorded in `PLAYGAMA_CONSTRAINTS.md` performance evidence log).
- Loading/memory management proposals (content streaming vs full-load, addressable strategies, texture compression) are validated only after the Unity project exists; no specific API final choices here.
- Do not present untested values as achieved performance or as guarantees.

## 21. Testing strategy (Open, expectations stated)
- Documentation phase: consistency/link review.
- Prototype phase: editor playmode validation + profiling evidence.
- Later phases: automated checks for scoring/progression rules; browser/device stability matrix per `PLAYGAMA_CONSTRAINTS.md`; visual quality gate reviews (OQ-A08); regression protection as content volume grows (OQ-T07).
- CI/build automation: proposed after Unity setup.

## 22. Browser compatibility (Open, requirement-driven)
- Test stability across the supported browser/device matrix (Playgama constraint 23), covering desktop keyboard/mouse and mobile fullscreen/touch/orientation; mobile quality bar OQ-P01.
- Startup warnings absence, memory ceiling, loading time, and orientation handling are release-blocking checks, re-verified per platform documentation.

## 23. Build and deployment workflow (Proposed, pending Unity setup)
Loop: local Unity WebGL build → archive structure per Playgama rules → Bridge-level testing → submission rehearsal on Playgama. Automation level decided after the Unity project exists. Never built during documentation phase.

## 24. Security and platform compliance
- Never commit secrets, credentials, tokens, or private keys (`.gitignore` guards `.env*`).
- No external analytics services; no external auth gates; no URL-based operation restrictions; no YouTube embeds; external links only through the Bridge capability gate (`PLAYGAMA_CONSTRAINTS.md` §3).
- Player privacy: invent no data collection beyond platform-approved Bridge Analytics (final scope verified at integration).

## 25. Architecture decision rule & deferred decisions
For each major technical decision, document problem, options, trade-offs, chosen approach, consequences — recorded as ADR entries when the docs/architecture structure is introduced.

**Explicitly deferred until Unity project creation + technical prototype/vertical slice evidence:**
- Unity version & render pipeline (OQ-T01, OQ-T02)
- Vehicle physics strategy (OQ-T03)
- Input abstraction details (OQ-T04)
- Scene/content organization (OQ-T06)
- Save strategy (OQ-T05/OQ-09)
- Build/CI automation (OQ-T07)
- Performance budgets (OQ-T08) and quality acceptance criteria (OQ-A08/OQ-A09)

## 26. Do not implement yet
No final class diagrams, namespaces, service locators, dependency-injection frameworks, or manager hierarchy has been approved. Avoid creating them before technical discovery and the prototype validate them. Do not reduce product scope to prototype scope; the Commercial V1.0 target (D-028) always governs.
