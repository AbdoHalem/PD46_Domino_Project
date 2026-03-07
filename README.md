Domino Multiplayer Game

A real-time, multiplayer Domino game built with C# .NET 8.0, featuring a custom TCP socket server, WinForms client, and a server-authoritative game engine.
🏗 Architecture

The project follows a robust Client-Server architecture separated into four main projects: Client_UI, Server_Side, Game_Engine, and Domino.Shared.

    Network Layer (Custom TCP): The server uses a low-level TcpListener with length-prefixed framing to handle real-time socket connections, solving TCP fragmentation natively.

    Routing Layer: Incoming JSON messages are dynamically mapped to specific handler methods using a custom MessageRouter and [MessageRoute] reflection attributes.

    State Management: The server maintains state across three main managers:

        ConnectionRegistry: Tracks active TCP sockets.

        GroupManager: A Pub/Sub system for broadcasting messages to specific rooms or the global lobby.

        GameManager: Manages room lifecycles, player registries, and maps rooms to their isolated GameEngine instances.

    Game Engine: A purely logical, network-agnostic engine that strictly validates all rules (turn order, tile possession, valid placements) before applying state changes.

    Client UI (WinForms): Uses an MVC-like approach where GameController acts as the mediator. It listens to raw TCP events, updates local state mirrors, and pushes UI updates to the pure-rendering GameForm.

✨ Available Features

    Real-time Multiplayer: Supports 2 to 4 players per room.

    Dynamic Lobby System: Features live room tracking and UI pagination (6 rooms per page).

    Spectator Mode: Users can join full or active games as watchers to view the board state live without participating.

    Server-Authoritative Validation: The server verifies every move, draw, and pass to prevent cheating.

    Robust Disconnect Handling: * Host Migration: If the host leaves a waiting room, the host title transfers to the next player automatically.

        Mid-Game Restarts: If a player disconnects during a game, their score is preserved, the board is wiped, and the round seamlessly restarts for the remaining players.

        Auto-Cleanup: Rooms are automatically destroyed if all players leave or if only 1 player remains during an active game.

🔄 Use Case Flows
1. Standard Game Flow (Login to Game Over)

    Login: The client connects via TCP and sends a LoginRequest with their name.

    Lobby: The server responds with EventLoginOk and an EventLobbySnapshot containing active rooms.

    Room Creation/Join: The player sends CreateRoom or JoinRoom. The server assigns them to a group and broadcasts a RoomSnapshot to everyone in the room.

    Waiting Room: Players wait. The "Start Game" button is only enabled for the Host when ≥2 players join.

    Start Game: The Host clicks start, sending an ActionReadyUp. The server initializes the GameEngine, deals tiles, and broadcasts EventTileDealt.

    Gameplay Loop: * Players send PlayDomino, DrawTile, or Pass.

        The server validates the action via RulesValidator.

        The server broadcasts EventDominoPlayed, EventTileDrawn, or EventPlayerPassed to update client UIs.

    Round End: A player empties their hand or the board is blocked. The server calculates scores and sends EventRoundEnded. The UI shows a popup.

    Game Over: If a player reaches the score limit, EventGameOver is sent. Players click to return, sending ActionJoinLobby.

2. Spectator Flow

    Watch Room: A player selects an active/full room and clicks "Watch", sending WatchRoom.

    Sync State: The server sends EventWatcherJoined containing the current board state.

    Read-Only UI: The GameController initializes in spectator mode (_isWatcher = true), rendering the board but disabling interactive inputs.

3. Disconnection / Leave Flow

    Trigger: A player clicks "Leave Room" or forcefully closes the app (triggering a synthetic leave via TcpCommServer.HandlePlayerCleanupAsync).

    Evaluation: HandleLeaveRoomAsync assesses the room state.

    Outcome A (Waiting Room): If the room hasn't started, the player is removed. If they were the host, GameManager.RemovePlayer migrates the host title.

    Outcome B (Active Game, 2+ Players): The player is removed via RemovePlayerKeepScore. The engine triggers StartNewRound() and deals fresh hands to the remaining players.

    Outcome C (Active Game, 1 Player Left): The game aborts. The UI shows "All other players left", and GameManager.RemoveRoom destroys the room.

