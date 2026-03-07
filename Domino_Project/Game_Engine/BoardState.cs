using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class BoardState
    {
        public List<DominoTile> PlayedCards { get; private set; }
        public int LeftValue { get; private set; }
        public int RightValue { get; private set; }
        public List<DominoTile> BankCards { get; private set; }

        public BoardState()
        {
            PlayedCards = new List<DominoTile>();
            BankCards = new List<DominoTile>();
            LeftValue = -1;
            RightValue = -1;
        }

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

        public void AssignPlayerCards(List<PlayerState> players)
        {
            Random random = new Random();
            int numberOfCards = players.Count % 2 == 0 ? 7 : 6;
            foreach (var player in players)
            {
                for (int i = 0; i < numberOfCards; i++)
                {
                    int index = random.Next(BankCards.Count);
                    player.AddCard(BankCards[index]);
                    BankCards.RemoveAt(index);
                }
            }
        }

        public void PlayCardAtLeft(DominoTile card)
        {
            PlayedCards.Add(card);
            if(LeftValue == -1)
            {
                LeftValue = card.LeftSide;
                RightValue = card.RightSide;
            }
            else if (LeftValue == card.RightSide)
            {
                LeftValue = card.LeftSide;
            }
            else
            {
                LeftValue = card.RightSide;
            }         
        }

        public void PlayCardAtRight(DominoTile card)
        {
            PlayedCards.Add(card);
            if (RightValue == -1)
            {
                LeftValue = card.LeftSide;
                RightValue = card.RightSide;
            }
            else if (RightValue == card.LeftSide)
            {
                RightValue = card.RightSide;
            }
            else
            {
                RightValue = card.LeftSide;
            }
        }

        public bool IsBankEmpty()
        {
            return BankCards.Count == 0;
        }
    }
}
