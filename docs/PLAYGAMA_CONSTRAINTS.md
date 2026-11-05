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

### Performance evidence log (empty — to be filled by profiling work)
| Date | Feature/measure | Device/browser | Result | Decision recorded |
|---|---|---|---|---|

## 6. Submission checklist

- [ ] Bridge integration completed against the current SDK and official documentation.
- [ ] Archive root contains index.html and only Latin filenames, no external asset hosting.
- [ ] Archive size within the current platform limit; startup memory within the current limit.
- [ ] Analytics events use Bridge Analytics only.
- [ ] No YouTube embed, no external authentication requirement.
- [ ] Platform QA completed; current official documentation date/version recorded.
- [ ] Design, art, and monetization constraints above all hold in the final build.

**Important:** This file is a project checklist, not a substitute for the current official Playgama documentation.
