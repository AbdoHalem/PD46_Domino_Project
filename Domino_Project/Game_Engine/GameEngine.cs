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

        public delegate void TurnChangedEventHandler(string playerName);
        public event TurnChangedEventHandler OnTurnChanged;

        public delegate void RoundEndedEventHandler(string winnerName);
        public event RoundEndedEventHandler OnRoundEnded;

        public delegate void GameOverEventHandler(string winnerName);
        public event GameOverEventHandler OnGameOver; 

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

        public DominoTile DrawFromBoneyard()
        {
            if (!_rulesValidator.CanDraw(Boneyard, Board, CurrentPlayer))
                throw new InvalidOperationException("Cannot draw: either you have a playable card or the boneyard is empty.");

            DominoTile tile = Boneyard.DrawCard();
            CurrentPlayer.AddCard(tile);

            if (_rulesValidator.IsBlockedRound(Players, Board, Boneyard))
            {
                EndRound(null);
            }
            return tile;
        }

        public void Pass()
        {
            if (!_rulesValidator.CanPass(CurrentPlayer, Board, Boneyard))
                throw new InvalidOperationException(
                    "Cannot pass: either the boneyard has tiles, or you have a playable card.");
            if (_rulesValidator.IsBlockedRound(Players, Board, Boneyard))
            {
                EndRound(null);
                return;
            }

            AdvanceTurn();
        }

        public void StartNewRound()
        {
            foreach (var player in Players)
            {
                player.ClearCards();
            }

            Board = new BoardState();
            Board.InitializeBankCards();
            Board.AssignPlayerCards(Players);
            Boneyard = new Boneyard(Board.BankCards);
            Board.BankCards.Clear();

            CurrentPlayerIndex = DetermineFirstPlayer();
            IsGameOver = false;
        }

        public bool CanCurrentPlayerPlay() => _rulesValidator.CanPlayerPlay(CurrentPlayer, Board);
        public bool CanCurrentPlayerDraw() => _rulesValidator.CanDraw(Boneyard, Board, CurrentPlayer);
        public bool CanCurrentPlayerPass() => _rulesValidator.CanPass(CurrentPlayer, Board, Boneyard);

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

        private int DetermineFirstPlayer()
        {
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
            if (roundWinner == null)
                roundWinner = Players.OrderBy(p => p.GetHandSum()).First();

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
