# Game Design Document — Velocity Rush

**Version:** 1.0-planning (Commercial V1.0)  
**Status:** Documentation-complete product plan for **Commercial V1.0** (DECISIONS D-028). Concept direction approved-initial; all balance values, thresholds, championship/event naming, and detailed content distribution are **provisional or open** and must be validated through playtesting and owner decisions. Companion files: `PROJECT_CHARTER.md`, `DECISIONS.md`, `OPEN_QUESTIONS.md`, `TRACEABILITY.md`.  
**Scope model:** Development Prototype and Vertical Slice (D-030) are production *stages*, never the product definition.

---

## 1. Product overview — Approved-initial
Velocity Rush: Precision Trials is a commercially positioned, 3D skill-based arcade driving game for Playgama Web, delivered in Unity/C#. It is planned as **Commercial V1.0**: a complete, high-quality player experience with a meaningful 10-vehicle roster, at least three distinct championships, five visually distinct environments, a coherent progression structure, bilingual localization (Arabic/English), and Playgama Bridge integration. It is not a small prototype or minimal-content demo (D-015/D-017/D-019 retired).

## 2. High concept — Approved
Wrapped in global championships, players face speed-, precision-, and style-driven driving challenges where **mastery, not horsepower, decides the result.** Working identity: title "Velocity Rush", subtitle "Precision Trials", tagline "Master the road. Conquer every challenge." (title not trademark-validated — OQ-P05).

## 3. Product pillars — Approved
1. **Skill over speed** — driving technique beats raw top speed and upgrades.
2. **Easy to learn, hard to master** — accessible Simcade controls with depth via mastery.
3. **Commercial quality** — presentation, UI, and performance that read as a professionally produced product.
4. **Championship progression** — at least three distinct championships forming a meaningful journey (D-029).
5. **Global racing identity** — five distinct environments without dependence on one region.
6. **Continuous improvement** — replay, personal bests, upgrades, progression.
7. **Player trust** — no ads during driving; monetization never creates pay-to-win pressure.

## 4. Target audience — Approved-initial
- **Primary:** web players on Playgama who want high-quality driving sessions with visible progression.
- **Secondary:** bilingual Arabic/English players; mobile-browser players (QA depth open — OQ-P01).
- Session-length expectations provisional pending playtesting (OQ-10).

## 5. Core player fantasy — Approved
"I beat better cars and harder events because I became a better driver." Observable self-improvement through practice and consistency — not purchases.

## 6. Gameplay loop — Approved-initial; detail Provisional
```text
Select Championship → Select Event → Select / Prepare Vehicle → Drive / Perform → Receive Results → Earn Rewards → Upgrade / Unlock → Progress → Replay
```
Failure must read as "so close", with minimal-friction retry. Exact retry flow: **open**; completed-event replay must remain attractive via bests/star chasing.

## 7. Driving model — Approved-initial (Simcade)
- Arcade accessibility + believable vehicle behavior; **not** a full racing simulator (changing it requires explicit owner approval and a new decision entry).
- Targets: responsive controls; believable acceleration/braking; convincing cornering; controlled drifting; distinct per-class/vehicle handling; good road feedback; consistent camera behavior; skill-based mastery.
- Exact physics model, grip behavior, assists, drift mechanics: **open** (OQ-02). Camera: **open** (OQ-01/OQ-A06).

## 8. Vehicle behavior — Approved direction; detail Open
- Vehicle differences are felt through tuned handling character, not arbitrary stat inflation (differentiation design: **open** — OQ-06/OQ-14).
- Upgrades improve characteristics within limits: reduced effort, never a requirement to succeed (skill-preservation rule).
- Damage/consequences on handling beyond approved direction: **open**.

## 9. Complete 10-vehicle roster structure — Approved count (D-010); identities Open
All ten vehicles are planned content for Commercial V1.0. Each vehicle must support: distinct driving characteristics, selection, unlocking, statistics, garage presentation, upgrade compatibility, and meaningful class differentiation. Final vehicle identities are fictional and unassigned (**open** — OQ-14); no real-world brands or licensed models without explicit licensing.

## 10. Four vehicle classes — Provisional planning distribution (D-014)
| Class | Vehicle count | Intended handling role |
|---|---:|---|
| Starter | 2 | Learning-focused, higher forgiveness |
| Sport | 3 | Balanced, general-use |
| Drift | 2 | Drift-oriented handling bias |
| Supercar | 3 | Highest capability, hardest to master |
| **Total** | **10** | |

## 11. Vehicle progression — Provisional; detail Open
Vehicles are obtained through championship/reward progression; unlock rules and pacing: **open** (OQ-17). Vehicle access paces content without gating skill expression.

