# Traceability Matrix — Velocity Rush

Maps each authority-level project requirement to the document(s) where it is recorded, its status, and remaining gaps. Product target: **Commercial V1.0** (D-028). Maintained during documentation work; consult alongside `DECISIONS.md` and `OPEN_QUESTIONS.md`.

Legend — Status: **PA** = Approved-initial decision; **PR** = Provisional (requires validation); **OP** = Open (no decision). Complete: ✅ = fully documented; 🟡 = documented, detail pending resolution of linked open question.

## Identity and platform

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| Studio: GameCraft Studio | `PROJECT_CHARTER.md` §1, `design/brand/README.md` | PA | ✅ | — |
| Working title/subtitle/tagline | `PROJECT_CHARTER.md` §1, `README.md`, `design/brand/README.md` | PR (trademark validation open OQ-P05) | ✅ | Trademark validation before release |
| Engine Unity / language C# | `PROJECT_CHARTER.md` §7, `DECISIONS.md` D-008, `TDD.md` §2 | PA (version open OQ-T01) | 🟡 | Unity stable LTS + WebGL Build Support verification at environment setup |
| Initial platform: Playgama Web | `PROJECT_CHARTER.md` §7, `DECISIONS.md` D-009, `TDD.md` | PA | ✅ | — |
| Single-player initially | `PROJECT_CHARTER.md`, `DECISIONS.md` D-012 | PA | ✅ | — |
| Landscape orientation | `PROJECT_CHARTER.md`, `DECISIONS.md` D-004 | PA | ✅ | — |
| Global visual identity | `ART_DIRECTION.md`, `DECISIONS.md` D-003 | PA | ✅ | — |
| Simcade driving model (never a full sim without owner approval) | `DECISIONS.md` D-002, `GDD.md` §7 | PA (exact handling open OQ-02) | 🟡 | Handling model after driving prototype |
| Decision register with dated change history | `DECISIONS.md` | — | ✅ | Living document |
| Open questions register | `OPEN_QUESTIONS.md` | — | ✅ | Living document |

## Commercial product scope (D-028)

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| Commercial V1.0 is the initial product release | `GDD.md` §1/§29, `DECISIONS.md` D-028/D-030, Charter §10 | PA | ✅ | None — baseline sign-off (OQ-08) resolved 2026-11-05; final release acceptance criteria remain open (OQ-13) |
| Scope model: Development Prototype / Vertical Slice / Commercial V1.0 | `GDD.md` §§30–31, `DECISIONS.md` D-030, `production/ROADMAP.md`/`MILESTONES.md` | PA | ✅ | Vertical-slice composition to be defined with owner |
| 10 playable vehicles (Starter 2 / Sport 3 / Drift 2 / Supercar 3) | `GDD.md` §§9–10, `DECISIONS.md` D-010 (PA), D-014 (PR), `design/vehicles/README.md` | 10-vehicle PA; distribution PR | 🟡 | Roster identities/stats (OQ-14/OQ-06); asset quality criteria (OQ-A09) |
| Vehicle systems: selection, stats, garage, upgrades, classes, unlocking, differentiation | `GDD.md` §§9–12/§23, `design/ui-ux/README.md`, `design/vehicles/README.md` | PA direction / open detail | 🟡 | Garage + upgrade + unlock specs (OQ-A03/OQ-07/OQ-17) |
| Six upgrade categories; skill preservation rule | `GDD.md` §12, `DECISIONS.md` D-011 | PA (formulas open OQ-07) | 🟡 | Upgrade formulas validated in playtests |
| At least three distinct championships | `GDD.md` §§13–15, `DECISIONS.md` D-029 | PA (names/structure open OQ-15/16) | 🟡 | Championship design (OQ-05/OQ-15/OQ-16/OQ-18) |
| Five environment themes in Commercial V1.0 (Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena) | `GDD.md` §18, `DECISIONS.md` D-028, `design/environments/README.md` | PA (detail open OQ-20/21) | 🟡 | Per-environment designs, track counts (OQ-20/OQ-21) |
| Event/challenge planning basis: 8 long-term types; meaningful mix; not all 8 per championship | `GDD.md` §§16–17, `DECISIONS.md` D-031 | PR | 🟡 | Event distribution (OQ-16/OQ-18/OQ-19) |
| High-quality realistic-inspired 3D presentation (quality never silently downgraded) | `ART_DIRECTION.md` §2/§8a/§11, `DECISIONS.md` D-006, `TDD.md` §§16–20, Charter §10/§13 | PA | 🟡 | Quality acceptance criteria (OQ-A08/OQ-A09); profiling evidence |
| Complete gameplay/progression systems | `GDD.md` §§6, 20–24 | PA direction (detail open) | 🟡 | Scoring/rewards/save/UI specs (OQ-03/04/09/13/A03) |
| Arabic (RTL) + English (LTR) | `GDD.md` §25, `DECISIONS.md` D-013 | PA | ✅ | Per-screen RTL/LTR criteria during UI production |
| Playgama Bridge integration (ads direction D-021/D-027, analytics D-022) | `GDD.md` §28, `TDD.md` §§4/14, `PLAYGAMA_CONSTRAINTS.md`, `MILESTONES.md` M11 | PA direction (config open OQ-P03) | 🟡 | Ad configuration, save strategy (OQ-T05) |
| Product exclusions (multiplayer, PvP, open world, story, AI traffic, marketplace, real-money) | `PROJECT_CHARTER.md` §11, `GDD.md` §35, `DECISIONS.md` D-020 | PA | ✅ | — |
| Retired MVP framing (not the product definition) | `DECISIONS.md` D-015/D-016/D-017/D-019 RETIRED + Retirements table | RETIRED | ✅ | — |
| Performance risk register for the commercial scope | `PLAYGAMA_CONSTRAINTS.md` §5, `TDD.md` §20, Charter §14 | Proposed-unverified values | 🟡 | Profiling evidence log to be filled |

