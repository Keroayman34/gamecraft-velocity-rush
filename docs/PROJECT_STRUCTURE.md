# Project Structure Guide

This file explains where project information and assets belong. It is a repository organization guide, not the game's technical architecture.

## 1. Pre-Unity repository structure

```text
GameCraft-Velocity-Rush/
├── README.md
├── CONTRIBUTING.md
├── CHANGELOG.md
├── docs/
│   ├── PROJECT_CHARTER.md
│   ├── AI_AGENT_GUIDELINES.md
│   ├── PROJECT_STRUCTURE.md
│   ├── DECISIONS.md
│   ├── OPEN_QUESTIONS.md
│   ├── PLAYGAMA_CONSTRAINTS.md
│   ├── GDD.md
│   └── TDD.md
├── design/
│   ├── ART_DIRECTION.md
│   ├── brand/
│   ├── ui-ux/
│   ├── vehicles/
│   ├── environments/
│   ├── gameplay/
│   └── references/
├── ai/
│   ├── MASTER_AGENT_PROMPT.md
│   ├── WORKFLOW.md
│   └── prompts/
└── production/
    ├── ROADMAP.md
    └── MILESTONES.md
```

## 2. After Unity project creation

Unity will add its project folders. The intended high-level arrangement is:

```text
GameCraft-Velocity-Rush/
├── Assets/
├── Packages/
├── ProjectSettings/
├── UserSettings/          # local/editor-specific; review ignore rules
├── docs/
├── design/
├── ai/
└── production/
```

Do not manually create or populate `Packages/` and `ProjectSettings/` before Unity creates the project.

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