## 12. Six upgrade categories — Approved (D-011)
Engine, Tires, Brakes, Suspension, Transmission, Aerodynamics.
- Categories map to the skills they support; exact numeric mapping and formulas: **open** (OQ-07).
- **Skill-preservation rule (binding):** upgrades never make skill irrelevant or create pressure to purchase.
- Upgrade UI is a planned screen (`design/ui-ux/README.md`).

## 13. Championship system — Approved count (D-029); structure Open
Commercial V1.0 includes **at least three distinct championships**, each with its own identity and a coherent event collection. The system must support: championship selection, event progression, unlock requirements, difficulty progression, rewards, performance evaluation, completion status, and replayability. Names, identities, structure, and counts per championship: **open** (OQ-15/OQ-16/OQ-18).

## 14. Minimum three distinct championships — Approved-initial (D-029)
The candidate progression names `Rookie / Street / Pro / Elite / World` (D-018) remain useful as progression vocabulary, but the owner approved a **minimum of three distinct championships**, not those five names and not five separate championships. Championship identity design — theme, environment pairing, tone, difficulty target: **open** (OQ-15).

## 15. Championship progression and unlock design — Open
Progression across championships (unlock conditions, ordering, backtracking, difficulty ramp) is unresolved; it must be designed with OQ-05/OQ-17 and validated in playtests. No invented gating values.

## 16. Event types — Approved planning basis (D-031, Provisional)
Long-term event list: Circuit Race, Time Trial, Drift Challenge, Precision Challenge, Jump Challenge, Brake Challenge, Obstacle Challenge, Mixed Challenge.

## 17. Event distribution planning — Open (OQ-16/OQ-18/OQ-19)
Not all eight types must appear in every championship. Event count in Commercial V1.0: **open** (OQ-18). Event distribution per championship and per environment: **open** (OQ-16/OQ-19). The formal event-mix plan is produced during championship design and validated with the owner before mass content production.

## 18. Five environment themes — Approved content target (D-028)
| Environment | Required for Commercial V1.0 |
|---|---|
| Desert Circuit | Yes |
| Neon City | Yes |
| Mountain Pass | Yes |
| Coastal Circuit | Yes |
| World Arena | Yes |

Each environment requires distinct visual identity, lighting, materials, atmosphere, landmarks, and a distinct driving experience. Reusability is permitted as a production technique, but must not reduce the five to one environment with cosmetic variation. Detailed designs: **open** (OQ-20).

## 19. Environment identity and gameplay role — Open per environment
Each environment package defines: art direction fit, track layouts and configurations, road/grip assumptions, hazards and readability, environment-specific landmarks and atmosphere, performance-aware geometry/materials, and gameplay role within events (OQ-20/OQ-21). Rules: `design/environments/README.md`.

## 20. Scoring and evaluation — Provisional intent; formulas Open
Per-challenge-type scoring (no universal formula). Stars (1–3 per event) as the provisional communication model. Thresholds and per-type rules: **open** (OQ-03/OQ-04). Personal bests and replay rewards reward improvement; exact rules **open**.

## 21. Rewards and economy — Provisional; detail Open
Basic flow: events award progression rewards; upgrades/upgrades consume earned value; no real-money purchases. Reward economy for the commercial release (currency, awarded items, upgrade costs): **open** (OQ-17/related). No numbers invented.

## 22. Save and progression — Open (OQ-09)
Expected persisted state: language/settings, best results, stars, unlocked vehicles, upgrade levels, championship/event progress. Persistence strategy per actual Bridge capabilities (OQ-T05). First-run defaults and save versioning: **open**.

## 23. Garage and vehicle selection — Planned; specs Open
A garage experience is part of the Commercial V1.0 systems set (roster browsing, stats, class filters, upgrade access, unlock state). Full screen specification: **open** (OQ-A03). Reference-first development required.

## 24. UI/UX screens — Planned; none completed
Planned screen inventory — the same 11 screens recorded in `design/ui-ux/README.md`, which holds the authoritative per-screen package table (purposes summarized identically here; no additional screen requirements are defined in this GDD section):

1. **Main Menu** — entry navigation, language settings hook.
2. **Championship selection** — multi-championship selection for ≥3 championships with identity + progress state.
3. **Event Selection** — event browsing/progression within a championship.
4. **Garage** — 10-vehicle roster browsing, stats, class differentiation, unlock state.
5. **Upgrades** — vehicle upgrade consumption and visualization (6 categories).
6. **Pre-Event** — event briefing, vehicle/assist/controls confirmation.
7. **In-Game HUD** — driving state, timers, score, feedback per challenge type.
8. **Pause** — pausable state, retry/settings/quit, safe state for ads.
9. **Results** — score, stars, rewards, progression deltas, retry/next actions.
10. **Settings** — language, audio, controls preferences.
11. **Rewards / Unlock presentation** — reward flow and unlock moments across events/championships.

All screens require approved reference + written specification before implementation (`design/ui-ux/README.md`, `ART_DIRECTION.md` §10). Landscape-only. Screen status: Planned; none completed.

