# Velocity Rush — Art Direction & Visual Identity v0.1

**Studio:** GameCraft Studio  
**Status:** Proposed visual direction — pending human approval  
**Selected direction:** A — Cinematic Motorsport  
**Realism target:** Balanced  
**Reference workflow:** Hybrid (visual references + written specifications)  
**Project:** Velocity Rush  
**Related concept:** Velocity Rush: Precision Trials

---

## 1. Purpose

This document defines the initial visual direction for Velocity Rush. It is a design constraint for future UI, environment, vehicle, lighting, and visual-effects work.

It is not a final production art bible. All values and examples remain provisional until reviewed in-engine and tested on target Playgama devices.

## 2. Art direction statement

Velocity Rush should feel like a premium, high-energy global motorsport championship: cinematic, confident, modern, and readable.

The game should use realistic-inspired 3D visuals with controlled complexity. Prioritize strong silhouettes, convincing materials, dramatic but legible lighting, and clear gameplay readability over expensive visual effects that harm WebGL performance.

### Design principles

1. **Cinematic, not cluttered:** dramatic lighting and composition, while keeping gameplay information easy to read.
2. **Motorsport credibility:** purposeful racing details, track markings, barriers, pit/garage elements, and believable vehicle proportions.
3. **Performance-aware realism:** balanced geometry, textures, materials, lighting, and effects for browser delivery.
4. **High contrast and hierarchy:** important buttons, event states, scores, and race feedback must be immediately recognizable.
5. **Global identity:** avoid making the whole game visually dependent on one country or region.
6. **Consistent system:** menus, garage, championship map, HUD, results, and settings must share one visual language.

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

The initial roster target is **10 playable vehicles**.

Provisional class distribution:

| Class | Count |
|---|---:|
| Starter | 2 |
| Sport | 3 |
| Drift | 2 |
| Supercar | 3 |
| **Total** | **10** |

The roster names and vehicles shown in early concept imagery are illustrative only. They are not final vehicle specifications, real-world licensed models, or approved production assets.

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

Global identity is the selected direction. Initial environment concepts:
- Desert Circuit
- Neon City
- Mountain Pass
- Coastal Circuit
- World Arena

These are long-term environment themes, not a commitment to build five complete environments in the MVP. The first playable milestone should use one optimized environment kit with multiple reusable track configurations.

Each environment package should specify:
- Reference images and composition.
- Track layout and gameplay purpose.
- Road surface and grip assumptions.
- Barriers, signage, props, and hazards.
- Lighting/time-of-day.
- Landmark silhouettes.
- Asset reuse plan and performance budget.

## 9. Reference asset governance

Every visual reference must have:
- A unique ID.
- A descriptive filename.
- A linked specification document.
- A status: `DRAFT`, `APPROVED`, or `REJECTED`.
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

## 11. Performance guardrails

The target is Playgama Web using Unity WebGL. Final budgets must be established through profiling, but the art direction must assume:
- Controlled polygon counts and draw calls.
- Texture atlases and sensible texture resolutions.
- Reuse of materials and environment props.
- Limited transparent particles and overdraw.
- Minimal expensive real-time shadows and post-processing.
- Compressed assets appropriate to target devices.
- Loading and memory behavior tested on real target browsers/devices.

Do not invent a final polygon, texture, draw-call, or frame-rate budget before the first representative prototype is profiled.

## 12. Approval checklist

A visual package can be marked `APPROVED` only when:
- The visual intent is clear.
- The reference is linked to a written specification.
- Required states and responsive behavior are described for UI.
- Asset ownership/licensing is understood.
- Performance risks are identified.
- The user has explicitly approved the direction.

## 13. Open decisions

- Final logo artwork and typography.
- Exact font families and licenses.
- Final color tokens and accessibility contrast.
- Camera presentation style.
- Vehicle-by-vehicle design and roster names.
- First MVP environment.
- Exact UI layouts and interaction states.
- Target device performance budgets.
- Whether mobile browser support is included in the first release.
