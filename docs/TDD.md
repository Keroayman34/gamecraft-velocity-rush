# Technical Design Document — Velocity Rush

**Version:** 0.1 skeleton  
**Status:** Architecture not yet decided. Do not infer final architecture from this placeholder.

## 1. Purpose
Define how the approved game design will be implemented in Unity/C#, including runtime systems, data, scenes, platform integration, testing, and performance.

## 2. Current technical direction
- Preferred engine: Unity
- Language: C#
- Target distribution: Playgama Web
- WebGL/browser performance is a primary constraint.
- Exact Unity version and render pipeline remain open until environment compatibility is checked.

## 3. Sections to complete after environment setup and technical discovery
1. Unity version and project settings
2. Render pipeline and quality tiers
3. Repository and Unity folder conventions
4. Scene architecture
5. Runtime architecture and dependency boundaries
6. Vehicle physics and controller strategy
7. Input abstraction
8. Camera system
9. Event and championship systems
10. Scoring and progression data
11. Save/load and Playgama Bridge
12. Ads and analytics integration
13. Localization and RTL/LTR support
14. Asset import and optimization pipeline
15. Testing strategy
16. Build and CI pipeline
17. Performance budgets and profiling plan
18. Security, privacy, and secrets handling

## 4. Architecture decision rule
For each major technical decision, document the problem, options, trade-offs, chosen approach, and consequences. Record durable decisions as ADRs under `docs/architecture/ADR/` when that folder is introduced.

## 5. Do not implement yet
No final class diagrams, namespaces, service locators, dependency injection framework, or manager hierarchy has been approved. Avoid creating them before the technical discovery and prototype.