## 25. Localization — Approved-initial (Arabic/English)
- Arabic (RTL) and English (LTR); Arabic uses neutral Arabic copy.
- All UI strings localized; direction-sensitive layouts driven by language state, never hard-coded positions.
- Language preference persists (OQ-09). Fonts verified for license + Arabic/English coverage (`design/brand/README.md`).

## 26. Audio direction — Open (OQ-A11)
Direction targets: engine sound that reflects acceleration and vehicle identity/class; road-surface feedback; UI feedback reinforcing state; music that supports the premium presentation without masking gameplay cues. Full audio spec: **open**. Vehicle sound differentiation across the 10-vehicle roster is an explicit Commercial V1.0 consideration.

## 27. Accessibility and usability — Approved baseline
- Color is never the only status indicator; contrast validation pending (OQ-A02).
- Responsive landscape layouts: no clipping/overflow/unwanted scrolling (`PLAYGAMA_CONSTRAINTS.md`).
- Escape never required for critical flow; popups closeable; no long-press selection issues.
- Beyond-baseline accessibility features: **open** (settings screen design).

## 28. Monetization direction — Approved-initial direction (D-021)
Optional rewarded ads + carefully limited interstitials via Playgama Bridge; **never during active driving** (D-027); rewarded rewards only after confirmed completion. Exact frequency, placement, amounts: **open** (OQ-P03). No external registration required to launch; Bridge Analytics only (D-022).

## 29. Commercial V1.0 scope — Approved-initial (D-028)
- 10 playable vehicles (D-010) across 4 classes (D-014).
- At least 3 distinct championships (D-029).
- All 5 environment themes (D-028/§18).
- Coherent event structure with a meaningful mix (OQ-16/OQ-18).
- Complete gameplay/progression systems: selection, garage, upgrades, scoring/results, rewards/unlocks, save/progression, settings.
- Arabic and English localization.
- Playgama Bridge integration, ad rules, analytics.
- Commercial-quality presentation (D-006) and professional UI/UX.
- QA and optimization to platform limits.
Exact event counts/names/track counts: **open** (do not invent).

## 30. Development prototype scope — Development stage, not product (D-030)
Temporary stage to validate: Unity environment, WebGL compatibility, vehicle controller, driving feel, camera, basic track interactions, performance, and core technical risks. A prototype may temporarily use one vehicle and one test environment; this allowance **never reduces product requirements** and is never presented as the product scope. Prior retired MVP framing (D-015/D-017/D-019) is now historical, applicable only where a prototype reuses its lesson.

## 31. Vertical slice definition — Development stage (D-030)
A representative, production-quality section demonstrating the intended commercial quality of: vehicle, environment, driving, lighting, UI, audio, gameplay, and performance. The vertical slice exists to **validate the quality bar before large-scale asset production** (`production/ROADMAP.md` Phase 7). Its exact composition is a planning item to be defined with the owner; no unvalidated composition is presented here as approved.

## 32. Quality acceptance criteria — Open (OQ-A08/OQ-A09); binding principle
The commercial release must pass explicit visual, performance, and gameplay-acceptance criteria. Criteria definitions are open and profiling-gated. Binding principles: high quality is not unlimited rendering complexity; WebGL limits; no silent downgrades of quality without documented profiling and owner approval.

## 33. Playtesting — Required process
1. **Driving-feel validation (first critical gate, D-023):** prototype playtests of handling, camera feel, feedback — before further production.
2. **Vertical-slice validation:** quality-bar and performance evidence against the intended commercial presentation.
3. Challenge-type playtests: scoring/threshold clarity.
4. Progression/UX playtests: loop comprehension, retry friction, upgrades, championship flow.
5. Localization playtests: Arabic RTL correctness/readability.
6. Platform playtests: browsers/devices, mobile-touch behavior.
Each playtest documented; decisions change only via `DECISIONS.md`.

## 34. Risks and assumptions
`PROJECT_CHARTER.md` §13–§14 and the performance risk register (`PLAYGAMA_CONSTRAINTS.md`) are authoritative. GDD-specific risks:
- The stronger quality target (D-006) may not be reachable within WebGL limits without painful optimization — treat via profiling gates early (OQ-T08).
- Content workload across 10 vehicles / 5 environments / ≥3 championships is substantial and must be staged via the vertical slice first.
- Championship/event design may require iteration after playtesting begins; resist locking event mix or unlock values too early.

## 35. Out-of-scope features — Approved (D-020)
Multiplayer, PvP, open world, large cinematic story, advanced AI traffic, complex marketplace, real-money purchases. Do not introduce any of these without a new owner-approved decision entry.

---

## Design rule
Do not treat illustrative examples or placeholder values as final rules. Every system must have explicit player-facing rules, edge cases, and acceptance criteria before implementation. Provisional/Open items map to `OPEN_QUESTIONS.md` IDs.
