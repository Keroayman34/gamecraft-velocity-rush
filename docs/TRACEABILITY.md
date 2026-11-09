# Traceability Matrix — Velocity Rush

Maps each authority-level project requirement to the document(s) where it is recorded, its status, and remaining gaps. Product target: **Commercial V1.0** (D-028). Maintained during documentation work; consult alongside `DECISIONS.md` and `OPEN_QUESTIONS.md`.

Legend — Status: **PA** = Approved-initial decision; **PR** = Provisional (requires validation); **OP** = Open (no decision). Complete: ✅ = fully documented; 🟡 = documented, detail pending resolution of linked open question.

## Identity and platform

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| Studio: GameCraft Studio | `PROJECT_CHARTER.md` §1, `design/brand/README.md` | PA | ✅ | — |
| Working title/subtitle/tagline | `PROJECT_CHARTER.md` §1, `README.md`, `design/brand/README.md` | PR (trademark validation open OQ-P05) | ✅ | Trademark validation before release |
| Engine Unity / language C# | `PROJECT_CHARTER.md` §7, `DECISIONS.md` D-008, `TDD.md` §2 | PA (Unity 6000.6.8f1 — Unity 6.6 Supported Update, not LTS — owner-approved 2026-11-05; OQ-T01 environment portion complete) | 🟡 | Playgama integration validation pending: actual WebGL build, Bridge v2.4.0 compatibility, runtime validation (open scope of OQ-T01) |
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
| Scope model: Development Prototype / Vertical Slice / Commercial V1.0 | `GDD.md` §§30–31, `DECISIONS.md` D-030, `production/ROADMAP.md` (phases + milestone register) | PA | ✅ | Vertical-slice composition to be defined with owner |
| 10 playable vehicles (Starter 2 / Sport 3 / Drift 2 / Supercar 3) | `GDD.md` §§9–10, `DECISIONS.md` D-010 (PA), D-014 (PR), `design/vehicles/README.md` | 10-vehicle PA; distribution PR | 🟡 | Roster identities/stats (OQ-14/OQ-06); asset quality criteria (OQ-A09) |
| Vehicle systems: selection, stats, garage, upgrades, classes, unlocking, differentiation | `GDD.md` §§9–12/§23, `design/ui-ux/README.md`, `design/vehicles/README.md` | PA direction / open detail | 🟡 | Garage + upgrade + unlock specs (OQ-A03/OQ-07/OQ-17) |
| Six upgrade categories; skill preservation rule | `GDD.md` §12, `DECISIONS.md` D-011 | PA (formulas open OQ-07) | 🟡 | Upgrade formulas validated in playtests |
| At least three distinct championships | `GDD.md` §§13–15, `DECISIONS.md` D-029 | PA (names/structure open OQ-15/16) | 🟡 | Championship design (OQ-05/OQ-15/OQ-16/OQ-18) |
| Five environment themes in Commercial V1.0 (Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena) | `GDD.md` §18, `DECISIONS.md` D-028, `design/environments/README.md` | PA (detail open OQ-20/21) | 🟡 | Per-environment designs, track counts (OQ-20/OQ-21) |
| Event/challenge planning basis: 8 long-term types; meaningful mix; not all 8 per championship | `GDD.md` §§16–17, `DECISIONS.md` D-031 | PR | 🟡 | Event distribution (OQ-16/OQ-18/OQ-19) |
| High-quality realistic-inspired 3D presentation (quality never silently downgraded) | `ART_DIRECTION.md` §2/§8a/§11, `DECISIONS.md` D-006, `TDD.md` §§16–20, Charter §10/§13 | PA | 🟡 | Quality acceptance criteria (OQ-A08/OQ-A09); profiling evidence |
| Complete gameplay/progression systems | `GDD.md` §§6, 20–24 | PA direction (detail open) | 🟡 | Scoring/rewards/save/UI specs (OQ-03/04/09/13/A03) |
| Arabic (RTL) + English (LTR) | `GDD.md` §25, `DECISIONS.md` D-013 | PA | ✅ | Per-screen RTL/LTR criteria during UI production |
| Playgama Bridge integration (ads direction D-021/D-027, analytics D-022) | `GDD.md` §28, `TDD.md` §§4/14, `PLAYGAMA_CONSTRAINTS.md`, milestone register M11 (`production/ROADMAP.md`) | PA direction (config open OQ-P03) | 🟡 | Ad configuration, save strategy (OQ-T05) |
| Product exclusions (multiplayer, PvP, open world, story, AI traffic, marketplace, real-money) | `PROJECT_CHARTER.md` §11, `GDD.md` §35, `DECISIONS.md` D-020 | PA | ✅ | — |
| Retired MVP framing (not the product definition) | `DECISIONS.md` D-015/D-016/D-017/D-019 RETIRED + Retirements table | RETIRED | ✅ | — |
| Performance risk register for the commercial scope | `PLAYGAMA_CONSTRAINTS.md` §5, `TDD.md` §20, Charter §14 | Proposed-unverified values | 🟡 | Profiling evidence log to be filled |

