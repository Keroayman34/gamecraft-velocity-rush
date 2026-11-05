# Project Charter — Velocity Rush

**Version:** 0.2  
**Status:** Initial direction approved as working direction; details remain subject to validation.

## 1. Project identity

| Attribute | Value |
|---|---|
| Studio | GameCraft Studio |
| Working title | Velocity Rush |
| Working subtitle | Precision Trials |
| Working concept | Velocity Rush: Precision Trials |
| Tagline | Master the road. Conquer every challenge. |
| Genre | 3D skill-based arcade driving game |
| Engine / language | Unity / C# |
| Initial platform | Playgama Web |
| Initial game mode | Single-player |
| Distribution target | Playgama |
| Languages | Arabic (RTL) and English (LTR) |
| Title status | Provisional — not trademark-validated |

## 2. Project purpose
Build a polished, web-delivered 3D driving game where players progress through a global championship by mastering speed, braking, cornering, drifting, jumps, and precision driving — proving that skill-based arcade driving on Playgama Web can deliver replayable depth with a controlled, documentation-driven production process.

## 3. Product vision
Create a driving game where the player's growing mastery — not vehicle statistics — is the reason results improve. Every event should be replayable, every failure should be understandable, and progression should reward consistency and technique.

## 4. Core player fantasy
"I am beating better cars and harder events because I've become a better driver." The fantasy is visible self-improvement: earlier impossible events become records and stars through practice, not purchases.

## 5. Target audience and market direction
- **Primary:** Web players on Playgama seeking quick mid-length driving sessions with meaningful progression (children/teens to adults who enjoy arcade racing).
- **Secondary:** Bilingual Arabic/English players who want a polished driving game with proper RTL/localized UI.
- **Market direction:** Playgama Web as initial distribution; the game targets a global identity rather than one regional audience. Mobile browser support is expected as part of Playgama delivery but is not yet playtest-validated (see `OPEN_QUESTIONS.md`).

## 6. Core design principles
1. **Skill over speed:** driving technique matters more than top speed or upgrades.
2. **Easy to learn, hard to master:** accessible controls with room for mastery.
3. **Championship progression:** events form a structured progression journey.
4. **Global racing identity:** varied environments without dependence on one region.
5. **Continuous improvement:** replay, personal bests, vehicle progression, and mastery.
6. **Trust:** monetization and difficulty must not pressure or manipulate the player.

## 7. Initial product decisions
| Area | Direction |
|---|---|
| Genre | 3D skill-based arcade driving |
| Game mode | Championship |
| Driving model | Simcade |
| World identity | Global |
| Orientation | Landscape |
| Visual direction | Cinematic Motorsport |
| Visual quality | High-quality realistic-inspired 3D presentation (D-006, owner-upgraded from "Balanced") |
| Players | Single-player initially |
| Engine | Unity / C# |
| Distribution target | Playgama Web |
| Languages | Arabic and English |
| Vehicle roster target | 10 playable vehicles in Commercial V1.0 |
| Product scope model | Development Prototype → Vertical Slice → Commercial V1.0 (D-030) |

Initial gameplay pillar coverage (event families originally listed in this charter):
- Circuit racing and time trials.
- Precision driving and checkpoint challenges.
- Drift challenges.
- Jump and landing challenges.
- Obstacle avoidance and mixed-skill events.

The full Commercial V1.0 event mix and exact count must be designed and approved before mass content production (see `GDD.md` §17, OQ-16/OQ-18).

## 8. Vehicle progression
Initial vehicle classes:
- Starter: 2
- Sport: 3
- Drift: 2
- Supercar: 3

Total target: **10 playable vehicles in Commercial V1.0** (D-010). The former 3-vehicle MVP target was retired by the owner-confirmed Commercial V1.0 scope transformation (D-015 retired); temporary development prototypes may use fewer vehicles without reducing the product scope (D-030).

Upgrade categories:
- Engine
- Tires
- Brakes
- Suspension
- Transmission
- Aerodynamics

The roster, names, stats, unlock rules, upgrade costs, and balance are not final.

## 9. Monetization direction
Initial hypothesis: optional rewarded ads and carefully limited interstitial ads through Playgama Bridge. No ads during active driving. Reward only after the platform confirms successful completion of a rewarded video. Monetization must not undermine progression or player trust. Exact ad frequency, placement, and reward economy remain open.

