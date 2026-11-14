namespace BoardGame.Library.Implementations.Nim
{
    using Interfaces;

    public class SmartNimPlayer : IPlayer
    {
        public string Name { get; }

        public SmartNimPlayer(string name = "Nim Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var rowCounts = new int[board.Height];
            int nimSum = 0;

            for (int y = 0; y < board.Height; y++)
            {
                int count = 0;
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        count++;
                    }
                }
                rowCounts[y] = count;
                nimSum ^= count;
            }

            if (nimSum != 0)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    int target = rowCounts[y] ^ nimSum;
                    if (target < rowCounts[y])
                    {
                        return new Location(target, y);
                    }
                }
            }

            // Losing position (nimSum == 0): remove 1 stone from the largest row
            int bestRow = -1;
            int maxCount = 0;
            for (int y = 0; y < board.Height; y++)
            {
                if (rowCounts[y] > maxCount)
                {
                    maxCount = rowCounts[y];
                    bestRow = y;
                }
            }

            return bestRow >= 0 ? new Location(maxCount - 1, bestRow) : null;
        }
    }
}