## Process and workflow

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| First critical gate = driving-feel validation | `PROJECT_CHARTER.md` §12, `GDD.md` §33, milestone register M3–M4 (`production/ROADMAP.md`), `DECISIONS.md` D-023 | PA | ✅ | — |
| Vertical slice validates the quality bar before mass content production | `production/ROADMAP.md` Phase 7, milestone register M5, `DECISIONS.md` D-030 | PA | ✅ | Slice composition defined with owner |
| 14-phase commercial roadmap with measurable milestones | `production/ROADMAP.md` (phases + milestone register M0–M12) | Proposed/merged | ✅ | Calendar dates deferred by policy |
| AI agent governance incl. scope discipline & quality rules | `docs/AI_AGENT_GUIDELINES.md` (§3a, §4a, §6–§9), `ai/MASTER_AGENT_PROMPT.md`, `ai/WORKFLOW.md`, `ai/prompts/README.md` | — | ✅ | — |
| Repository organization stays accurate | `docs/PROJECT_STRUCTURE.md` | — | ✅ | Future Assets/ layout at Unity setup |
| Formal scope/documentation sign-off (gated environment setup; now resolved) | `docs/OPEN_QUESTIONS.md` OQ-08, milestone register M0 (`production/ROADMAP.md`) | **RESOLVED — APPROVED 2026-11-05** | ✅ | None — resolution recorded in `OPEN_QUESTIONS.md` (Resolved questions), `DECISIONS.md` change history v0.3.1, milestone register M0 |
| Phase-3 technical architecture discovery + owner provisional review of P1–P8 | `docs/TDD.md` §§27–28 (discovery synthesis), `DECISIONS.md` D-032…D-038 (+ D-006 review note), `OPEN_QUESTIONS.md` review notes (OQ-T02/T03/T04/T05/T06/T07/T08, OQ-09) | PR (provisional directions only — DECISIONS v0.3.3, 2026-11-05) | 🟡 | Final technical decisions + validation evidence (A/B pipeline profiling, driving-feel gate, prototype input validation, build evidence); nothing implemented, built, or tested yet; package installs owner-gated |
| Phase-3 detailed design package (architecture + architecture audit + physics + input/camera + scene/data/save + WebGL strategy + integration review) — **consolidated 2026-11-05 into `docs/TDD.md` Part II (§§28–46); the nine standalone root documents were retired** | `docs/TDD.md` §28 (discovery/platform facts), §§29–35 (architecture), §36 (vehicle physics), §§37–38 (input/camera), §39 (save), §40 (WebGL), §§41–42 (errors/testing), §43 (integration review), §44 (draft decisions D-039…D-042 UNFILED + owner gates), §45 (risks) | PR / Proposal (recommendations; audit corrections A1–A4 applied; draft decisions D-039…D-042 UNFILED — owner-gated) | 🟡 | Owner approval of the architecture + filing decisions; all planned tests unbuilt; no WebGL build yet (OQ-T01/OQ-T07/T08 pending) |
| Unity setup pending statement + no implementation yet | `README.md`, `production/ROADMAP.md` Phase 2 | Current state | ✅ | Becomes obsolete when Phase 2 starts |
| Playgama platform constraints preserved & re-verify ritual | `PLAYGAMA_CONSTRAINTS.md` §§1–2, `TDD.md` §3/§22 | Re-verify | ✅ | Re-verification vs official docs before integration/submission |

