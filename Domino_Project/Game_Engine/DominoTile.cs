namespace Game_Engine
{
    public class DominoTile
    {
        // Prperties
        public int LeftSide { get; private set; }
        public int RightSide { get; private set; }
        public int isDouble { get; private set; }
        public int Total { get; private set; }
        // Methods
        public DominoTile(int leftSide, int rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
            isDouble = (leftSide == rightSide) ? 1 : 0;
            Total = leftSide + rightSide;
        }
        /**
         * Checks if the tile has a specific value on either side.
         */
        public bool HasValue(int value)
        {
            return LeftSide == value || RightSide == value;
        }
    }
}
