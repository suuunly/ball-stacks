# Ball Stacks — CLAUDE.md

Unity 6 (6000.6.0f1) local-multiplayer physics party game. URP (PC + Mobile render pipelines), new Input System (`InputSystem_Actions.inputactions`).

## The Game

**Ball Stacks**: stack balls higher than your opponents, balance your wobbly physics-driven tower, sabotage rivals by bumping into their stacks, and be the tallest when the timer runs out. 2–4 players, one arena, spheres only.

Hanna's concept fragment is the north star: **"Ball Stacks — GameJam Concept"** (fragment `6bc9cdf0-dded-4e92-968d-b02deb3019ce`). Important: that document is the **base concept, not the full game**. We flesh it out as we go. The humans know more about the game than you do — when a design question or ambiguity comes up, **ask instead of inventing an answer**, and capture the resulting feature/task as a fragment in Usable so we can work it.

## Knowledge Base — Gaman Games workspace (Usable)

All project knowledge lives in the **Gaman Games** workspace: `d96cca7c-bfe2-455a-b039-ad733415545a`. Use it as the source of truth:

- **Before writing code**, search the workspace for relevant Coding Standards, Recipes, and Solutions and follow them. Fetch full fragment content before relying on it.
- **When wrapping up** (`/wrap-up`, handovers, end of session), capture durable learnings back into this workspace as fragments — Solutions for problems we fixed, Recipes for reusable patterns we built, Features/Tasks for scoped work we discovered.
- **New features/questions** we decide on get captured as fragments in this workspace before or while we build them.

### Must-read fragments

Coding standards (all three layers are mandatory, plus structure):

| Fragment | ID |
|---|---|
| Coding Standard — General Purpose | `901588d3-94db-47ac-a8c4-283dc8a125a4` |
| Coding Standard — C# Conventions | `571f7d06-b696-4621-9f8b-1139e5193d00` |
| Coding Standard — Unity Specific | `053ad510-d02a-46db-9974-302edcd93a2c` |
| Coding Standard — Unity Asset & Project Structure | `150a66ee-d0ae-4988-b731-085ccb3804bc` |

Core architecture recipes (implement these patterns, don't improvise your own):

| Recipe | ID |
|---|---|
| ScriptableObject Event System (GameEvent & GameEventPure) | `9c86b39d-f70a-4330-86b4-39a229e45e3d` |
| Runtime Set (ScriptableObject Registry Pattern) | `eca18aea-c67e-402c-9e8c-62471b9e1d18` |
| Game Pool (ScriptableObject Object Pool) | `eb0c38f0-e34b-4df0-bb36-6d9250a8edcb` |
| ScriptableEnum (Asset-Based Enum) | `8dded499-7572-4daf-98ab-a5ceca27733e` |
| Audio Pool (ScriptableObject Audio Source Pool) | `12b2cf40-9f80-4753-9997-f3897af118ac` |
| Playlist SO (Abstract ScriptableObject Playlist) | `ad4d306e-5222-4c6b-89db-19112c190950` |

There is also a **review-unity** skill fragment (`40c44e76-c528-49c8-b777-7d4c6747b992`) for reviewing code against the standards.

## How We Program

1. **Event-driven when possible.** Direct references are allowed, but prefer decoupling systems through GameEvent ScriptableObjects — especially across feature boundaries. Use `GameEvent` (UnityEvent-based) when the designer should wire responses in the Inspector; use `GameEventPure` (C# Action-based) for code-to-code subscriptions. Follow the recipe.
2. **Runtime Sets for discovery.** No singletons, no `FindObjectOfType`, no static managers. When a system needs to find runtime objects it doesn't already hold a natural reference to (players, stacks, balls), it goes through a RuntimeSet asset. Follow the recipe.
3. **ScriptableObjects everywhere it makes sense.** Shared config, tunable game data (timer length, stack physics values, ball definitions), events, sets, enums — all as SO assets so they're designer-editable without code changes.
4. **Standards-aware, always.** Code must comply with all three coding-standard layers plus the asset/project structure standard. When in doubt, re-read the fragment — don't guess.
5. **Self-review before calling it done.** After writing or changing scripts, evaluate the code against the standards (the review-unity skill fragment describes how). Clean code is part of "done", not a follow-up.
6. **Keep jam scope in mind.** Balls only, one arena, one mode. Prefer the simplest compliant implementation; lean into physics chaos rather than fighting it.

## Working with Unity

Interact with the Unity Editor through the **Unity CLI** (`unity`, installed at `~/.unity/bin/unity`) — don't guess at editor state or ask the humans to relay it:

- `unity status` — live state of connected editors (project, state, PID).
- `unity pipeline` — editor automation; after script changes, trigger a recompile and check the console log for errors before calling the work done.
- `unity test` — run EditMode/PlayMode tests with a results report.
- `unity list` / `unity command` — discover and execute commands registered on the connected editor.
- `unity open` / `unity run` / `unity build` — open, run in batch mode, or build the project.

The humans share the editor and may be playtesting — if the editor is in Play mode, stop or wait before recompiling or running EditMode tests.

## Division of Labour

- **The humans own the editor creativity**: scene composition, placing objects, animations, materials, visual tweaking. Leave them room — don't rearrange scenes or redo visual work uninvited.
- **Claude owns the scripts**: writing C# code, creating the SO assets that back it (events, runtime sets, config), and hooking components up so the systems function. Expose the knobs (serialized fields, SO config assets, Inspector-wireable GameEvents) so the humans can tune and wire creatively without touching code.

## Workflow

- Unknown design question mid-task → ask the user, then capture the decision/feature as a Usable fragment in Gaman Games.
- New reusable pattern or non-obvious fix discovered → capture as Recipe/Solution fragment at wrap-up.
- Keep `Assets/Game/` as the home for game content (feature-based folders per the structure standard); `Assets/TutorialInfo/` is Unity template noise and can be ignored.

## Usable Tasks

- WorkspaceId: `d96cca7c-bfe2-455a-b039-ad733415545a` (Gaman Games)
- TaskFragmentTypeId: `7311ceb1-ac4f-4426-b55b-5155f9b54072` (Task)
- FeatureFragmentTypeId: `316e2a1d-a0bd-43f0-a798-7c9485a86c41` (Feature)
- Project: `ball-stacks` (repo tag: `Ball-Stacks`)

Paper-trail convention: scoped work gets a Feature fragment plus child Task fragments (My Tasks Planner format); Tasks reference their parent Feature by ID, and the Feature lists its child tasks.
