using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class PlayerState
    {
        // Properties to represent the player's state
        public string PlayerName { get; private set; }
        public List<DominoTile> Cards { get; private set; }
        public int TotalScore { get; private set; }
        // Constructor to initialize the player's state
        public PlayerState(string playerName)
        {
            PlayerName = playerName;
            Cards = new List<DominoTile>();
            TotalScore = 0;
        }
        // Methods
        /**
         * Adds a domino tile to the player's hand.
         */
        public void AddCard(DominoTile card)
        {
            Cards.Add(card);
        }
        /**
         * Plays a domino tile from the player's hand.
         * Throws an exception if the player does not have the specified card.
         */
        public void PlayCard(DominoTile card)
        {
            if (Cards.Contains(card))
            {
                Cards.Remove(card);
            }
            else
            {
                throw new InvalidOperationException("The player does not have this card.");
            }
        }
        /**
         * Calculates the total sum of the values of the remaining cards in the player's hand.
         */
        public int GetHandSum()
        {
            return Cards.Sum(card => card.Total);
        }
        /**
         * Checks if the player has any playable card based on the current left and right values on the board.
         */
        public bool HasPlayableCard(int leftValue, int rightValue)
        {
            return Cards.Any(card => card.HasValue(leftValue) || card.HasValue(rightValue));
        }

        public void AddToScore(int points)
        {
            TotalScore += points;
        }
    }
}
