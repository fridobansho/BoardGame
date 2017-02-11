namespace BoardGame.Library.Implementations
{
    using Interfaces;

    public class Board : IBoard
    {
        public const int DEFAULT_LENGTH = 3;
        private IPiece[,] pieces;

        public int Height { get; }

        public int Width { get; }

        public Board() : this(DEFAULT_LENGTH, DEFAULT_LENGTH)
        {
        }

        public Board(int width, int height)
        {
            Width = width;
            Height = height;
            pieces = new IPiece[Width, Height];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    pieces[x, y] = new Piece();
                }
            }
        }

        public IPiece PieceAt(ILocation location)
        {
            if (CheckBounds(location))
            {
                return pieces[location.X, location.Y];
            }
            return new Piece();
        }

        public bool CheckBounds(ILocation location)
        {
            if ((location.X > (Width - 1)) || (location.X < 0)) return false;
            if ((location.Y > (Height - 1)) || (location.Y < 0)) return false;
            return true;
        }

        public void PieceAt(ILocation location, IPiece piece)
        {
            if (CheckBounds(location))
            {
                pieces[location.X, location.Y] = piece;
            }
        }
    }
}
