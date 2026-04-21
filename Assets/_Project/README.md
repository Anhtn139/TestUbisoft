# Prototype Architecture

This project is organized so the prototype can run as an in-process Unity game today and later swap the local simulator for a real server.

## Folder Structure

- `Scripts/Runtime/Core`
  - Shared config, math, entity snapshots, and other transport-safe data contracts.
  - These classes should not depend on scene objects or `MonoBehaviour`.
- `Scripts/Runtime/Messaging`
  - Client/server message contracts and the transport interface.
  - `SimulatedMessageTransport` is an in-memory prototype implementation.
- `Scripts/Runtime/Server`
  - Authoritative gameplay simulation.
  - Server state changes belong here, not in views or bootstrappers.
- `Scripts/Runtime/Client`
  - Client-side orchestration.
  - Sends local intent to the server boundary and applies authoritative snapshots to a view boundary.
- `Scripts/Runtime/Presentation`
  - Unity adapters and visual-only MonoBehaviours.
  - These classes create, update, and destroy GameObjects from snapshots.
- `Scripts/Runtime/Bootstrap`
  - Composition root.
  - `GameBootstrapper` creates `GameConfig`, `ServerSimulator`, `SimulatedMessageTransport`, and `ClientGame`.

## Responsibility Boundaries

- Server simulation is authoritative. It owns gameplay state and decides where entities are after each tick.
- Client code sends intent and renders snapshots. It should not decide authoritative gameplay outcomes.
- Transport is an interface. The in-memory queue can later be replaced by sockets, relay, Netcode, or a backend client.
- MonoBehaviours should stay thin. They bootstrap services, read Unity input, and render GameObjects.
