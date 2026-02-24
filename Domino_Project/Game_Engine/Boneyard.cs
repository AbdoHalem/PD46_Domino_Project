using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Engine
{
    public class Boneyard
    {
        List<DominoTile> _cards { get; set; }
        Random _random { get; set; }

        public int Count => _cards.Count;
        public bool IsEmpty => _cards.Count == 0;

        public Boneyard(List<DominoTile> remainingCards)
        {
            _cards = new List<DominoTile>(remainingCards);
            _random = new Random();
        }

        // Draws a random card from the boneyard
        public DominoTile DrawCard()
        {
            if(IsEmpty)
                throw new InvalidOperationException("The boneyard is empty. No more cards to draw.");

            int index = _random.Next(_cards.Count);  // Generate a random index to select a card from the boneyard
            DominoTile drawnCard = _cards[index];
            _cards.RemoveAt(index);
            return drawnCard;
        }

        public List<DominoTile> GetRemainingCards()
        {
            return new List<DominoTile>(_cards);
        }
    }
}
