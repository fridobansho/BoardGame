namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;

    public class SimplePlayer : IPlayer
    {
        public string Name { get; }

        public SimplePlayer() : this(typeof(SimplePlayer).Name)
        {

        }

        public SimplePlayer(string name)
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var location = new Location();
            for(int x = 0;x < board.Width;x++)
            {
                for(int y = 0;y < board.Height;y++)
                {
                    location = new Location(x, y);
                    var piece = board.PieceAt(location);
                    if(piece.Value == Piece.BlankValue)
                    {
                        return location;
                    }
                }
            }
            return location;
        }
    }
}
