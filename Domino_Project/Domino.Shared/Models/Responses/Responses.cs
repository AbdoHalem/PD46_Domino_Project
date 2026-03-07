using System.Collections.Generic;

namespace Domino.Shared.Models.Responses
{
    public class LoginOkResponse
    {
        public string ConnectionId { get; set; }
        public string PlayerName   { get; set; }
    }

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
        public string        FirstTurn    { get; set; }
    }

    public class WatcherJoinedResponse
    {
        public string        RoomName     { get; set; }
        public List<TileDto> Board        { get; set; }
        public string        CurrentTurn  { get; set; }
        public List<PlayerScoreDto> Scores { get; set; }
    }

    public class DominoPlayedResponse
    {
        public string  PlayerId   { get; set; }
        public string  PlayerName { get; set; }
        public TileDto Tile       { get; set; }
        public string  Edge       { get; set; }
        public string  NextTurn   { get; set; }
        public int     HandCount  { get; set; }
    }

    public class TileDrawnResponse
    {
        public TileDto DrawnTile     { get; set; }
        public int     BoneyardCount { get; set; }
    }

    public class DrawBroadcast
    {
        public string PlayerName  { get; set; }
        public int    BoneyardCount { get; set; }
    }

    public class PlayerPassedResponse
    {
        public string PlayerName { get; set; }
        public string NextTurn   { get; set; }
    }

    public class PlayerScoreDto
    {
        public string PlayerName { get; set; }
        public int    RoundPoints { get; set; }
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

    public class PlayerLeftResponse
    {
        public string PlayerName { get; set; }
        public string Message    { get; set; }
    }
}
