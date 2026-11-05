# Velocity Rush: Precision Trials

**Tagline:** Master the road. Conquer every challenge.  
**Studio:** GameCraft Studio  
**Project status:** Pre-production — Commercial V1.0 product planning (documentation phase)  
**Working concept:** Velocity Rush: Precision Trials (working title; not trademark-validated)

## What is this repository?
This repository is the single source of truth for the design, technical decisions, production assets, and implementation of Velocity Rush.

## Current approved direction
- Genre: 3D skill-based arcade driving
- Product target: **Commercial V1.0** — a complete, commercially positioned release (not a small prototype or demo)
- Game mode: Championship system with **at least three distinct championships**
- Driving model: Simcade
- World identity: Global
- Orientation: Landscape
- Visual direction: Cinematic Motorsport with **high-quality realistic-inspired 3D presentation**
- Visual reference workflow: Hybrid (images + written specifications)
- Target distribution: Playgama Web
- Engine: Unity with C# (final Unity version still open)
- Vehicle roster: **10 playable vehicles in Commercial V1.0** (planning distribution: Starter 2 / Sport 3 / Drift 2 / Supercar 3)
- Environments: **all five approved themes** — Desert Circuit, Neon City, Mountain Pass, Coastal Circuit, World Arena
- Vehicle upgrade categories: Engine, Tires, Brakes, Suspension, Transmission, Aerodynamics
- Languages: Arabic (RTL) and English (LTR)

Development stages (prototype → vertical slice → Commercial V1.0) are production-validation levels only — they never reduce the product scope (see `docs/DECISIONS.md` D-028/D-030).

## Current phase
**Phase 1 — Project Documentation Foundation**

Do not start gameplay implementation until the concept, technical plan, and first prototype scope are approved.

## Current development phase
**Phase 1 — Product Documentation & Commercial Scope Approval**

- Unity (Hub, Editor, and modules) is **not yet set up**. Environment setup is pending and must not begin until the documentation foundation and Commercial V1.0 product scope are approved (see `docs/OPEN_QUESTIONS.md` OQ-08, `production/ROADMAP.md`).
- No Unity project, runtime code, or game assets exist yet.
- Note: the game is **not yet implemented**; this repository currently contains planning documentation and design references only.
- Getting started for future agents and developers: read the documents below in order; do not implement anything before currently open items in `docs/OPEN_QUESTIONS.md` that block your task are resolved by the project owner.

## Where to start
1. Read `docs/PROJECT_CHARTER.md` — project identity, vision, and non-goals.
2. Read `docs/AI_AGENT_GUIDELINES.md` — mandatory working rules.
3. Read `docs/PROJECT_STRUCTURE.md` — where files belong.
4. Review `docs/DECISIONS.md` — the central decision register.
5. Read `docs/GDD.md` (game design) and `docs/TDD.md` (technical planning; architecture still open).
6. Review `design/ART_DIRECTION.md` and the reference board in `design/references/art-direction-v0.1/`.
7. Review `docs/PLAYGAMA_CONSTRAINTS.md` before any platform-facing work.
8. Check `docs/OPEN_QUESTIONS.md` before proposing decisions.
9. Check `docs/TRACEABILITY.md` for requirement-to-document coverage.

## Repository areas
- `docs/`: product, design, technical, platform, and process documentation.
- `design/`: approved visual direction, UI specifications, and visual references.
- `ai/`: reusable AI-agent instructions and task prompt templates.
- `production/`: roadmap, milestones, and decision tracking.
- `Assets/`: Unity runtime assets only; create after Unity project setup.
- `Packages/`, `ProjectSettings/`: Unity-generated project configuration; create with Unity.

## Important
Generated concept images are references, not production-ready 3D models, textures, or editable UI source files. Every asset must have a clear status, source, license, and intended use.
