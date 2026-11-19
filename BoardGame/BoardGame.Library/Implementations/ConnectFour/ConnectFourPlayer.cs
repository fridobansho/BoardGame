namespace BoardGame.Library.Implementations.ConnectFour
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class ConnectFourPlayer : IPlayer
    {
        public string Name { get; }
        internal string AssignedSymbol { get; set; }
        internal int TargetLength { get; set; } = 4;
        internal bool IsMisere { get; set; } = false;

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

            IPiece myPiece;
            if (!string.IsNullOrEmpty(AssignedSymbol))
            {
                myPiece = AssignedSymbol == RedPiece.Red.Value ? RedPiece.Red : YellowPiece.Yellow;
            }
            else
            {
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
                myPiece = (rCount <= yCount) ? (IPiece)RedPiece.Red : YellowPiece.Yellow;
            }

            IPiece oppPiece = (myPiece.Value == RedPiece.Red.Value) ? (IPiece)YellowPiece.Yellow : RedPiece.Red;
            int target = Math.Max(2, TargetLength);
            var sim = CloneBoard(board);
            int center = board.Width / 2;
            int[] columnPreference = GenerateColumnPreference(board.Width, center);

            if (IsMisere)
            {
                var safeCols = new List<int>();
                foreach (var col in validCols)
                {
                    int row = ApplyGravity(sim, col, myPiece);
                    if (row >= 0)
                    {
                        bool losesImmediately = HasWon(sim, myPiece, target);
                        sim.PieceAt(col, row, BlankPiece.Blank);
                        if (!losesImmediately) safeCols.Add(col);
                    }
                }

                if (safeCols.Count == 0)
                {
                    return new Location(validCols[0], 0);
                }

                foreach (var col in columnPreference)
                {
                    if (!safeCols.Contains(col)) continue;
                    int row = ApplyGravity(sim, col, myPiece);
                    if (row >= 0)
                    {
                        bool forcesSelfTrap = false;
                        if (sim.PieceAt(col, 0).Value == BlankPiece.BlankValue)
                        {
                            int nextRow = ApplyGravity(sim, col, oppPiece);
                            if (nextRow >= 0 && sim.PieceAt(col, 0).Value == BlankPiece.BlankValue)
                            {
                                int thirdRow = ApplyGravity(sim, col, myPiece);
                                if (thirdRow >= 0)
                                {
                                    forcesSelfTrap = HasWon(sim, myPiece, target);
                                    sim.PieceAt(col, thirdRow, BlankPiece.Blank);
                                }
                            }
                            if (nextRow >= 0) sim.PieceAt(col, nextRow, BlankPiece.Blank);
                        }
                        sim.PieceAt(col, row, BlankPiece.Blank);
                        if (!forcesSelfTrap) return new Location(col, 0);
                    }
                }

                return new Location(safeCols[0], 0);
            }

            // 1. Can we win this turn in any column?
            foreach (var col in validCols)
            {
                int row = ApplyGravity(sim, col, myPiece);
                if (row >= 0)
                {
                    bool wins = HasWon(sim, myPiece, target);
                    sim.PieceAt(col, row, BlankPiece.Blank);
                    if (wins) return new Location(col, 0);
                }
            }

            // 2. Can opponent win next turn in any column? Block it!
            foreach (var col in validCols)
            {
                int row = ApplyGravity(sim, col, oppPiece);
                if (row >= 0)
                {
                    bool oppWins = HasWon(sim, oppPiece, target);
                    sim.PieceAt(col, row, BlankPiece.Blank);
                    if (oppWins) return new Location(col, 0);
                }
            }

            // 3. Center outward preference
            foreach (var col in columnPreference)
            {
                if (validCols.Contains(col))
                {
                    int row = ApplyGravity(sim, col, myPiece);
                    if (row >= 0)
                    {
                        bool givesOppWin = false;
                        if (sim.PieceAt(col, 0).Value == BlankPiece.BlankValue)
                        {
                            int oppRow = ApplyGravity(sim, col, oppPiece);
                            if (oppRow >= 0)
                            {
                                givesOppWin = HasWon(sim, oppPiece, target);
                                sim.PieceAt(col, oppRow, BlankPiece.Blank);
                            }
                        }
                        sim.PieceAt(col, row, BlankPiece.Blank);
                        if (!givesOppWin) return new Location(col, 0);
                    }
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

        private static int ApplyGravity(IBoard board, int col, IPiece piece)
        {
            for (int y = board.Height - 1; y >= 0; y--)
            {
                if (board.PieceAt(col, y).Value == BlankPiece.BlankValue)
                {
                    board.PieceAt(col, y, piece);
                    return y;
                }
            }
            return -1;
        }

        private static bool HasWon(IBoard board, IPiece piece, int targetLength)
        {
            (int dx, int dy)[] directions = { (1, 0), (0, 1), (1, 1), (1, -1) };

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y).Value != piece.Value) continue;

                    foreach (var (dx, dy) in directions)
                    {
                        if (TicTacToe.TicTacToeLogic.HasTargetInARow(board, x, y, dx, dy, piece.Value, targetLength, exactOnly: false))
                        {
                            return true;
                        }
                    }
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
