using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class GameEngine
    {
        RulesValidator _rulesValidator;

        public BoardState Board { get; private set; }
        public List<PlayerState> Players { get; private set; }
        public Boneyard Boneyard { get; private set; }
        public string RoomName { get; private set; }
        public int ScoreLimit { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public bool IsGameOver { get; private set; }
        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];

        #region delegates & events
        // fired when a new player turn
        public delegate void TurnChangedEventHandler(string playerName);
        public event TurnChangedEventHandler OnTurnChanged;

        // fired when a round ends
        public delegate void RoundEndedEventHandler(string winnerName);
        public event RoundEndedEventHandler OnRoundEnded;

        // fired when the whole game is over
        public delegate void GameOverEventHandler(string winnerName);
        public event GameOverEventHandler OnGameOver; 
        #endregion

        public GameEngine(Room room)
        {
            if(room == null) throw new ArgumentNullException("Room can’t be null.");
            if(!room.IsReady()) throw new InvalidOperationException("Room is not ready. At least 2 players are required to start the game.");

            RoomName = room.Name;
            ScoreLimit = room.ScoreLimit;
            _rulesValidator = new RulesValidator();
            Players = new List<PlayerState>();

            foreach (var playerName in room.PlayerNames)
            {
                Players.Add(new PlayerState(playerName));
            }

            StartNewRound();
        }

        // play a tile on the board for the current player, true if the move was valid and accepted, false otherwise
        public bool PlayCard(DominoTile tile, string side)
        {
            if (IsGameOver) return false;

            if (!_rulesValidator.IsValidMove(tile, Board, side))
                return false;

            CurrentPlayer.PlayCard(tile);

            if (side.ToLower() == "left")
                Board.PlayCardAtLeft(tile);
            else
                Board.PlayCardAtRight(tile);

            if (CurrentPlayer.Cards.Count == 0)
            {
                EndRound(CurrentPlayer);
                return true;
            }

            if (_rulesValidator.IsBlockedRound(Players, Board, Boneyard))
            {
                EndRound(null);
                return true;
            }

            AdvanceTurn();
            return true;
        }

        // Draws one tile from the boneyard and adds it to the current player's hand
        public DominoTile DrawFromBoneyard()
        {
            if (!_rulesValidator.CanDraw(Boneyard))
                throw new InvalidOperationException("Boneyard is empty, cannot draw.");

            DominoTile tile = Boneyard.DrawCard();
            CurrentPlayer.AddCard(tile);
            return tile;
        }

        // pass the current player's turn
        public void Pass()
        {
            if (!_rulesValidator.CanPass(CurrentPlayer, Board, Boneyard))
                throw new InvalidOperationException(
                    "Cannot pass: either the boneyard has tiles, or you have a playable card.");

            AdvanceTurn();
        }

        // Resets the board and deals new hands for another round
        public void StartNewRound()
        {
            Board = new BoardState();
            Board.InitializeBankCards();
            Board.AssignPlayerCards(Players);
            Boneyard = new Boneyard(Board.BankCards);
            Board.BankCards.Clear();

            CurrentPlayerIndex = DetermineFirstPlayer();
            IsGameOver= false;
        }

        // for UI use
        public bool CanCurrentPlayerPlay() => _rulesValidator.CanPlayerPlay(CurrentPlayer, Board);
        public bool CanCurrentPlayerDraw() => _rulesValidator.CanDraw(Boneyard);
        public bool CanCurrentPlayerPass() => _rulesValidator.CanPass(CurrentPlayer, Board, Boneyard);

        // save the result to a text file
        public void SaveResultToFile()
        {
            string outputDirectory = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "GameResults")
            );

            string line= $"Game Room_Name = \"{RoomName}\"";
            foreach(PlayerState player in Players.OrderBy(p => p.TotalScore))
            {
                line += $", Player Name = \"{player.PlayerName}\", Player Points = \"{player.TotalScore}\"";
            }

            string fileName= $"Result_{RoomName}_{DateTime.Now:yyyyMMdd_HHmm}.txt";
            File.WriteAllText(Path.Combine(outputDirectory, fileName), line);
        }

        // helpers
        private int DetermineFirstPlayer()
        {
            // player holding the highest double goes first
            for (int val = 6; val >= 0; val--)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    foreach (DominoTile tile in Players[i].Cards)
                    {
                        if (tile.LeftSide == val && tile.RightSide == val)
                            return i;
                    }
                }
            }

            // fallback: player with highest total tile goes first
            int bestPlayer = 0;
            int bestTotal = -1;
            for (int i = 0; i < Players.Count; i++)
            {
                foreach (DominoTile tile in Players[i].Cards)
                {
                    if (tile.Total > bestTotal)
                    {
                        bestTotal = tile.Total;
                        bestPlayer = i;
                    }
                }
            }
            return bestPlayer;
        }

        private void AdvanceTurn()
        {
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % Players.Count;
            OnTurnChanged?.Invoke(CurrentPlayer.PlayerName);
        }

        private void EndRound(PlayerState roundWinner)
        {
            // In a blocked round, the player with the lowest hand sum wins the round
            if (roundWinner == null)
                roundWinner = Players.OrderBy(p => p.GetHandSum()).First();

            // All other players add their remaining hand sum to their score
            foreach (PlayerState player in Players)
            {
                if (player != roundWinner)
                    player.AddToScore(player.GetHandSum());
            }

            OnRoundEnded?.Invoke(roundWinner.PlayerName);

            if (_rulesValidator.IsGameOver(Players, ScoreLimit))
            {
                IsGameOver = true;
                PlayerState gameWinner = _rulesValidator.GetWinner(Players);
                OnGameOver?.Invoke(gameWinner.PlayerName);
            }
        }
    }
}
