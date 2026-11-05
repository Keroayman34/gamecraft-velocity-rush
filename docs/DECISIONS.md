# Project Decision Register

The central register of decisions that materially affect the product, technology, art direction, or production workflow. This file is the source of truth for what is approved versus still open.

Statuses:
- `APPROVED-INITIAL`: accepted as a working direction; validation may change it.
- `APPROVED`: formally approved for implementation.
- `PROVISIONAL`: agreed as a proposal to plan against, explicitly not final; requires formal validation.
- `PROPOSED`: under discussion, not yet agreed.
- `OPEN`: not decided.
- `REJECTED`: not to be used.
- `RETIRED`: superseded by a later decision but preserved for history (the decision no longer applies).

Rule: **Do not mark provisional items as approved.** Do not silently overwrite a previous decision — add a dated change entry below the table.

## Decision table

| ID | Topic | Decision | Status | Reason / context | Impact on future work |
|---|---|---|---|---|---|
| D-001 | Game mode | Championship progression wrapping all events | APPROVED-INITIAL | Gives replayability and structured long-term goals | Championship/event systems are Commercial V1.0 core; championship count pinned by D-029; tier detail open (OQ-05) |
| D-002 | Driving model | Simcade | APPROVED-INITIAL | Balances arcade accessibility with believable vehicle behavior for web | Defines the core vehicle-controller work (M3 prototype); no full-sim realism |
| D-003 | World identity | Global | APPROVED-INITIAL | Enables varied environments and a global audience | Guides environment and art direction |
| D-004 | Orientation | Landscape | APPROVED-INITIAL | Standard driving-game framing on desktop and mobile | Responsive layout rules in `PLAYGAMA_CONSTRAINTS.md` |
| D-005 | Visual direction | Cinematic Motorsport (Direction A) | APPROVED-INITIAL | Premium, high-energy, recognizable motorsport style | Governs all art work via `design/ART_DIRECTION.md` |
| D-006 | Visual quality | High-quality realistic-inspired 3D presentation (owner-upgraded from "Balanced"; Cinematic Motorsport direction preserved) | APPROVED-INITIAL | The commercial release must look like a professionally produced product; quality is planned together with WebGL performance constraints, never silently downgraded | Raises the quality bar for vehicles, materials, lighting, environments, VFX, and UI; optimization strategies and budgets documented via profiling gates (see `TDD.md`, `PERFORMANCE_RISKS.md`) |
| D-007 | Visual reference workflow | Hybrid: visual references + written specifications | APPROVED-INITIAL | Images alone are ambiguous; text alone is uninstructive for AI agents | Every screen/asset needs reference + specification (see `design/references/README.md`) |
| D-008 | Engine / language | Unity with C# | APPROVED-INITIAL | Industry-standard, WebGL-capable, C# ecosystem; final Unity version still open (OQ-T01) | Unity environment setup is Roadmap Phase 2; do not install during documentation phase |
| D-009 | Initial distribution | Playgama Web | APPROVED-INITIAL | Target web platform; current requirements in `PLAYGAMA_CONSTRAINTS.md` | All platform-facing design must respect Bridge/integration constraints |
| D-010 | Vehicle count in Commercial V1.0 | 10 playable vehicles in the Commercial V1.0 release | APPROVED-INITIAL | Owner-confirmed commercial target (previously a provisional long-term ambition, v0.2); upgrade recorded in change history | Full 10-vehicle roster planned in `design/vehicles/README.md` and `GDD.md`; identities/stats open (OQ-06/OQ-14) |
| D-011 | Vehicle upgrade categories | Engine, Tires, Brakes, Suspension, Transmission, Aerodynamics | APPROVED-INITIAL | Covers the vehicle characteristics that matter for skill-based play | Upgrade system design in `GDD.md` §10; exact formulas open (OQ-07) |
| D-012 | Player mode | Single-player initially | APPROVED-INITIAL | Reduces early technical risk; multiplayer excluded | No networking in the Commercial V1.0 architecture |
| D-013 | Languages | Arabic and English | APPROVED-INITIAL | Serves a bilingual audience; mandatory RTL/LTR correctness | Localization architecture in `TDD.md`; fonts verified for Arabic glyphs |
| D-014 | Vehicle class distribution (Commercial V1.0 planning) | Starter 2, Sport 3, Drift 2, Supercar 3 — total 10 | PROVISIONAL | Planning distribution for the 10-vehicle commercial roster; counts per class not contractually frozen | `design/vehicles/README.md`; roster identities, stats, and per-class handling differentiation open (OQ-06/OQ-14) |
| D-015 | MVP vehicle count (3) | RETIRED — superseded by D-010/D-028 (Commercial V1.0 targets 10 vehicles; 3-vehicle MVP was the former product framing). Temporary development prototypes may use fewer vehicles; that is a development-stage allowance, not a product-scope statement | RETIRED | Owner transformed the product scope from MVP-oriented to Commercial V1.0 | Roster planning no longer constrained to 3 vehicles; see `RETIREMENTS` section below |
| D-016 | Former MVP challenge-type focus (3 types) | RETIRED — superseded by D-031: Commercial V1.0 plans against the full 8-type event list with a per-championship mix; the former 3-type MVP focus (Time Trial / Precision / Drift) remains recorded as former product framing and may be re-used inside development prototypes | RETIRED | Owner transformed the product scope to Commercial V1.0 | See `RETIREMENTS` section below |
| D-017 | Environment count (former one-kit scope) | RETIRED — superseded by D-028: Commercial V1.0 includes **all five** approved environment themes. One reusable kit remains allowed for the development prototype and as a shared technical foundation | RETIRED | Owner transformed the product scope; the one-kit MVP framing no longer defines the product | Five-theme planning in `design/environments/README.md` and `GDD.md`; per-environment kit choice still open (OQ-A04/OQ-20) |
| D-018 | Championship progression tier names | Rookie, Street, Pro, Elite, World — preserved as **candidate** progression names | PROVISIONAL | 5 candidate tier names remain useful as progression vocabulary; they are NOT automatically five distinct championships (see D-029) | Championship names/identities for the minimum three championships are open (OQ-15); roadmap mapping open (OQ-16) |
| D-019 | MVP scope (former product definition) | RETIRED — superseded by D-028 (Commercial V1.0). The 3-vehicle / 1-environment / 8–12-event / 3-challenge-type / 1-championship framing remains on record as the former product definition and as guidance for what a *development prototype* may re-use | RETIRED | Owner confirmed the commercial scope transformation; the limited MVP must no longer define the final product | Product planning now references `D-028 / GDD.md §29`; see `RETIREMENTS` section below |
| D-020 | Product exclusions (out of Commercial V1.0) | Multiplayer, PvP, open world, cinematic story, advanced AI traffic, complex marketplace, real-money purchases | APPROVED-INITIAL | Prevents scope creep; explicit exclusions | Any inclusion requires a new decision entry (recorded in `GDD.md` §35) |
| D-021 | Monetization model | Optional rewarded ads + carefully limited interstitials; no ads during active driving; reward only after confirmed completion | APPROVED-INITIAL | Approves the monetization **direction** only: exact ad frequency, placement, reward amounts, and configuration remain open (OQ-P03), subject to testing and current platform requirements | Preserves player trust and gameplay integrity; ad configuration open (OQ-P03); platform rules in `PLAYGAMA_CONSTRAINTS.md` |
| D-022 | Analytics | Playgama Bridge Analytics only | APPROVED-INITIAL | Platform requirement; avoids external analytics services | Event taxonomy open (OQ-P04) |
| D-023 | First milestone priority | Validate driving feel before building the rest of the game | APPROVED-INITIAL | Many roles: core experience must be proven first | Menus/progression/monetization are deliberately deferred to Roadmap Phase 8+ |
| D-024 | Camera perspective | — | OPEN | Not yet decided; multiple viable options | Blocks vehicle controller polish and HUD design (OQ-01); does not block documentation |
| D-025 | Title & branding | "Velocity Rush: Precision Trials" + tagline as working identity | PROVISIONAL | Working identity for all documentation | Not trademark-validated; validate before broader release |
| D-026 | Art direction governance | Generated reference images are conceptual guidance, not production assets | APPROVED-INITIAL | Prevents moodboard misuse | `design/references/art-direction-v0.1/README.md` rules |
| D-028 | Commercial product scope | **Commercial V1.0** is the intended initial product release: 10 playable vehicles (D-010), at least 3 distinct championships (D-029), all 5 environment themes (Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena), high-quality realistic-inspired 3D presentation (D-006), complete gameplay/progression/garage/upgrade systems, Arabic+English localization, Playgama Bridge integration, commercial-quality presentation, QA and optimization | APPROVED-INITIAL | Owner-confirmed product direction: the project is not a small prototype or demo; a meaningful commercial product is the release target | Governs the GDD, TDD, design folders, roadmap, and milestones. Exact event counts, championship names, track layouts, and detailed content distribution remain OPEN (OQ-15…OQ-21, OQ-A03) |
| D-029 | Championship count | At least THREE distinct championships in Commercial V1.0, forming a meaningful progression structure; each has its own identity and coherent event collection | APPROVED-INITIAL | Owner approved the minimum count; names, structure, event counts, and unlock formulas are NOT approved and remain open (OQ-15…) | Championship selection/progression/unlock/reward design in `GDD.md`; event distribution open (OQ-16) |
| D-030 | Development scope model | Three-level scope model: **Development Prototype** (temporary validation stage; may use 1 vehicle / 1 test environment; never presented as product scope) → **Vertical Slice** (production-quality representative section validating the intended commercial quality bar) → **Commercial V1.0** (complete initial commercial release per D-028) | APPROVED-INITIAL | Prevents prototype/early-stage size from being confused with product ambition | Roadmap and milestones structured around the three levels; agents must never reduce product scope to prototype scope (see `ai/MASTER_AGENT_PROMPT.md`) |
| D-031 | Event/challenge planning basis | The 8 challenge types (Circuit Race, Time Trial, Drift Challenge, Precision Challenge, Jump Challenge, Brake Challenge, Obstacle Challenge, Mixed Challenge) remain the long-term planning list for Commercial V1.0; a meaningful event mix per championship is planned, but not all 8 types must appear in every championship | PROVISIONAL | Owner preserved the full list while leaving event mix design open | Event distribution design open (OQ-16/OQ-18/OQ-19); scoring per type open (OQ-03) |

