# Project Structure Guide

This file explains where project information and assets belong. It is a repository organization guide, not the game's technical architecture.

## 1. Current repository structure (exists today)

```text
<repository root>/
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── .gitignore
├── docs/
│   ├── PROJECT_CHARTER.md
│   ├── AI_AGENT_GUIDELINES.md
│   ├── PROJECT_STRUCTURE.md
│   ├── DECISIONS.md
│   ├── OPEN_QUESTIONS.md
│   ├── PLAYGAMA_CONSTRAINTS.md
│   ├── GDD.md
│   ├── TDD.md
│   └── TRACEABILITY.md
├── design/
│   ├── ART_DIRECTION.md
│   ├── brand/README.md
│   ├── ui-ux/README.md
│   ├── vehicles/README.md
│   ├── environments/README.md
│   ├── gameplay/README.md
│   └── references/
│       ├── README.md
│       └── art-direction-v0.1/
│           ├── README.md
│           └── art-direction-board.png
├── ai/
│   ├── MASTER_AGENT_PROMPT.md
│   ├── WORKFLOW.md
│   └── prompts/
│       └── README.md
└── production/
    ├── ROADMAP.md
    └── MILESTONES.md
```

Note: `docs/TRACEABILITY.md` maps project requirements to the documents that cover them. Screen-, vehicle-, and environment-specific specification files are added inside their design folders only when their packages are produced; a new file must have a justified responsibility (see `AI_AGENT_GUIDELINES.md`).

## 2. After Unity project creation (future — does not exist yet)

Unity will add its project folders. The intended high-level arrangement is:

```text
<repository root>/
├── Assets/                   # future — runtime assets only
├── Packages/                 # future — Unity-generated
├── ProjectSettings/          # future — Unity-generated
├── UserSettings/             # future — Unity-generated; local/editor-specific; review ignore rules
├── docs/
├── design/
├── ai/
└── production/
```

Existing folders: `docs/`, `design/`, `ai/`, `production/`. Proposed future folders: `Assets/`, `Packages/`, `ProjectSettings/`, `UserSettings/`.

Do not manually create or populate `Packages/` and `ProjectSettings/` before Unity creates the project. The future `Assets/` organization (scripts, scenes, data, vehicle/environment bundles) belongs in `docs/TDD.md` and is decided after technical discovery — it must not be invented now.

Location and deferral clarifications (Task 0.1, 2026-11-05):

- The Unity project is created **at the repository root** as specified above — `Assets/`, `Packages/`, `ProjectSettings/`, `UserSettings/` appear at `<repository root>/` alongside `docs/`, `design/`, `ai/`, `production/`. This does not change.
- The **test folder location and assembly-definition conventions** (e.g. `Assets/Tests/` vs an assembly-level arrangement) are decided during Roadmap Phase 3 (technical architecture planning) and recorded in `docs/TDD.md` then — do not invent them now.
- **Localization asset ownership and location** (where localized string tables live, who owns bilingual QA) is likewise finalized during Phase 3 — see `docs/TDD.md` §12 for the pre-approved localization rules that remain binding in the meantime.
- Until Phase 3, no `Assets/` internal architecture (module folders, ScriptableObject layout, scene naming) may be invented or scaffolded — this document only defines the root-level placement above.

## 3. What belongs where?

| Location | Put this here | Do not put here |
|---|---|---|
| `docs/` | GDD, TDD, platform rules, decisions, guides | Unity runtime assets |
| `design/` | Reference images, UI specs, concept art, art direction | Production scripts |
| `ai/` | Agent rules, reusable prompts, task templates | Secrets or credentials |
| `production/` | Roadmap, milestones, release planning | Source assets |
| `Assets/` | Imported runtime models, textures, audio, scenes, prefabs, scripts | Moodboards and planning docs |
| `Packages/` | Unity package manifest and lock file | Arbitrary assets |
| `ProjectSettings/` | Unity-generated project settings | Design references |

## 4. Reference images versus production assets
- Reference image: guides how something should look; store under `design/references/` or its feature folder.
- Production asset: imported into the game; store under `Assets/`.
- Generated concept image is not automatically a production asset.
- A screenshot of a desired UI is a reference; the actual Unity UI must be implemented and tested.

## 5. File naming
Use lowercase kebab-case for folders and descriptive filenames. Prefer stable names such as `garage-screen-reference-v01.png`. Avoid names like `final-final2.png`.

## 6. Large files
Use ordinary Git for Markdown and reasonably sized references. Evaluate Git LFS for large binary files such as FBX, large textures, and source art. Never commit Unity's generated `Library/`, `Temp/`, `Obj/`, or build output folders unless there is a documented exceptional reason.

## 7. Architecture boundary
This document does not define C# namespaces, classes, services, scenes, data models, or runtime dependencies. Those belong in `docs/TDD.md` after technical discovery and prototype decisions.
