# Velocity Rush — Art Direction & Visual Identity v0.1

**Studio:** GameCraft Studio  
**Status:** Proposed visual direction — pending human approval  
**Selected direction:** A — Cinematic Motorsport  
**Realism target:** Balanced → **updated (owner-confirmed, 2026-11-03): High-quality realistic-inspired 3D presentation** (D-006). Quality ambition is stronger than the former "Balanced" wording; performance-aware constraints below are optimization strategies, not quality downgrades.  
**Reference workflow:** Hybrid (visual references + written specifications)  
**Project:** Velocity Rush  
**Related concept:** Velocity Rush: Precision Trials  

**Reference board:** see [`design/references/art-direction-v0.1/`](references/art-direction-v0.1/README.md) (board image: `references/art-direction-v0.1/art-direction-board.png`). This document — not the board — is the authoritative written direction.

---

## 1. Purpose

This document defines the initial visual direction for Velocity Rush. It is a design constraint for future UI, environment, vehicle, lighting, and visual-effects work.

It is not a final production art bible. All values and examples remain provisional until reviewed in-engine and tested on target Playgama devices.

## 2. Art direction statement

Velocity Rush must look and feel like a professionally produced commercial product: a premium, high-energy global motorsport championship that is cinematic, confident, modern, and readable.

The game uses **high-quality realistic-inspired 3D visuals** (owner-upgraded quality target, D-006): high-quality vehicle models, convincing materials and surface details, professional lighting, believable environments, and high-quality visual effects — always within the real constraints of Unity WebGL delivery (browser performance, memory, archive size, loading time, device compatibility). Complexity is optimized through profiling and technique, never silently lowered below the quality bar.

### Design principles

1. **Commercial quality bar:** features must be planned to a professional production standard; "good enough for a prototype" is not the acceptance bar for product content.
2. **Cinematic, not cluttered:** dramatic lighting and composition, while keeping gameplay information easy to read.
3. **Motorsport credibility:** purposeful racing details, track markings, barriers, pit/garage elements, and believable vehicle proportions.
4. **Performance-aware high quality:** balanced complexity via measured optimization (texture atlases, LOD, baking), not reduced quality targets.
5. **High contrast and hierarchy:** important buttons, event states, scores, and race feedback immediately recognizable.
6. **Global identity:** varied environments without dependence on one country or region; all five Commercial V1.0 environments read as distinct experiences.
7. **Consistent system:** menus, garage, championships, HUD, results, effects, and settings must share one visual language.

## 3. Provisional color system

The following values are starting tokens inspired by the initial moodboard. They must be validated for contrast and accessibility before production.

| Token | Hex | Intended use |
|---|---|---|
| Primary Dark | `#0B0D12` | Main backgrounds |
| Accent Red | `#E10600` | Primary actions, active states, key highlights |
| Highlight Amber | `#FFB400` | Rewards, trophies, secondary highlights |
| Text Light | `#E6E6E6` | Primary text on dark surfaces |
| Surface | `#2A2F3A` | Cards and panels |
| UI Panel | `#1A1F27` | HUD and menu panels |

Rules:
- Red is for emphasis, not large uninterrupted backgrounds.
- Amber is reserved for rewards, progression highlights, and selected secondary emphasis.
- Maintain readable contrast for all text and interactive states.
- Do not use color as the only way to communicate status.

## 4. Typography

Provisional direction:
- English headings: a bold condensed motorsport-style sans serif.
- English body/UI: a highly legible sans serif.
- Arabic: a modern Arabic sans serif with clear shapes and strong legibility.

Candidate families shown in concept exploration are not licensed or approved production dependencies. Before implementation, verify font licensing, Arabic glyph coverage, weights, file size, and Unity/WebGL rendering.

Arabic UI must support RTL layout; English UI must support LTR layout. Avoid hard-coded positioning that breaks when the language changes.

## 5. Materials, lighting, and rendering

### Materials
- Vehicle paint: controlled metallic or clear-coat look; avoid excessive mirror-like reflections.
- Glass: visually convincing but inexpensive.
- Tires and rubber: matte, textured appearance.
- Track: asphalt with restrained roughness variation and visible racing markings.
- Metal barriers: clear material separation without excessive texture detail.

### Lighting
- Use cinematic key lighting and restrained rim lights for presentation scenes.
- Gameplay lighting must preserve road visibility, obstacle readability, and vehicle silhouette.
- Avoid relying on many real-time lights or expensive post-processing effects.
- Prefer baked, simplified, or otherwise optimized lighting where appropriate.

### Effects
- Tire smoke, sparks, speed lines, and dust should communicate events rather than obscure the road.
- Effects must be tested for overdraw and GPU cost in WebGL.
- Never let visual effects hide checkpoints, hazards, or UI.

## 6. UI/UX visual direction

All screens use dark motorsport surfaces, strong hierarchy, restrained red accents, and amber reward highlights.

### Core screens
1. Main Menu
2. Championship Selection / World Map
3. Garage / Vehicle Selection
4. Vehicle Upgrades
5. Pre-Event Briefing
6. In-Game HUD
7. Pause
8. Results
9. Settings

### UI rules
- One dominant primary action per screen.
- Keep labels short and readable.
- Use consistent spacing, panel radii, icon style, and button states.
- Provide clear hover, focus, pressed, disabled, selected, and loading states.
- Do not reproduce text embedded in generated reference images as authoritative copy.
- Arabic and English layouts must be checked separately.
- Gameplay HUD must prioritize road visibility and quick comprehension.

## 7. Vehicle visual direction

The Commercial V1.0 roster target is **10 playable vehicles** (D-010) with the planning distribution:

