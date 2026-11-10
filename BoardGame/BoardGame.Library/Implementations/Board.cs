namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System;

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
                    pieces[x, y] = BlankPiece.Blank;
                }
            }
        }

        public IPiece PieceAt(ILocation location)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            return PieceAt(location.X, location.Y);
        }

        public IPiece PieceAt(int x, int y)
        {
            ValidateBounds(x, y);
            return pieces[x, y];
        }

        public bool CheckBounds(int x, int y)
        {
            return (x >= 0) && (x < Width) && (y >= 0) && (y < Height);
        }

        private void ValidateBounds(int x, int y)
        {
            if ((x > (Width - 1)) || (x < 0)) throw new ArgumentOutOfRangeException("x", x, "Specified argument was out of the range of valid values.");
            if ((y > (Height - 1)) || (y < 0)) throw new ArgumentOutOfRangeException("y", y, "Specified argument was out of the range of valid values.");
        }

        public void PieceAt(ILocation location, IPiece piece)
        {
            if (location == null) throw new ArgumentNullException(nameof(location));
            PieceAt(location.X, location.Y, piece);
        }

        public void PieceAt(int x, int y, IPiece piece)
        {
            ValidateBounds(x, y);
            pieces[x, y] = piece;
        }
    }
}
