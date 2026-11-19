namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class MinimaxPlayer : IPlayer
    {
        public string Name { get; }
        internal IPiece AssignedPiece { get; set; }

        public MinimaxPlayer() : this(nameof(MinimaxPlayer))
        {
        }

        public MinimaxPlayer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            int xCount = 0;
            int oCount = 0;
            var availableMoves = new List<ILocation>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var p = board.PieceAt(x, y);
                    if (p != null && p.Value == XPiece.X.Value) xCount++;
                    else if (p != null && p.Value == OPiece.O.Value) oCount++;
                    else if (p == null || p.Value == BlankPiece.BlankValue) availableMoves.Add(new Location(x, y));
                }
            }

            if (availableMoves.Count == 0) return new Location(0, 0);

            IPiece myPiece = AssignedPiece ?? ((xCount <= oCount) ? (IPiece)XPiece.X : OPiece.O);
            IPiece opponentPiece = (myPiece.Value == XPiece.X.Value) ? (IPiece)OPiece.O : XPiece.X;

            var clone = CloneBoard(board);
            if (board.Width > 3 || board.Height > 3)
            {
                return SelectLargeBoardMove(clone, availableMoves, myPiece, opponentPiece);
            }

            int bestScore = int.MinValue;
            ILocation bestMove = availableMoves[0];

            foreach (var move in availableMoves)
            {
                clone.PieceAt(move.X, move.Y, myPiece);

                int score = Minimax(clone, 0, false, myPiece, opponentPiece, int.MinValue, int.MaxValue);
                clone.PieceAt(move.X, move.Y, BlankPiece.Blank);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private static ILocation SelectLargeBoardMove(IBoard clone, List<ILocation> availableMoves, IPiece myPiece, IPiece opponentPiece)
        {
            foreach (var move in availableMoves)
            {
                clone.PieceAt(move.X, move.Y, myPiece);
                bool wins = HasWon(clone, myPiece);
                clone.PieceAt(move.X, move.Y, BlankPiece.Blank);
                if (wins) return move;
            }

            foreach (var move in availableMoves)
            {
                clone.PieceAt(move.X, move.Y, opponentPiece);
                bool opponentWins = HasWon(clone, opponentPiece);
                clone.PieceAt(move.X, move.Y, BlankPiece.Blank);
                if (opponentWins) return move;
            }

            int cx = clone.Width / 2;
            int cy = clone.Height / 2;
            if (clone.PieceAt(cx, cy)?.Value == BlankPiece.BlankValue)
            {
                return new Location(cx, cy);
            }

            var corners = new (int X, int Y)[]
            {
                (0, 0),
                (clone.Width - 1, 0),
                (0, clone.Height - 1),
                (clone.Width - 1, clone.Height - 1)
            };
            foreach (var (x, y) in corners)
            {
                if (clone.PieceAt(x, y)?.Value == BlankPiece.BlankValue)
                {
                    return new Location(x, y);
                }
            }

            return availableMoves[0];
        }

        private static int Minimax(IBoard board, int depth, bool isMaximizing, IPiece myPiece, IPiece opponentPiece, int alpha, int beta)
        {
            if (HasWon(board, myPiece)) return 10 - depth;
            if (HasWon(board, opponentPiece)) return depth - 10;
            if (IsFull(board)) return 0;

            if (isMaximizing)
            {
                int maxEval = int.MinValue;
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (board.PieceAt(x, y).Value == BlankPiece.BlankValue)
                        {
                            board.PieceAt(x, y, myPiece);
                            int eval = Minimax(board, depth + 1, false, myPiece, opponentPiece, alpha, beta);
                            board.PieceAt(x, y, BlankPiece.Blank);
                            maxEval = Math.Max(maxEval, eval);
                            alpha = Math.Max(alpha, eval);
                            if (beta <= alpha) break;
                        }
                    }
                    if (beta <= alpha) break;
                }
                return maxEval;
            }
            else
            {
                int minEval = int.MaxValue;
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (board.PieceAt(x, y).Value == BlankPiece.BlankValue)
                        {
                            board.PieceAt(x, y, opponentPiece);
                            int eval = Minimax(board, depth + 1, true, myPiece, opponentPiece, alpha, beta);
                            board.PieceAt(x, y, BlankPiece.Blank);
                            minEval = Math.Min(minEval, eval);
                            beta = Math.Min(beta, eval);
                            if (beta <= alpha) break;
                        }
                    }
                    if (beta <= alpha) break;
                }
                return minEval;
            }
        }

        private static bool HasWon(IBoard board, IPiece piece)
        {
            if (board.Width < 3 || board.Height < 3) return false;
            // Rows
            for (int y = 0; y < board.Height; y++)
            {
                bool rowWin = true;
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y).Value != piece.Value)
                    {
                        rowWin = false;
                        break;
                    }
                }
                if (rowWin) return true;
            }

            // Columns
            for (int x = 0; x < board.Width; x++)
            {
                bool colWin = true;
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y).Value != piece.Value)
                    {
                        colWin = false;
                        break;
                    }
                }
                if (colWin) return true;
            }

            if (board.Width == board.Height)
            {
                bool mainDiag = true;
                bool antiDiag = true;
                for (int i = 0; i < board.Width; i++)
                {
                    if (board.PieceAt(i, i).Value != piece.Value) mainDiag = false;
                    if (board.PieceAt(board.Width - 1 - i, i).Value != piece.Value) antiDiag = false;
                }
                if (mainDiag || antiDiag) return true;
            }

            return false;
        }

        private static bool IsFull(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y).Value == BlankPiece.BlankValue) return false;
                }
            }
            return true;
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
