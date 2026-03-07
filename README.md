# 🁣 Domino — Real-Time Multiplayer Game

A fully server-authoritative, real-time multiplayer Domino game built on a custom TCP socket infrastructure. Engineered in **C# / .NET 8.0** with a WinForms client, featuring a purpose-built game engine, dynamic lobby system, spectator mode, and resilient disconnect handling.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8.0 / C# |
| Networking | Custom TCP (`TcpListener`, length-prefixed framing) |
| Serialization | JSON over raw sockets |
| Client UI | WinForms (MVC-like pattern) |
| Game Logic | Network-agnostic `GameEngine` library |

---

## Architecture Overview

The solution is divided into four focused projects:

```
Domino.sln
├── Client_UI        → WinForms frontend + GameController mediator
├── Server_Side      → TCP server, message routing, state managers
├── Game_Engine      → Pure game logic: rules, board state, round lifecycle
└── Domino.Shared    → Shared message contracts (DTOs, enums)
```

### Network Layer
The server uses a raw `TcpListener` with **length-prefixed message framing**, solving TCP stream fragmentation without a third-party library. Incoming byte streams are decoded into JSON, then dispatched through a `MessageRouter` that uses **reflection-based `[MessageRoute]` attributes** to map action strings to handler methods — eliminating fragile switch statements.

### State Management
Three thread-safe managers govern all server-side state:

- **`ConnectionRegistry`** — Tracks active TCP socket sessions by player identity.
- **`GroupManager`** — A Pub/Sub broadcast system for rooms and the global lobby.
- **`GameManager`** — Owns room lifecycles, player registries, and the 1-to-1 mapping of rooms to isolated `GameEngine` instances.

### Game Engine
`GameEngine` is deliberately **network-agnostic** — it has no knowledge of sockets, players, or clients. Every mutation (play, draw, pass) passes through `RulesValidator` before any state change is applied, guaranteeing that cheating or out-of-order messages cannot corrupt game state.

### Client Architecture
The WinForms client follows a **Controller-View separation**: `GameController` acts as the sole mediator between raw TCP events and the UI. It maintains a local state mirror, translates server events into structured model updates, and pushes pure render calls to `GameForm`.

---

## Features

- **Real-Time Multiplayer** — 2 to 4 players per game room, with sub-second state propagation.
- **Dynamic Lobby** — Live room discovery with paginated UI (6 rooms per page) and continuous sync.
- **Spectator Mode** — Join any active or full game as a read-only observer. Spectators receive a full board snapshot on join and stream all subsequent updates.
- **Server-Authoritative Validation** — Every `PlayDomino`, `DrawTile`, and `Pass` action is validated server-side for turn order and tile possession before broadcast.
- **Robust Disconnect Handling:**
  - *Host Migration* — If the host leaves a waiting room, ownership transfers to the next player automatically.
  - *Mid-Game Restart* — If a player disconnects during an active game, their score is preserved, the board resets, and the round restarts for remaining players.
  - *Auto-Cleanup* — Rooms are destroyed when all players leave or only one player remains in an active game.

---

## Core Flows

### Standard Game Flow

```
Login → Lobby Snapshot → Create/Join Room → Waiting Room
  → Host Starts Game → Tiles Dealt → Gameplay Loop
  → Round End (score popup) → [Repeat or Game Over] → Return to Lobby
```

1. **Login** — Client connects via TCP and sends `LoginRequest`. Server responds with `EventLoginOk` + `EventLobbySnapshot`.
2. **Room** — Player sends `CreateRoom` or `JoinRoom`. Server assigns them to a group and broadcasts `RoomSnapshot`.
3. **Start** — Host sends `ActionReadyUp` (requires ≥2 players). Server initializes the engine, deals tiles, and emits `EventTileDealt`.
4. **Gameplay** — Players send `PlayDomino`, `DrawTile`, or `Pass`. Each is validated, applied, and broadcast.
5. **Round End** — Empty hand or blocked board triggers score calculation and `EventRoundEnded`.
6. **Game Over** — First player to reach the score limit receives `EventGameOver`.

