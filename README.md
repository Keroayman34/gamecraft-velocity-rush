# Velocity Rush

**Studio:** GameCraft Studio  
**Project status:** Pre-production / Foundation  
**Working concept:** Velocity Rush: Precision Trials

## What is this repository?
This repository is the single source of truth for the design, technical decisions, production assets, and implementation of Velocity Rush.

## Current approved direction
- Genre: 3D skill-based arcade driving
- Game mode: Championship
- Driving model: Simcade
- World identity: Global
- Orientation: Landscape
- Visual direction: Cinematic Motorsport
- Visual realism: Balanced
- Visual reference workflow: Hybrid (images + written specifications)
- Target distribution: Playgama Web
- Engine preference: Unity with C#
- Initial vehicle target: 10 cars
- Vehicle upgrade categories: Engine, Tires, Brakes, Suspension, Transmission, Aerodynamics

These are initial design decisions, not proof of technical feasibility or final production scope.

## Current phase
**Phase 1 — Project Documentation Foundation**

Do not start gameplay implementation until the concept, technical plan, and first prototype scope are approved.

## Where to start
1. Read `docs/PROJECT_CHARTER.md`.
2. Read `docs/AI_AGENT_GUIDELINES.md`.
3. Read `docs/PROJECT_STRUCTURE.md`.
4. Review `docs/DECISIONS.md`.
5. Review `design/ART_DIRECTION.md`.
6. Check `docs/OPEN_QUESTIONS.md` before proposing decisions.

## Repository areas
- `docs/`: product, design, technical, platform, and process documentation.
- `design/`: approved visual direction, UI specifications, and visual references.
- `ai/`: reusable AI-agent instructions and task prompt templates.
- `production/`: roadmap, milestones, and decision tracking.
- `Assets/`: Unity runtime assets only; create after Unity project setup.
- `Packages/`, `ProjectSettings/`: Unity-generated project configuration; create with Unity.

## Important
Generated concept images are references, not production-ready 3D models, textures, or editable UI source files. Every asset must have a clear status, source, license, and intended use.
