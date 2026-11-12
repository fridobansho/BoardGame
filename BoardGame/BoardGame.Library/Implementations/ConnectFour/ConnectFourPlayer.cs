namespace BoardGame.Library.Implementations.ConnectFour
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class ConnectFourPlayer : IPlayer
    {
        public string Name { get; }

        public ConnectFourPlayer() : this(nameof(ConnectFourPlayer))
        {
        }

        public ConnectFourPlayer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            var validCols = new List<int>();
            for (int x = 0; x < board.Width; x++)
            {
                if (board.PieceAt(x, 0).Value == BlankPiece.BlankValue)
                {
                    validCols.Add(x);
                }
            }

            if (validCols.Count == 0) return new Location(0, 0);

            int rCount = 0;
            int yCount = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var p = board.PieceAt(x, y);
                    if (p != null && p.Value == RedPiece.Red.Value) rCount++;
                    else if (p != null && p.Value == YellowPiece.Yellow.Value) yCount++;
                }
            }

            IPiece myPiece = (rCount <= yCount) ? (IPiece)RedPiece.Red : YellowPiece.Yellow;
            IPiece oppPiece = (myPiece.Value == RedPiece.Red.Value) ? (IPiece)YellowPiece.Yellow : RedPiece.Red;

            // 1. Can we win this turn in any column?
            foreach (var col in validCols)
            {
                var clone = CloneBoard(board);
                ApplyGravity(clone, col, myPiece);
                if (HasWon(clone, myPiece))
                {
                    return new Location(col, 0);
                }
            }

            // 2. Can opponent win next turn in any column? Block it!
            foreach (var col in validCols)
            {
                var clone = CloneBoard(board);
                ApplyGravity(clone, col, oppPiece);
                if (HasWon(clone, oppPiece))
                {
                    return new Location(col, 0);
                }
            }

            // 3. Center outward preference
            int center = board.Width / 2;
            int[] columnPreference = GenerateColumnPreference(board.Width, center);
            foreach (var col in columnPreference)
            {
                if (validCols.Contains(col))
                {
                    var clone = CloneBoard(board);
                    ApplyGravity(clone, col, myPiece);

                    // Check if opponent can now play on top and win
                    if (clone.PieceAt(col, 0).Value == BlankPiece.BlankValue)
                    {
                        var testOpponent = CloneBoard(clone);
                        ApplyGravity(testOpponent, col, oppPiece);
                        if (HasWon(testOpponent, oppPiece))
                        {
                            continue;
                        }
                    }
                    return new Location(col, 0);
                }
            }

            return new Location(validCols[0], 0);
        }

        private static int[] GenerateColumnPreference(int width, int center)
        {
            var list = new List<int> { center };
            for (int offset = 1; offset < width; offset++)
            {
                if (center - offset >= 0) list.Add(center - offset);
                if (center + offset < width) list.Add(center + offset);
            }
            return list.ToArray();
        }

        private static void ApplyGravity(IBoard board, int col, IPiece piece)
        {
            for (int y = board.Height - 1; y >= 0; y--)
            {
                if (board.PieceAt(col, y).Value == BlankPiece.BlankValue)
                {
                    board.PieceAt(col, y, piece);
                    return;
                }
            }
        }

        private static bool HasWon(IBoard board, IPiece piece)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y).Value != piece.Value) continue;

                    // Horizontal
                    if (x + 3 < board.Width &&
                        board.PieceAt(x + 1, y).Value == piece.Value &&
                        board.PieceAt(x + 2, y).Value == piece.Value &&
                        board.PieceAt(x + 3, y).Value == piece.Value)
                        return true;

                    // Vertical
                    if (y + 3 < board.Height &&
                        board.PieceAt(x, y + 1).Value == piece.Value &&
                        board.PieceAt(x, y + 2).Value == piece.Value &&
                        board.PieceAt(x, y + 3).Value == piece.Value)
                        return true;

                    // Diagonal down-right
                    if (x + 3 < board.Width && y + 3 < board.Height &&
                        board.PieceAt(x + 1, y + 1).Value == piece.Value &&
                        board.PieceAt(x + 2, y + 2).Value == piece.Value &&
                        board.PieceAt(x + 3, y + 3).Value == piece.Value)
                        return true;

                    // Diagonal up-right
                    if (x + 3 < board.Width && y - 3 >= 0 &&
                        board.PieceAt(x + 1, y - 1).Value == piece.Value &&
                        board.PieceAt(x + 2, y - 2).Value == piece.Value &&
                        board.PieceAt(x + 3, y - 3).Value == piece.Value)
                        return true;
                }
            }
            return false;
        }

        private static IBoard CloneBoard(IBoard board)
        {
            var clone = new Board(board.Width, board.Height);
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    clone.PieceAt(x, y, board.PieceAt(x, y));
                }
            }
            return clone;
        }
    }
}
