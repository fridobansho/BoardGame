namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;

    public class Board : IBoard
    {
        public const uint DEFAULT_LENGTH = 3;
        private IPiece[,] pieces;

        public uint Height { get; private set; }

        public uint Width { get; private set; }

        public Board() : this(DEFAULT_LENGTH, DEFAULT_LENGTH)
        {
        }

        public Board(uint width, uint height)
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

        public IPiece PieceAt(uint x, uint y)
        {
            CheckBounds(x, y);
            return pieces[x, y];
        }

        private void CheckBounds(uint x, uint y)
        {
            if (x > Width) throw new ArgumentOutOfRangeException("x");
            if (y > Height) throw new ArgumentOutOfRangeException("y");
        }

        public void PieceAt(uint x, uint y, IPiece piece)
        {
            CheckBounds(x, y);
            pieces[x, y] = piece;
        }
    }
}