## Development prototype implementation (T5.x)

| Requirement | Primary document(s) | Status | Complete | Remaining gap |
|---|---|---|---|---|
| T5.4 minimal race loop: `Countdown → Running → Finished`, configurable countdown, monotonic elapsed timer, ordered checkpoints, finish gating, immutable `RaceResult`, exactly-once `RaceFinished`, in-scene retry without reload | `docs/TDD.md` §29 S4/S5, §30.5, §35.5; `production/ROADMAP.md` T-5.4 status note | PR (development prototype; straight-track **one-way timed run**, not a lap) | ✅ | Actual-project Unity 6000.6.8f1 batch import/compile passed with 0 errors/warnings; full suites passed (129/129 EditMode; 4/4 PlayMode; zero failures); `TestTrack` script references resolve; retry/no-reload verified on the real project. No commercial Results UI, scoring, rewards, progression, save, multiplayer, AI, audio, or WebGL performance validation is included. |
| T5.5.1 permanent editor-only WebGL Development build tool: menu entry, blocking preflight (editor/WebGL module/active target/approved scene order), safe outside-project output with overwrite confirmation, BuildReport summary, artifact sizes, JSON report | `docs/TDD.md` §40.20–40.23; `production/ROADMAP.md` T-5.5 status note | PR (development-prototype tooling) | ✅ | Actual-project editor compile passed (0 errors/warnings); menu item resolved; preflight checks and negative safety guards passed; JSON report valid. This step created the tool only; the build/runtime evidence was subsequently completed under T5.5/T5.5.3 (2026-11-10). |
| T5.5 WebGL Development build + OQ-T08 prototype evidence; T5.5.3 tooling hardening: `build.reportPath`, permanent CLI batch entry, collision-free defaults, project-containing output rejection, scoped overwrite consent, stale-window reset | `docs/TDD.md` §40.20–40.23; `production/ROADMAP.md` T-5.5 closure note; `CHANGELOG.md` 2026-11-10 entry | PR (development-prototype evidence; not final budgets) | ✅ | Build `dev-20261109-210202`: exit 0, 0 errors/warnings, 17.354 s cold, 29,722,007 B total; five-artifact payload 29.71 MB (**~9.9% of the 300 MB Playgama archive limit**). Chromium 152/WebGL 2.0: wasm heap **91.50 MiB**, JS heap **~9.5 MiB**, no startup errors, no observed GC hitches; input and chase camera verified. CLI collision refusal left the sentinel intact; a clean rebuild exited 0 with a populated `reportPath`; final EditMode **160/160**, PlayMode **6/6**. Remaining gap: owner-set OQ-T08 budgets and the later device/browser matrix. |

## Verification notes
- Cross-document consistency verified as of the Commercial V1.0 transformation (2026-11-03, DECISIONS v0.3): retired MVP wording is preserved only in historical/retirement contexts.
- Phase-3 architecture review (2026-11-05, DECISIONS v0.3.3): owner review of the Phase-3 discovery proposals (P1–P8) is recorded strictly as PROVISIONAL decisions/status notes. No architecture item became approved; no implementation, build, package change, or test occurred to produce it; all governing open questions remain open.
- Phase-3 design package pass (2026-11-05, post-audit): audit corrections A1–A4 applied; five additional design documents created — all documentation-only; draft decision records D-039…D-042 proposed but deliberately NOT filed into `DECISIONS.md`; no decision status or open-question status changed; no tests executed and no WebGL build performed (claims remain pending: OQ-T01 compatibility scope, OQ-T07, OQ-T08 evidence).
- Documentation consolidation (2026-11-05, owner-approved): the nine standalone Phase-3 root documents were consolidated into `docs/TDD.md` Part II (§§28–46) and retired; `production/MILESTONES.md` was consolidated into `production/ROADMAP.md` (milestone register M0–M12) and retired. Unique content migrated before each deletion; decision statuses and open questions preserved untouched; supporting set (`README.md`, `docs/PROJECT_STRUCTURE.md`, `docs/AI_AGENT_GUIDELINES.md`, `docs/PLAYGAMA_CONSTRAINTS.md`, `CHANGELOG.md`) retained in place.
- This matrix marks gaps; no entry claims complete coverage where a contrary or silently-converted decision exists.
