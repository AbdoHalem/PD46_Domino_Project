namespace Domino.Shared.Models.Requests
{
    public class LoginRequest
    {
        public string PlayerName { get; set; }
    }

    public class CreateRoomRequest
    {
        public string RoomName    { get; set; }
        public int    MaxPlayers  { get; set; }
        public int    ScoreLimit  { get; set; }
    }

    public class JoinRoomRequest
    {
        public string RoomId { get; set; }
    }

    public class WatchRoomRequest
    {
        public string RoomId { get; set; }
    }

    public class PlayDominoRequest
    {
        public string GameId      { get; set; }
        public int    TileValue1  { get; set; }
        public int    TileValue2  { get; set; }
        public string TargetEdge  { get; set; }
    }

    public class DrawTileRequest
    {
        public string GameId { get; set; }
    }

    public class PassRequest
    {
        public string GameId { get; set; }
    }
}