📖 Method Summaries
Client_UI

LobbyForm.cs

    BuildUI(): Constructs the WinForms components (buttons, panels, lists) programmatically.

    OnMessageReceived(): The main thread-safe event listener for routing server JSON payloads to lobby UI updates.

    RenderPage() / LoadRooms(): Handles the logic for displaying 6 room cards per page using pagination.

    ShowWaitingRoom(): Swaps the UI from the lobby view to the specific room's player list and evaluates host permissions.

    OpenGameForm() / OpenSpectatorForm(): Hides the lobby and boots up the GameController for playing or watching.

GameController.cs

    OnMessageReceived(): Translates server events (DominoPlayed, TileDrawn, RoundEnded) into local state updates.

    HandleDominoPlayed(): Updates the local board mirror, flips tiles if necessary based on edge placement, and decrements opponent card counts.

    HandleRoundEnded() / HandleNewRoundDealt(): Updates local scores, triggers UI popups, and clears the board when fresh tiles arrive.

    OnTilePlaced() / OnLeaveGame(): Translates user UI clicks into outgoing network requests (ActionPlayDomino, ActionLeaveRoom).

    OnFormClosing(): Intercepts the window 'X' button to ensure the ActionLeaveRoom packet is flushed before the app dies.

Server_Side

TcpCommServer.cs & MessageRouter.cs

    Start() / AcceptClientsAsync(): Binds to the port and continuously accepts incoming TCP connections.

    HandleClientAsync(): Reads length-prefixed bytes, decodes JSON strings, and passes them to the MessageRouter.

    HandlePlayerCleanupAsync(): Catches dropped sockets, injects a synthetic LeaveRoom request to gracefully update game states, and destroys the socket.

    RouteMessageAsync(): Parses the Action string from JSON and invokes the matching [MessageRoute] handler.

GameManager.cs & GroupManager.cs

    CreateRoom() / RemoveRoom(): Allocates or completely destroys a RoomRecord and its associated GameEngine.

    RemovePlayer(): Removes a player from a room's registry and performs automatic Host Migration if the leaving player was the OwnerId.

    AddToGroup() / BroadcastToGroupAsync(): Manages pub/sub channels (e.g., "Lobby" or "Room_123") and parallel-sends TCP messages to all grouped sockets.

Server Handlers (RoomHandlers, MatchHandlers, LobbyHandlers)

    HandleLoginAsync(): Registers the player's name and sends the initial lobby snapshot.

    HandleCreateRoomAsync() / HandleJoinRoomAsync(): Validates room capacity, adds players to the room group, and broadcasts updated room states.

    HandleLeaveRoomAsync(): Complex teardown logic that determines whether to migrate a host, restart a mid-game round, or abort an empty match.

    HandlePlayDominoAsync(): Validates turn order and tile possession, executes the move on the engine, and broadcasts the result.

Game_Engine

GameEngine.cs

    StartNewRound(): Initializes the Boneyard, wipes the BoardState, distributes tiles to players, and calculates who goes first.

    PlayTileLeft() / PlayTileRight(): Validates rules, removes the tile from the player's hand, updates the board edges, and triggers EndRound if the hand is empty.

    RemovePlayerKeepScore(): Safely extracts a disconnected player from the Players array and recalibrates the CurrentPlayerIndex while keeping scores intact.

    AdvanceTurn(): Shifts the active player index and checks if the round is deadlocked (blocked).

RulesValidator.cs & BoardState.cs

    IsValidMove() / CanPlayTile(): Checks if a domino's pips match the current LeftValue or RightValue of the board.

    IsBlockedRound(): Determines if the boneyard is empty and absolutely no player has a valid move available.
