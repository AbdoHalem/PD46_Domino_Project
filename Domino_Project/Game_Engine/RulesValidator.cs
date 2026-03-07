using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class RulesValidator
    {
        #region Tile Place
        // if player can play a tile based on the current left value on the board
        public bool CanPlayLeft(DominoTile tile, BoardState board)
        {
            if (board.LeftValue == -1) return true;
            return tile.HasValue(board.LeftValue);
        }

        // if player can play a tile based on the current right value on the board
        public bool CanPlayRight(DominoTile tile, BoardState board)
        {
            if (board.RightValue == -1) return true;
            return tile.HasValue(board.RightValue);
        }

        // if player can play a tile on either end of the board
        public bool CanPlayTile(DominoTile tile, BoardState board)
        {
            return CanPlayLeft(tile, board) || CanPlayRight(tile, board);
        }

        // validates a move, checks tile against the chosen side
        public bool IsValidMove(DominoTile tile, BoardState board, string side)
        {
            if (side.ToLower() == "left")
            {
                return CanPlayLeft(tile, board);
            }
            else if (side.ToLower() == "right")
            {
                return CanPlayRight(tile, board);
            }
            else
            {
                throw new ArgumentException("Side must be either 'left' or 'right'.");
            }
        }
        #endregion

        #region Player turn
        // if player has at least one valid tile based on the current board state
        public bool CanPlayerPlay(PlayerState player, BoardState board)
        {
            if (board.LeftValue == -1 && board.RightValue == -1) return true;
            return player.HasPlayableCard(board.LeftValue, board.RightValue);
        }

        // if player can draw a tile (if it's not empty)
        public bool CanDraw(Boneyard boneyard,BoardState board, PlayerState player)
        {
            return !boneyard.IsEmpty && !CanPlayerPlay(player, board);
        }

        // if player can pass (if they have no playable cards and the boneyard is empty)
        public bool CanPass(PlayerState player, BoardState board, Boneyard boneyard)
        {
            return !CanPlayerPlay(player, board) && boneyard.IsEmpty;
        }
        #endregion

        #region Round
        // round ends when any player has no tiles left
        public bool IsRoundOverByEmptyHand(List<PlayerState> players)
        {
            return players.Any(p => p.Cards.Count == 0);
        }

        // round ends when the boneyard is empty and no player can place a tile
        public bool IsBlockedRound(List<PlayerState> players, BoardState board, Boneyard boneyard)
        {
            if (!boneyard.IsEmpty) return false;
            return players.All(p => !CanPlayerPlay(p, board));
        }
        #endregion

        #region Game End
        // Game ends when any player's total score reach or exceed the score limit
        public bool IsGameOver(List<PlayerState> players, int scoreLimit)
        {
            return players.Any(p => p.TotalScore >= scoreLimit);
        }

        // the winner is the player with the lowest total score
        public PlayerState GetWinner(List<PlayerState> players)
        {
            return players.OrderBy(p => p.TotalScore).First();
        } 
        #endregion
    }
}