## Process and workflow

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| First critical gate = driving-feel validation | `PROJECT_CHARTER.md` §12, `GDD.md` §33, `production/MILESTONES.md` M3–M4, `DECISIONS.md` D-023 | PA | ✅ | — |
| Vertical slice validates the quality bar before mass content production | `production/ROADMAP.md` Phase 7, `MILESTONES.md` M5, `DECISIONS.md` D-030 | PA | ✅ | Slice composition defined with owner |
| 14-phase commercial roadmap with measurable milestones | `production/ROADMAP.md`, `production/MILESTONES.md` (M0–M12) | Proposed/merged | ✅ | Calendar dates deferred by policy |
| AI agent governance incl. scope discipline & quality rules | `docs/AI_AGENT_GUIDELINES.md` (§3a, §4a, §6–§9), `ai/MASTER_AGENT_PROMPT.md`, `ai/WORKFLOW.md`, `ai/prompts/README.md` | — | ✅ | — |
| Repository organization stays accurate | `docs/PROJECT_STRUCTURE.md` | — | ✅ | Future Assets/ layout at Unity setup |
| Formal scope/documentation sign-off (gated environment setup; now resolved) | `docs/OPEN_QUESTIONS.md` OQ-08, `production/MILESTONES.md` M0 | **RESOLVED — APPROVED 2026-11-05** | ✅ | None — resolution recorded in `OPEN_QUESTIONS.md` (Resolved questions), `DECISIONS.md` change history v0.3.1, `MILESTONES.md` M0 |
| Unity setup pending statement + no implementation yet | `README.md`, `production/ROADMAP.md` Phase 2 | Current state | ✅ | Becomes obsolete when Phase 2 starts |
| Playgama platform constraints preserved & re-verify ritual | `PLAYGAMA_CONSTRAINTS.md` §§1–2, `TDD.md` §3/§22 | Re-verify | ✅ | Re-verification vs official docs before integration/submission |

## Verification notes
- Cross-document consistency verified as of the Commercial V1.0 transformation (2026-11-03, DECISIONS v0.3): retired MVP wording is preserved only in historical/retirement contexts.
- This matrix marks gaps; no entry claims complete coverage where a contrary or silently-converted decision exists.
