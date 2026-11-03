# Project Charter — Velocity Rush

**Version:** 0.1  
**Status:** Initial direction approved; details remain subject to validation.

## 1. Vision
Create a polished 3D driving game where players progress through a global championship by mastering speed, braking, cornering, drifting, jumps, and precision driving.

## 2. Product pillars
1. Skill over speed: driving technique matters, not only top speed.
2. Easy to learn, hard to master: accessible controls with room for mastery.
3. Championship progression: events form a structured progression journey.
4. Global racing identity: varied environments without dependence on one region.
5. Continuous improvement: replay, personal bests, vehicle progression, and mastery.

## 3. Initial product decisions
| Area | Direction |
|---|---|
| Genre | 3D skill-based arcade driving |
| Game mode | Championship |
| Driving model | Simcade |
| World identity | Global |
| Orientation | Landscape |
| Visual direction | Cinematic Motorsport |
| Realism | Balanced |
| Players | Single-player initially |
| Engine | Unity / C# preferred |
| Distribution target | Playgama Web |
| Languages | Arabic and English |
| Vehicle roster target | 10 cars |

## 4. Initial gameplay pillars
- Circuit racing and time trials.
- Precision driving and checkpoint challenges.
- Drift challenges.
- Jump and landing challenges.
- Obstacle avoidance and mixed-skill events.

The MVP event mix and exact count must be validated before production.

## 5. Vehicle progression
Initial vehicle classes:
- Starter: 2
- Sport: 3
- Drift: 2
- Supercar: 3

Total target: 10 vehicles.

Upgrade categories:
- Engine
- Tires
- Brakes
- Suspension
- Transmission
- Aerodynamics

The roster, names, stats, unlock rules, upgrade costs, and balance are not final.

## 6. Monetization direction
Initial hypothesis: optional rewarded ads and carefully limited interstitial ads through Playgama Bridge. No ads during active driving. Reward only after the platform confirms successful completion. Monetization must not undermine progression or player trust.

## 7. Non-goals for the first playable prototype
- Multiplayer or online PvP.
- Open world.
- Large cinematic story campaign.
- Five fully built environments.
- Real-money purchases.
- Complex online economy.

## 8. Success principle
The first proof point is whether the driving itself is enjoyable and understandable. Do not use revenue projections or untested retention assumptions as proof of product-market fit.

## 9. Scope control
Any material change to genre, core loop, platform, monetization, vehicle count, or visual direction must be recorded in `docs/DECISIONS.md` and approved before implementation.
