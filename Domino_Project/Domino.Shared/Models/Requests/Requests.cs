// =====================================================================
//  FILE: Requests.cs  (Domino.Shared/Models/Requests)
//  All C→S request payloads. Keep properties nullable so JSON
//  deserialization with PropertyNameCaseInsensitive is forgiving.
// =====================================================================
namespace Domino.Shared.Models.Requests
{
    public class LoginRequest
    {
        public string PlayerName { get; set; }
    }

    public class CreateRoomRequest
    {
        public string RoomName    { get; set; }
        public int    MaxPlayers  { get; set; }   // 2, 3, or 4
        public int    ScoreLimit  { get; set; }   // game ends when any player reaches this
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
        public string TargetEdge  { get; set; }   // "Left" or "Right"
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
