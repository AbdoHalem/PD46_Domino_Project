// =====================================================================
//  FILE: Responses.cs  (Domino.Shared/Models/Responses)
//  All S→C response / broadcast payloads.
// =====================================================================
using System.Collections.Generic;

namespace Domino.Shared.Models.Responses
{
    // ── Login ─────────────────────────────────────────────────────────
    public class LoginOkResponse
    {
        public string ConnectionId { get; set; }
        public string PlayerName   { get; set; }
    }

    // ── Lobby ─────────────────────────────────────────────────────────
    public class RoomSummary
    {
        public string RoomId        { get; set; }
        public string RoomName      { get; set; }
        public int    MaxPlayers    { get; set; }
        public int    CurrentCount  { get; set; }
        public bool   GameRunning   { get; set; }
    }

    public class LobbySnapshotResponse
    {
        public List<RoomSummary> Rooms { get; set; }
    }

    // ── Room ──────────────────────────────────────────────────────────
    public class RoomStateResponse
    {
        public string       RoomId           { get; set; }
        public string       RoomName         { get; set; }
        public int          MaxPlayers       { get; set; }
        public int          ScoreLimit       { get; set; }
        public List<string> ConnectedPlayers { get; set; }
        public bool         IsGameStarting   { get; set; }
        public string OwnerName { get; set; }

    }

    // ── Game start: sent to every player in the room ──────────────────
    public class TileDto
    {
        public int Left  { get; set; }
        public int Right { get; set; }
    }

    public class PlayerHandResponse
    {
        public string        PlayerName   { get; set; }
        public List<TileDto> Hand         { get; set; }
        public int           BoneyardCount { get; set; }
        public string        FirstTurn    { get; set; }  // name of player who goes first
    }

    // ── Sent to spectators – no hand, just board state ────────────────
    public class WatcherJoinedResponse
    {
        public string        RoomName     { get; set; }
        public List<TileDto> Board        { get; set; }
        public string        CurrentTurn  { get; set; }
        public List<PlayerScoreDto> Scores { get; set; }
    }

    // ── Per-move broadcast ────────────────────────────────────────────
    public class DominoPlayedResponse
    {
        public string  PlayerId   { get; set; }
        public string  PlayerName { get; set; }
        public TileDto Tile       { get; set; }
        public string  Edge       { get; set; }        // "Left" or "Right"
        public string  NextTurn   { get; set; }        // name of next player
        public int     HandCount  { get; set; }        // tiles left in that player's hand
    }

    // ── Draw ──────────────────────────────────────────────────────────
    public class TileDrawnResponse   // private – only to the drawing player
    {
        public TileDto DrawnTile     { get; set; }
        public int     BoneyardCount { get; set; }
    }

    public class DrawBroadcast       // public – so opponents see hand count go up
    {
        public string PlayerName  { get; set; }
        public int    BoneyardCount { get; set; }
    }

    // ── Pass ──────────────────────────────────────────────────────────
    public class PlayerPassedResponse
    {
        public string PlayerName { get; set; }
        public string NextTurn   { get; set; }
    }

    // ── Round / Game end ─────────────────────────────────────────────
    public class PlayerScoreDto
    {
        public string PlayerName { get; set; }
        public int    RoundPoints { get; set; }  // points added this round
        public int    TotalScore  { get; set; }
    }

    public class RoundEndedResponse
    {
        public string                RoundWinner { get; set; }
        public List<PlayerScoreDto>  Scores      { get; set; }
        public bool                  GameOver    { get; set; }
    }

    public class GameOverResponse
    {
        public string                GameWinner  { get; set; }
        public List<PlayerScoreDto>  FinalScores { get; set; }
    }

    // ── Player disconnected mid-game ──────────────────────────────────
    public class PlayerLeftResponse
    {
        public string PlayerName { get; set; }
        public string Message    { get; set; }
    }
}