## Detail: rejected items

| ID | Topic | Rejected option | Date |
|---|---|---|---|
| D-027 | Ads during driving | Any interstitial/rewarded ad during active driving | 2026-11 (initial direction) |

## Retirements (superseded decisions — preserved)

The Commercial V1.0 scope transformation (owner-confirmed, 2026-11-03) retired the MVP-oriented product framing. The retired decisions remain registered above with their original IDs and are summarized here:

| Retired ID | Former content | Superseded by | Note |
|---|---|---|---|
| D-015 | MVP vehicle count: 3 | D-010 (10 vehicles in Commercial V1.0), D-028 | A development prototype may still use 1+ temporary vehicles (D-030) — that is a development-stage allowance, not product scope |
| D-016 | Former MVP challenge-type focus: 3 types (Time Trial / Precision / Drift) | D-031 (8-type event planning basis with per-championship mix) | The 3 types remain valid sub-sets usable in prototypes/vertical slice |
| D-017 | One reusable environment kit as product environment scope | D-028 (all five themes in Commercial V1.0) | One shared/kit-based foundation may still be used as a production technique (OQ-20), and for the development prototype |
| D-019 | Limited MVP scope as the product definition (3 vehicles, 1 environment kit, 8–12 events, 3 challenge types, 1 championship) | D-028 (Commercial V1.0) | The former MVP event/challenge mix may be re-used inside the development prototype or vertical slice where useful |