## 10. High-level scope for Commercial V1.0 (approved-initial direction, D-028; detail open)
- **10 playable vehicles** across the four classes above.
- **At least three distinct championships** forming a meaningful progression structure (names/structure open — OQ-15/16).
- **All five environment themes**: Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena (detail open — OQ-20/21).
- A coherent, approved event structure (exact event count open — OQ-18).
- Complete gameplay and progression systems: selection, garage, upgrades, scoring/results, rewards/unlocks, save/progression, settings.
- High-quality realistic-inspired 3D presentation and professional UI/UX (D-006).
- Arabic and English support.
- Playgama Bridge integration and Playgama Web deployment preparation.
- QA and optimization to platform limits.

## 11. Explicit non-goals for the product
- Multiplayer or online PvP.
- Open world.
- Large cinematic story campaign.
- Real-money purchases.
- Complex online economy or marketplace.
- Advanced AI traffic systems.

(Formerly "five fully built environments" was listed as a non-goal for the tiny MVP framing; the owner-confirmed Commercial V1.0 scope now includes all five themes — recorded in `DECISIONS.md` change history. The other exclusions remain product-level non-goals.)

## 12. Success criteria
1. **First critical gate (blocking):** the vehicle driving experience feels enjoyable and understandable — validated through structured playtesting before further production investment.
2. **Vertical slice (quality gate):** the intended commercial quality bar and WebGL performance headroom are proven before mass content production (D-030).
3. Subsequent criteria: replayability (players voluntarily replay events), progression comprehension without tutorials, stable Playgama delivery within platform limits, bilingual UI correctness, full Commercial V1.0 content complete per the approved plan.
4. No revenue projections or untested retention assumptions are used as proof of product-market fit.

## 13. Project assumptions
- Playgama Web is a viable initial distribution channel for a Unity WebGL title.
- A Simcade driving model can be tuned to feel enjoyable on keyboard and touch input.
- The Bridge SDK covers ads, analytics, and save needs for the commercial product.
- **High-quality realistic-inspired 3D presentation can be delivered within WebGL limits — for ten vehicles, five environments, and multiple championships — through disciplined optimization. This is the central technical/product assumption and a standing risk (see §14 and the performance risk register).**
- These assumptions are risk items, not verified facts (see §14).

## 14. Main risks
| Risk | Impact | Mitigation |
|---|---|---|
| Driving feel not enjoyable after prototyping | Critical — core concept fails | Validate first; defer menus/progression/monetization investment until the driving-feel gate passes |
| Unity WebGL performance/memory cannot sustain the high-quality presentation (D-006) with 10 vehicles / 5 environments | Critical | Vertical slice proves quality-vs-performance headroom **before** mass content production; profiling gates on every expensive feature; optimization strategies in `ART_DIRECTION.md` §11 and `PLAYGAMA_CONSTRAINTS.md` register |
| Production workload (10 vehicles, 5 environments, ≥3 championships) exceeds realistic capacity | High | Content production gated on OQ-15–OQ-21 planning and playtests; quality acceptance criteria defined before scaling (OQ-A08) |
| Web performance with content at commercial scale | High | Profile early and continuously; budget headroom decisions at the vertical slice gate; performance risk register (`PLAYGAMA_CONSTRAINTS.md`) |
| Mobile browser quality below expectations | High | Test on representative devices early; quality depth open (OQ-P01) |
| Playgama platform requirements change | Medium | `PLAYGAMA_CONSTRAINTS.md` re-verification ritual before integration and submission |
| Language/RTL implementation quality | Medium | Bilingual acceptance criteria enforced per screen in `design/ui-ux/` |
| Title/asset trademark conflicts | Medium | Fictional branding only; validate title before broader release |

## 15. Current project status
- Phase 1 (Documentation & Commercial Product Scope Approval) **complete — owner approved the Commercial V1.0 documentation baseline on 2026-11-05 (OQ-08 resolved)**.
- Phase 2 (Environment setup) is authorized to begin; Unity environment setup has **not** been performed yet.
- Driving feel and the vertical slice have **not** been validated yet.
- No game content has been produced; no implementation exists.

## 16. Scope control
Any material change to genre, core loop, platform, monetization, vehicle count, or visual direction must be recorded in `docs/DECISIONS.md` and approved before implementation.
