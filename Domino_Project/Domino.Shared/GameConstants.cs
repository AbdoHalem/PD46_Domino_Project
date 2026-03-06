// =====================================================================
//  FILE: GameConstants.cs  (Domino.Shared project)
//  All action strings used by both the Server and Client must be
//  defined here so both sides share the exact same contract.
// =====================================================================
namespace Domino.Shared
{
    public static class GameConstants
    {
        // ── Server port ───────────────────────────────────────────────
        public const int ServerPort = 5500;

        // ── Lobby ─────────────────────────────────────────────────────
        public const string ActionLogin         = "Login";
        public const string ActionJoinLobby     = "JoinLobby";

        // ── Room management ───────────────────────────────────────────
        public const string ActionCreateRoom    = "CreateRoom";
        public const string ActionJoinRoom      = "JoinRoom";
        public const string ActionWatchRoom     = "WatchRoom";
        public const string ActionLeaveRoom     = "LeaveRoom";
        public const string ActionReadyUp       = "ReadyUp";

        // ── Gameplay ──────────────────────────────────────────────────
        public const string ActionPlayDomino    = "PlayDomino";
        public const string ActionDrawTile      = "DrawTile";
        public const string ActionPass          = "Pass";

        // ── Server→Client broadcasts ──────────────────────────────────
        public const string EventLobbySnapshot  = "LobbySnapshot";
        public const string EventRoomSnapshot   = "RoomSnapshot";
        public const string EventGameStarted    = "GameStarted";
        public const string EventWatcherJoined = "WatcherJoined";
        public const string EventTileDealt      = "TileDealt";        // only to that player
        public const string EventTurnChanged    = "TurnChanged";
        public const string EventDominoPlayed   = "DominoPlayed";
        public const string EventTileDrawn      = "TileDrawn";        // only to that player
        public const string EventPlayerPassed   = "PlayerPassed";
        public const string EventRoundEnded     = "RoundEnded";
        public const string EventGameOver       = "GameOver";
        public const string EventPlayerLeft     = "PlayerLeft";
        public const string EventError          = "Error";
        public const string EventLoginOk        = "LoginOk";

        // ── Config ────────────────────────────────────────────────────
        public const int MaxPlayersPerRoom = 4;
        public const int MinPlayersToStart = 2;
    }
}
