# Vehicle Design Documentation — Velocity Rush

Vehicle design roster targets, specification template, and governance. Authoritative decision references: `docs/DECISIONS.md` D-010/D-011/D-014/D-015(retired)/D-028; game-design context in `docs/GDD.md` §§9–12.

## Commercial V1.0 roster target

**10 playable vehicles** (D-010, APPROVED-INITIAL) across four classes — owner-confirmed planning distribution (D-014, provisional as planning):

| Class | Count | Intended handling role |
|---|---:|---|
| Starter | 2 | Learning, forgiveness |
| Sport | 3 | Balanced general use |
| Drift | 2 | Drift-biased handling |
| Supercar | 3 | High capability, hard to master |
| **Total** | **10** | |

- Every vehicle must support: distinct driving characteristics, selection, unlocking, statistics, garage presentation, upgrade compatibility, and meaningful class differentiation.
- Roster identities, stats, and per-vehicle specs are **unassigned** (OQ-14/OQ-06); never invented by an agent.

## Development prototype vehicles vs commercial roster
Temporary development prototypes may use one or a few vehicles just for validation (D-030). Such prototype vehicles are explicitly temporary:
- They are not part of the commercial roster plan.
- They do not define, replace, or reduce the 10-vehicle Commercial V1.0 target.
- They are not presented as production content.

## Performance and handling attributes to be defined
The attribute set is **not finalized**. Directionally: top speed, acceleration, braking, grip/cornering, drift behavior, jump/landing stability, steering responsiveness. Exact names, scales, and values require approval — never invented.

Per-class visual/behavior differentiation is a goal: each class must be distinguishable in silhouette, materials, and handling character (`../ART_DIRECTION.md` §7). Commercial vehicle models target the high-quality realistic-inspired bar (`design/ART_DIRECTION.md`); vehicle asset quality requirements are a dedicated open question (OQ-A09).

## Vehicle specification template
One folder per vehicle (`starter-01/`, ...). Each vehicle package must contain:
- Name (fictional) and class.
- Asset folder with reference views: front, side, rear, three-quarter.
- Intended gameplay role and skill emphasis.
- Performance profile (once stats are approved; linked to the GDD/upgrade spec).
- Scale and proportion notes.
- Color/material options.
- Upgrade category mapping (`docs/DECISIONS.md` D-011).
- Asset provenance: source, creator, license, format (`docs/AI_AGENT_GUIDELINES.md` §6).
- Status (Draft / Approved / Rejected).

## Upgrade categories
Engine, Tires, Brakes, Suspension, Transmission, Aerodynamics (D-011). Upgrade formulas and balance values are **open** (OQ-07); the skill-preservation rule in `docs/GDD.md` §12 is binding.

## Asset licensing requirements
- All vehicle names, shapes, liveries, and branding must be fictional or properly licensed. No real-world makes, models, badges, or trademarks without explicit permission.
- Concept/reference images are not production models (`../references/README.md`).

## Reference-first asset workflow
Reference views + role → written specification → references approved → production asset work begins, against the commercial quality criteria (where defined, OQ-A08). Complexity must pass WebGL profiling gates (`../ART_DIRECTION.md` §11) — quality is delivered via optimization technique, not lowered targets.
