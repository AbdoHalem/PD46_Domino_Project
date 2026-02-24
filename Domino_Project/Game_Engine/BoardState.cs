using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class BoardState
    {
        // Prperties to represent the board state
        /**
         * List of domino tiles that have been played on the board.
         */
        public List<DominoTile> PlayedCards { get; private set; }
        public int LeftValue { get; private set; }
        public int RightValue { get; private set; }
        /**
         * List of domino tiles that are still available in the bank (not yet assigned to players).
         */
        public List<DominoTile> BankCards { get; private set; }
        // Constructor to initialize the board state
        public BoardState()
        {
            PlayedCards = new List<DominoTile>();
            BankCards = new List<DominoTile>();
            LeftValue = -1;     // Indicates no cards have been played yet
            RightValue = -1;    // Indicates no cards have been played yet
        }
        // Methods
        /**
         * Plays a domino tile on the board. Updates the left and right values accordingly.
         */
        public void InitializeBankCards()
        {
            for(int i = 0; i <= 6; i++)
            {
                for(int j = i; j <= 6; j++)
                {
                    BankCards.Add(new DominoTile(i, j));
                }
            }
        }
        /**
         * Assigns a specific number of random cards from the bank to each player.
         */
        public void AssignPlayerCards(List<PlayerState> players)
        {
            Random random = new Random();
            int numberOfCards = players.Count % 2 == 0 ? 7 : 6; // If odd number of players, each gets 6 cards, otherwise 7
            foreach (var player in players)
            {
                for (int i = 0; i < numberOfCards; i++)
                {
                    int index = random.Next(BankCards.Count);   // Generate a random index to select a card from the bank
                    player.AddCard(BankCards[index]);   // Assign a random card from the bank to the player
                    BankCards.RemoveAt(index);  // Remove the card from the bank after assigning it to a player
                }
            }
        }
        /**
         * Plays a domino tile on the left side of the board. Updates the left value based on the played card.
         */
        public void PlayCardAtLeft(DominoTile card)
        {
            PlayedCards.Add(card);
            if(LeftValue == -1)  // If this is the first card being played, set both left and right values
            {
                LeftValue = card.LeftSide;
                RightValue = card.RightSide;
            }
            else if (LeftValue == card.RightSide)  // If the right side of the card matches the current left value, update the left value
            {
                LeftValue = card.LeftSide;
            }
            else
            {
                LeftValue = card.RightSide;
            }         
        }
        /**
         * Plays a domino tile on the right side of the board. Updates the right value based on the played card.
         */
        public void PlayCardAtRight(DominoTile card)
        {
            PlayedCards.Add(card);
            if (RightValue == -1)  // If this is the first card being played, set both left and right values
            {
                LeftValue = card.LeftSide;
                RightValue = card.RightSide;
            }
            else if (RightValue == card.LeftSide)  // If the left side of the card matches the current right value, update the right value
            {
                RightValue = card.RightSide;
            }
            else
            {
                RightValue = card.LeftSide;
            }
        }
        /**
         * Checks if the bank is empty (i.e., all cards have been taken by players).
         */
        public bool IsBankEmpty()
        {
            return BankCards.Count == 0;
        }
    }
}
