namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;

    public class ChompPlayer : IPlayer
    {
        public string Name { get; }

        public ChompPlayer() : this(typeof(ChompPlayer).Name)
        {
        }

        public ChompPlayer(string name)
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            // Strategy: try not to eat the poison block at (0, 0) if another uneaten piece is available.
            // Search from top-right to bottom-left to chomp safe pieces first.
            ILocation fallbackPoison = null;

            for (int x = board.Width - 1; x >= 0; x--)
            {
                for (int y = board.Height - 1; y >= 0; y--)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        if (x == 0 && y == 0)
                        {
                            fallbackPoison = new Location(0, 0);
                        }
                        else
                        {
                            return new Location(x, y);
                        }
                    }
                }
            }

            return fallbackPoison ?? new Location(0, 0);
        }
    }
}