Also updated (not retired): D-006 (Balanced → high-quality realistic-inspired 3D), D-010 (provisional long-term target → approved Commercial V1.0 requirement), D-014 (re-anchored to Commercial V1.0 planning), D-018 (reframed as candidate names, distinct from D-029).

## Open decisions captured elsewhere
Camera behavior, scoring formulas, star thresholds, accessibility specifics, exact Unity version, render pipeline, and remaining platform integration details are listed in `OPEN_QUESTIONS.md` — not silently decided here.

## Change procedure
For every new or changed decision, record at the end of this file:
- ID
- Date
- Context / problem
- Options considered
- Decision and rationale
- Consequences / risks
- Approval status

## Change history
- 2026-11-03 (v0.1): Initial register created with D-001 … D-013.
- 2026-11-03 (v0.2): Audit pass. Added D-014 … D-027. D-008 normalized from "PROPOSED / PREFERRED" to APPROVED-INITIAL (Unity/C# is the agreed working direction; final version remains open). D-009 normalized from "TARGET" to APPROVED-INITIAL. No decision was promoted beyond APPROVED-INITIAL without new owner approval.
- 2026-11-03 (v0.2.1): Consistency pass. D-021 status normalized from "APPROVED-INITIAL as direction" to the registered status **APPROVED-INITIAL**, with an explicit note that only the monetization direction is approved (frequency/placement/amounts open under OQ-P03).
- 2026-11-03 (v0.3 — Commercial V1.0 scope transformation, owner-confirmed):
  - **D-006 updated:** visual quality upgraded from "Balanced realism" to "High-quality realistic-inspired 3D presentation"; Cinematic Motorsport preserved; WebGL performance implications recorded (quality targets must be balanced with browser performance via documented profiling gates, never silently downgraded).
  - **D-010 updated:** promoted from provisional long-term target to APPROVED-INITIAL: 10 playable vehicles **in Commercial V1.0**.
  - **D-014 updated:** re-anchored as the Commercial V1.0 planning distribution (Starter 2 / Sport 3 / Drift 2 / Supercar 3); remains PROVISIONAL as planning, not a frozen contract.
  - **D-015 RETIRED:** former MVP 3-vehicle product target superseded by D-010/D-028; change dated. One-vehicle prototype allowance belongs to the development process (D-030), not product scope.
  - **D-017 RETIRED:** former one-environment-kit product assumption superseded by the five approved themes via D-028; change dated.
  - **D-018 updated (not retired):** Rookie/Street/Pro/Elite/World preserved as candidate progression names, explicitly distinguished from championship count (D-029).
  - **D-019 RETIRED:** former limited MVP scope no longer defines the final product; superseded by D-028; historical record preserved in place and in the Retirements table.
  - **D-016 RETIRED:** former MVP 3-challenge-type focus superseded by D-031 (8-type planning basis with per-championship event mix); change dated.
  - **D-028 ADDED (APPROVED-INITIAL):** Commercial V1.0 product scope (10 vehicles, ≥3 championships, 5 environments, high-quality realistic-inspired 3D, complete gameplay/progression systems, Arabic+English, Playgama integration, QA/optimization). No event counts, championship names, or track counts invented.
  - **D-029 ADDED (APPROVED-INITIAL):** minimum three distinct championships forming a meaningful progression structure; names/structure open.
  - **D-030 ADDED (APPROVED-INITIAL):** three-level development scope model (Development Prototype → Vertical Slice → Commercial V1.0).
  - **D-031 ADDED (PROVISIONAL):** 8 long-term challenge types as Commercial V1.0 planning basis; per-championship event mix open.
- 2026-11-05 (v0.3.1 — baseline sign-off): **OQ-08 RESOLVED** — the owner explicitly approved the Commercial V1.0 documentation baseline (this documentation set as source of truth + the D-028 release target) on 2026-11-05. This records that approval only; **no other decision status was changed and no provisional item was promoted**. Evidence recorded in `OPEN_QUESTIONS.md` (Resolved questions) and `production/MILESTONES.md` M0.