| Class | Count |
|---|---:|
| Starter | 2 |
| Sport | 3 |
| Drift | 2 |
| Supercar | 3 |
| **Total** | **10** |

All ten vehicles are Commercial V1.0 content; the roster names and vehicles shown in early concept imagery are illustrative only — not final vehicle specifications, licensed models, or approved assets. Temporary development prototypes may use fewer vehicles (D-030), which never reduces the product roster.

### Vehicle identity requirements
Each vehicle needs:
- Unique silhouette and recognizable front/rear identity.
- Defined class and gameplay role.
- Approved color/material options.
- Front, side, rear, and three-quarter reference views.
- Scale and proportion notes.
- Performance profile linked to the game balance specification.
- Clear distinction from real-world trademarks unless licensed.

### Upgrade categories
- Engine
- Tires
- Brakes
- Suspension
- Transmission
- Aerodynamics

Upgrades must visibly communicate improvement where practical, but gameplay effects and visual changes must be specified separately. Do not invent upgrade stats.

## 8. Environment direction

Global identity is the selected direction. **All five environment themes are Commercial V1.0 content targets (D-028):**
- Desert Circuit
- Neon City
- Mountain Pass
- Coastal Circuit
- World Arena

Each must have a distinct visual identity and distinct driving experience — reuse is a production technique, not an excuse for five copies of one environment. Per-environment art direction, lighting, materials, and atmosphere requirements: `design/environments/README.md`; detailed designs open (OQ-20/OQ-21). One shared technical/kit foundation may be used as a foundation, provided the five environments remain visibly distinct.

Each environment package should specify:
- Reference images and composition.
- Track layout(s) and gameplay purpose.
- Road surface and grip assumptions.
- Barriers, signage, props, and hazards.
- Lighting/time-of-day approach.
- Landmark silhouettes.
- Asset reuse plan and performance budget.

## 8a. Visual quality acceptance criteria (direction)
Before production asset approval, quality criteria are defined (OQ-A08/OQ-A09) covering at minimum:
- Vehicle models: high-quality modeling/texturing appropriate to hero assets; believable materials and surface detail.
- Materials/shaders: convincing paint, glass, rubber, asphalt, metal under the WebGL profiling gates (`TDD.md` §16).
- Lighting: professional, cinematic presentation while preserving gameplay readability.
- Environments: distinct identity per theme; coherent quality across all five.
- VFX: quality that enhances readability, cost-measured.
- UI: professionally produced layouts, consistent iconography/panels, full bilingual RTL/LTR correctness.
Acceptance is against approved references and written criteria — not compilability.

## 9. Reference asset governance

Every visual reference must have:
- A unique ID.
- A descriptive filename.
- A linked specification document.
- A status: `APPROVED`, `DRAFT`, `REJECTED`, or `ARCHIVED` (definitions in `design/references/README.md`).
- A note explaining what the image is authoritative for.

Generated images are visual targets, not automatically production-ready assets. They must not be treated as exact 3D models, UI source files, or validated technical specifications.

The AI agent must not silently replace or reinterpret an approved reference. If an exact implementation is infeasible, it must report the limitation and propose options before changing direction.

## 10. AI implementation requirements

Before implementing a visual feature, the agent must:
1. Read the relevant GDD/TDD sections and this document.
2. Read the feature-specific design specification.
3. Inspect all references marked `APPROVED`.
4. Identify missing assets, unknowns, and technical constraints.
5. Implement only the agreed scope.
6. Compare the result with references at matching screen sizes or camera angles.
7. Report deviations, placeholders, performance concerns, and tests performed.

A feature is not visually complete merely because it compiles or runs.

## 11. Performance guardrails (quality-preserving optimization strategies)

The target is Playgama Web using Unity WebGL, at the high-quality presentation bar (`D-006`). Final budgets must be established through profiling (OQ-T08), but the art direction plans for optimization **without lowering the quality target**:
- Controlled polygon counts and draw calls via disciplined modeling and LOD strategy — not lower-detail products.
- Texture atlases, compressed texture formats, and sensible resolutions.
- Reuse of materials, kits, and environment props **while keeping the five environments visibly distinct**.
- Limited transparent particles and overdraw via effect tuning; proof of cost before envelope expansion.
- Expensive real-time shadows and post-processing adopted only where profiling proves their cost is acceptable (OQ-T02/OQ-A10); otherwise achieved look via baking and composition.
- Compressed assets appropriate to target devices.
- Loading and memory behavior tested on real target browsers/devices.

These are delivery strategies for the quality bar, not justifications to downgrade it: a deliberate quality reduction is a decision requiring owner approval with profiling evidence.

Do not invent final polygon, texture, draw-call, or frame-rate budgets before the first representative prototype is profiled.

## 12. Approval checklist

A visual package can be marked `APPROVED` only when:
- The visual intent is clear.
- The reference is linked to a written specification.
- Required states and responsive behavior are described for UI.
- Asset ownership/licensing is understood.
- Performance risks are identified.
- The visual quality meets the written acceptance criteria (OQ-A08) — where defined.
- The user has explicitly approved the direction.

## 13. Open decisions

- Final logo artwork and typography.
- Exact font families and licenses.
- Final color tokens and accessibility contrast.
- Camera presentation style.
- Vehicle-by-vehicle design and roster names (all 10 Commercial V1.0 vehicles — OQ-14).
- Environment production order and per-environment kit design (OQ-20/OQ-21).
- Exact UI layouts and interaction states for the full commercial screen set.
- Target device performance budgets for the high-quality presentation (OQ-T08).
- Visual quality acceptance criteria details (OQ-A08/OQ-A09).
