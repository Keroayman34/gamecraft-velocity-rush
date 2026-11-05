# Environment Design Documentation — Velocity Rush

Environment themes, kit rules, readability requirements, and governance. Authoritative decision references: `docs/DECISIONS.md` D-003, D-028 (all five themes are Commercial V1.0 content), D-030; game-design context `docs/GDD.md` §§18–19; visual direction `../ART_DIRECTION.md` §2/§8.

## Five approved environment themes — Commercial V1.0 content targets (D-028)

| Theme | Character direction |
|---|---|
| Desert Circuit | Open desert terrain, sweeping curves, heat/dust readability |
| Neon City | Urban night presentation, luminous signage, tighter geometry |
| Mountain Pass | Elevation changes, cliff edges, demanding cornering |
| Coastal Circuit | Elevated seaside views, flowing sweepers, long sightlines |
| World Arena | Global showcase venue, open presentation space |

All five are part of the Commercial V1.0 environment target (previously treated as long-term only — owner-confirmed change). Each must have its own art direction and a distinct driving experience.

## Reuse is a technique, not an identity
- Reusable technical components, kits, and systems are allowed and encouraged for production efficiency.
- **Constraint:** reuse must not make the five environments look like one environment with minor cosmetic changes. Each theme keeps a distinct visual identity — lighting, materials, atmosphere, palette, and landmark language — verified in review against the environment acceptance criteria (OQ-20).
- Detailed designs (identity, lighting, atmosphere, landmarks, hazards): **open** (OQ-20).

## Environment design requirements
Each environment package must specify:
- Reference images with composition notes.
- Track layout(s) and gameplay purpose per track configuration (track counts/layouts open — OQ-21).
- Road surface and grip assumptions (linked to the handling model once decided — OQ-02).
- Barriers, signage, props, and hazards.
- Lighting / time-of-day approach (lighting strategy open — OQ-A10).
- Landmark silhouettes (orientation landmarks for the player).
- Asset reuse plan and performance budget (profiling-gated — OQ-T08).

## Track readability and gameplay visibility
- Racing line, braking points, checkpoints, and hazards readable at gameplay speed.
- Track edges, barriers, and jump landings visually unambiguous.
- Environmental lighting must not reduce road or obstacle visibility (`../ART_DIRECTION.md` §5).
- Global identity: no single-environment dependence (D-003).

## Performance considerations
- Modular kit pieces over unique hero geometry where efficient — without uniform kits that erase environmental identity.
- Controlled draw calls, texture sizes, and real-time lights (`../ART_DIRECTION.md` §11), with per-environment profiling evidence for heavier features.
- Test lighting/effects overdraw cost in the target WebGL environment before approval.

## Environment reference workflow
Reference-first: theme concept → kit references approved → track configuration spec → environment package assembled with performance notes and distinctness review. Concept images live in `../references/` with explicit status and are never production-ready 3D assets. Production order across the five themes: open (OQ-A04).
