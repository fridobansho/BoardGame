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

        public IPiece PieceAt(int x, int y)
        {
            if (CheckBounds(x, y))
            {
                return pieces[x, y];
            }
            return null;
        }

        public bool CheckBounds(int x, int y)
        {
            if ((x > (Width - 1)) || (x < 0)) throw new ArgumentOutOfRangeException("x");
            if ((y > (Height - 1)) || (y < 0)) throw new ArgumentOutOfRangeException("y");
            return true;
        }

        public void PieceAt(int x, int y, IPiece piece)
        {
            if (CheckBounds(x, y))
            {
                pieces[x, y] = piece;
                return;
            }
        }
    }
}