### Spectator Flow

1. Player selects an active room and sends `WatchRoom`.
2. Server responds with `EventWatcherJoined` containing full current board state.
3. `GameController` initializes in `_isWatcher = true` mode — board renders, all inputs are disabled.

### Disconnect / Leave Flow

| Scenario | Outcome |
|---|---|
| Leave waiting room (was host) | Host title migrates to next player |
| Leave waiting room (not host) | Player removed, room snapshot re-broadcast |
| Leave active game (2+ players remain) | Score preserved, `StartNewRound()` triggered |
| Leave active game (1 player remains) | Room aborted, remaining player shown "All others left" |

---

## Project Reference

### `Client_UI`

| Class | Responsibility |
|---|---|
| `LobbyForm` | Lobby rendering, pagination, room navigation |
| `GameController` | TCP event handling, local state mirroring, UI dispatch |
| `GameForm` | Pure rendering of board, tiles, scores |

**Key methods:**
- `GameController.HandleDominoPlayed()` — Updates board mirror, flips tiles for edge placement, decrements opponent hand counts.
- `GameController.HandleRoundEnded()` — Triggers score popup, clears board state.
- `GameController.OnFormClosing()` — Intercepts window close to flush `ActionLeaveRoom` before socket teardown.

### `Server_Side`

| Class | Responsibility |
|---|---|
| `TcpCommServer` | Connection acceptance, framed byte I/O, socket cleanup |
| `MessageRouter` | Reflection-based JSON-to-handler dispatch |
| `GameManager` | Room lifecycle, player registry, engine mapping |
| `GroupManager` | Pub/Sub channels, parallel group broadcast |
| `RoomHandlers` | `CreateRoom`, `JoinRoom`, `LeaveRoom` |
| `MatchHandlers` | `PlayDomino`, `DrawTile`, `Pass`, `ReadyUp` |
| `LobbyHandlers` | `Login`, `JoinLobby`, `WatchRoom` |

**Key methods:**
- `TcpCommServer.HandlePlayerCleanupAsync()` — Catches dropped sockets, injects a synthetic `LeaveRoom` to maintain consistent game state.
- `GameManager.RemovePlayer()` — Removes a player and triggers host migration when the departing player held the `OwnerId`.
- `HandleLeaveRoomAsync()` — Central teardown logic; branches across all three disconnect scenarios.

### `Game_Engine`

| Class | Responsibility |
|---|---|
| `GameEngine` | Round lifecycle, tile distribution, turn management |
| `RulesValidator` | Move legality, draw eligibility, block detection |
| `BoardState` | Live board representation, edge values |

**Key methods:**
- `GameEngine.StartNewRound()` — Initializes Boneyard, wipes board, deals tiles, determines first player.
- `GameEngine.PlayTileLeft/Right()` — Full validation pipeline → hand removal → edge update → end-of-round check.
- `GameEngine.RemovePlayerKeepScore()` — Extracts disconnected player, recalibrates `CurrentPlayerIndex`, preserves cumulative score.
- `RulesValidator.IsBlockedRound()` — Returns `true` only when the Boneyard is empty and no player has a legal move.

---

## Getting Started

```bash
# Clone the repository
git clone https://github.com/your-org/domino-multiplayer.git

# Build the solution
dotnet build Domino.sln

# Start the server
dotnet run --project Server_Side

# Launch a client (repeat for multiple players)
dotnet run --project Client_UI
```

> The server binds to a configurable TCP port (default: see `appsettings.json`). All clients must point to the server's host and port before connecting.

---

## Team
***- Abdelrahim Ahmed : https://github.com/Speedmob***

***- Mohamed Ali : https://github.com/mohdali300***

***- Abdelrahman Abdelhalem : https://github.com/AbdoHalem***

***- Ahmed Ebrahim : https://github.com/AhmedNabilko***

***- Abdelrahman Saleem : https://github.com/AbdelrhmanSaleem***

---
## License

This project for educational purpose.
