namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class MinimaxPlayer : IPlayer
    {
        public string Name { get; }

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

            IPiece myPiece = (xCount <= oCount) ? (IPiece)XPiece.X : OPiece.O;
            IPiece opponentPiece = (myPiece.Value == XPiece.X.Value) ? (IPiece)OPiece.O : XPiece.X;

            int bestScore = int.MinValue;
            ILocation bestMove = availableMoves[0];

            foreach (var move in availableMoves)
            {
                var clone = CloneBoard(board);
                clone.PieceAt(move.X, move.Y, myPiece);

                int score = Minimax(clone, 0, false, myPiece, opponentPiece, int.MinValue, int.MaxValue);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
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
            // Rows
            for (int y = 0; y < board.Height; y++)
            {
                if (board.PieceAt(0, y).Value == piece.Value &&
                    board.PieceAt(1, y).Value == piece.Value &&
                    board.PieceAt(2, y).Value == piece.Value)
                    return true;
            }

            // Columns
            for (int x = 0; x < board.Width; x++)
            {
                if (board.PieceAt(x, 0).Value == piece.Value &&
                    board.PieceAt(x, 1).Value == piece.Value &&
                    board.PieceAt(x, 2).Value == piece.Value)
                    return true;
            }

            // Main diagonal
            if (board.PieceAt(0, 0).Value == piece.Value &&
                board.PieceAt(1, 1).Value == piece.Value &&
                board.PieceAt(2, 2).Value == piece.Value)
                return true;

            // Anti diagonal
            if (board.PieceAt(2, 0).Value == piece.Value &&
                board.PieceAt(1, 1).Value == piece.Value &&
                board.PieceAt(0, 2).Value == piece.Value)
                return true;

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
