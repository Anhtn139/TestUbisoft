# TestUbisoft

Unity prototype for an egg-collection match with an in-process client/server simulation, snapshot-based presentation, and grid pathfinding for bot behavior.

## Unity Version

- `2022.3.62f3`

## Current Architecture

The project is already split into layers so the prototype can run locally today and still evolve toward a real networked architecture later.

- `Assets/_Project/Scripts/Runtime/Core`
  Transport-safe data contracts such as config, entity snapshots, score data, and simulation-friendly math types.
- `Assets/_Project/Scripts/Runtime/Messaging`
  Message contracts and transport abstractions. `SimulatedMessageTransport` currently acts as the in-memory transport layer.
- `Assets/_Project/Scripts/Runtime/Server`
  Authoritative simulation code. Server-side state and game rules belong here.
- `Assets/_Project/Scripts/Runtime/Client`
  Client orchestration that sends input and consumes authoritative snapshots.
- `Assets/_Project/Scripts/Runtime/Presentation`
  Unity-facing rendering and scene adapters.
- `Assets/_Project/Scripts/Runtime/Bootstrap`
  Composition root. `GameBootstrapper` wires config, transport, server simulator, client game, and world view together.
- `Assets/Scripts/Pathfinding/Runtime`
  Grid map and A* pathfinding implementation, with edit-mode tests in `Assets/Tests/EditMode/Pathfinding`.
- `Assets/Scripts/Server/AI`
  Bot targeting and movement adapters, including lowest-path-cost egg selection.

## How To Run

1. Open the project in Unity Hub with editor `2022.3.62f3`.
2. Load `Assets/Scenes/SampleScene.unity`.
3. Enter Play Mode.
4. Ensure the scene has a configured `GameBootstrapper`, `UnityClientWorldView`, and pathfinding grid component if you want bot navigation and snapshot rendering active.

## Tests

- Edit-mode coverage currently exists for A* pathfinding in `Assets/Tests/EditMode/Pathfinding/AStarPathfinderTests.cs`.
- Run them from Unity Test Runner using Edit Mode tests.

## Intended Approach

The intended approach is to keep the server simulation authoritative and keep Unity scene logic thin.

- Treat the simulation as the source of truth for movement, egg collection, scoring, and match timing.
- Keep client responsibilities limited to collecting local input, forwarding intent through the transport boundary, and rendering snapshots from the server.
- Continue using plain C# classes for simulation-heavy logic so gameplay code stays testable outside of MonoBehaviour lifecycles.
- Preserve the transport abstraction so the current in-memory simulation can later be replaced with a real backend or networking layer without rewriting gameplay rules.
- Build bot decision-making on top of the grid/A* foundation, with path cost driving target selection instead of scene-driven heuristics.
- Expand automated tests first around pathfinding, bot target selection, and deterministic simulation rules before adding more presentation complexity.

## Notes

- There is already an internal architecture note at `Assets/_Project/README.md`.
- The root `README.md` is intended to be the quick entry point for setup, orientation, and implementation direction.
