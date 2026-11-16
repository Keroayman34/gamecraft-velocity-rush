# Environment Design Documentation — Velocity Rush

Environment themes, kit rules, readability requirements, and governance. Authoritative decision references: `docs/DECISIONS.md` D-003, D-028 (all five themes are Commercial V1.0 content), D-030; game-design context `docs/GDD.md` §§18–19; visual direction `../ART_DIRECTION.md` §2/§8.

## Five approved environment briefs (OQ-20 RESOLVED 2026-11-16 — D-054)

| Theme | Production order | Approved character brief |
|---|---|---|
| Desert Circuit | 1 | Open desert terrain, sand/asphalt contrast, warm haze, sweeping curves, heat/dust readability |
| Coastal Circuit | 2 | Elevated seaside views, teal water, pale concrete, late-afternoon light, flowing sweepers and long sightlines |
| Mountain Pass | 3 | Granite, pine, snow, overcast light, elevation changes, cliff edges, demanding cornering |
| Neon City | 4 | Urban night, magenta/cyan, emissive signage, tighter geometry, luminous landmark language |
| World Arena | 5 | Global showcase venue, charcoal/white palette, stadium night, open presentation space |

Each theme must keep a distinct visual identity through palette, lighting, atmosphere, landmark/decor language, and track character. Shared technical kits are allowed, but a cosmetic swap of one kit does not satisfy distinctness. No approved reference images exist; the written briefs above plus in-engine captures are the Phase-9 reference basis (D-055).

All five are part of the Commercial V1.0 environment target (previously treated as long-term only — owner-confirmed change). Each must have its own art direction and a distinct driving experience.

## Phase 9 Stage 2/3 delivered packages (2026-11-16)

| Theme | Stable id / scene | Layout | Signature character | Generated dressing |
|---|---|---|---|---|
| Desert Circuit | `desert-signature` · `Assets/Scenes/Environments/desert-signature.unity` | 713.7 m, 7 segments, 12 m road | wide sweepers, long straights, midday haze | 98 decor, 4 mesa landmarks, sand/asphalt contrast |
| Coastal Circuit | `coastal-signature` · `Assets/Scenes/Environments/coastal-signature.unity` | 681.6 m, 9 segments, 11 m road | flowing sweepers, long sightlines, late afternoon/sea haze | 102 decor, lighthouse + sea markers, 2 collider-free water planes |
| Mountain Pass | `mountain-pass-signature` · `Assets/Scenes/Environments/mountain-pass-signature.unity` | 507.5 m, 9 segments, 8 m road, 22 m elevation | narrow sections, blind crests, cliff edges, dawn valley fog | 137 decor, 12 cliff-wall/snow-cap landmarks |
| Neon City | `neon-city-signature` · `Assets/Scenes/Environments/neon-city-signature.unity` | 485.1 m, 13 segments, 9 m road | tight technical corners, night, emissive signage | 212 decor, 21 neon spires, 53 emissive lane markers |
| World Arena | `world-arena-signature` · `Assets/Scenes/Environments/world-arena-signature.unity` | 606.8 m, 9 segments, 10 m road | mixed speed, close walls, stadium night | 216 decor, start arch + 3 grandstand pairs |

- **Authoring source:** `Assets/Data/Environments/<Theme>/<id>/` (definition, layout, lighting). **Generated art:** `Assets/Generated/Environments/<id>/` (7 materials, road/barrier/edge/skirt meshes, skybox).
- **Race format:** 3 ordered checkpoints at 1/4, 1/2, 3/4 of total length and finish at `TotalLength - 1`; gates sit on the centerline with visible gantries. Layouts are open point-to-point.
- **Wiring:** every scene carries the vehicle, camera rig, race/pause/UI audio, HUD, telemetry overlay, VFX, probe, roster/rig catalogs, selection spawner, and environment catalog. Fail-closed audit result: **0 missing scripts / 0 missing references / 7 environment materials**.
- **Regenerate:** menu `Tools/GameCraft Studio/Velocity Rush/Build Environment Content` or batch `-executeMethod VelocityRush.EditorTools.EnvironmentContentBuilder.BuildFromCommandLine`. `EditorBuildSettings` is not modified (D-057).
- **Grip/hazards:** presentation-only. No per-surface grip, hazard-physics, damage, or Phase-8 vehicle-controller value is authored here (D-057).
- **Status:** Stages 2–3 complete; M7 stays open pending Stage 4 WebGL profiling/browser evidence and the owner distinctness/readability review.

## Reuse is a technique, not an identity
- Reusable technical components, kits, and systems are allowed and encouraged for production efficiency.
- **Constraint:** reuse must not make the five environments look like one environment with minor cosmetic changes. Each theme keeps a distinct visual identity — lighting, materials, atmosphere, palette, and landmark language — verified in review against the environment acceptance criteria (OQ-20).
- Detailed designs are the approved briefs above (OQ-20 resolved 2026-11-16 — D-054).

## Environment design requirements
Each environment package must specify:
- Reference basis: the approved written brief plus in-engine captures (no approved reference images — D-055).
- Track layout(s) and gameplay purpose per configuration — **one signature + one reverse configuration per theme** (OQ-21 resolved 2026-11-16 — D-054).
- Road surface and grip assumptions for presentation/readability only. **Grip and hazards are visual-only; no Phase-8 vehicle-physics change is permitted** (D-057).
- Barriers, signage, props, and hazards.
- Realtime lighting/time-of-day approach — procedural skybox, skybox ambient, one realtime directional light, linear fog; no bake (OQ-A10 resolved 2026-11-16 — D-056).
- Landmark silhouettes (orientation landmarks for the player).
- Asset reuse plan and performance budget — per-environment **≤15 MB `.data`** and **≤8 materials** inside the OQ-T08 candidate ceilings (D-056).

## Track readability and gameplay visibility
- Racing line, braking points, checkpoints, and hazards readable at gameplay speed.
- Track edges, barriers, and jump landings visually unambiguous.
- Environmental lighting must not reduce road or obstacle visibility (`../ART_DIRECTION.md` §5).
- Global identity: no single-environment dependence (D-003).

## Performance considerations
- Modular kit pieces over unique hero geometry where efficient — without uniform kits that erase environmental identity.
- Controlled draw calls, texture sizes, and real-time lights (`../ART_DIRECTION.md` §11), with per-environment profiling evidence for heavier features. Phase-9 gates: per-environment **≤15 MB `.data`**, **≤8 materials**, inside the OQ-T08 candidate ceilings (D-056).
- Test lighting/effects overdraw cost in the target WebGL environment before approval. Phase 9 is realtime-only, so there is no lightmap/probe bake cost or artifact to review.

## Environment reference workflow
Reference-first: approved written brief → data-driven layout/lighting definition → environment package assembled with performance notes → distinctness and readability review. Concept images live in `../references/` with explicit status and are never production-ready 3D assets. **Production order (OQ-A04 resolved 2026-11-16 — D-054): Desert Circuit → Coastal Circuit → Mountain Pass → Neon City → World Arena.**
