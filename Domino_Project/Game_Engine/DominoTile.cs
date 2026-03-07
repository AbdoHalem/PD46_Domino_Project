namespace Game_Engine
{
    public class DominoTile
    {
        public int LeftSide { get; private set; }
        public int RightSide { get; private set; }
        public int isDouble { get; private set; }
        public int Total { get; private set; }

        public DominoTile(int leftSide, int rightSide)
        {
            LeftSide = leftSide;
            RightSide = rightSide;
            isDouble = (leftSide == rightSide) ? 1 : 0;
            Total = leftSide + rightSide;
        }

        public bool HasValue(int value)
        {
            return LeftSide == value || RightSide == value;
        }

        public override bool Equals(object obj)
        {
            if (obj is DominoTile other)
            {
                return (LeftSide == other.LeftSide && RightSide == other.RightSide) ||
                       (LeftSide == other.RightSide && RightSide == other.LeftSide);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Math.Min(LeftSide, RightSide), Math.Max(LeftSide, RightSide));
        }
    }
}
