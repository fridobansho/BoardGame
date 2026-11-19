namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class MinimaxPlayer : IPlayer
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        private readonly Random _random = new();

        public string Name { get; }
        public IPiece AssignedPiece { get; set; }
        public int TargetLength { get; set; } = 0;
        public bool ExactTargetOnly { get; set; } = false;
        public bool IsMisere { get; set; } = false;

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

            var (myPiece, opponentPiece, availableMoves) = ResolvePiecesAndMoves(board);
            if (availableMoves.Count == 0) return new Location(0, 0);

            if (board.Width > 3 || board.Height > 3)
            {
                return SelectLargeBoardMove(board, myPiece.Value, opponentPiece.Value);
            }

            int target = TargetLength > 0 ? TargetLength : Math.Max(board.Width, board.Height);
            var clone = CloneBoard(board);
            int bestScore = int.MinValue;
            ILocation bestMove = availableMoves[0];

            foreach (var move in availableMoves)
            {
                clone.PieceAt(move.X, move.Y, myPiece);
                int score = Minimax(clone, 0, false, myPiece, opponentPiece, target, int.MinValue, int.MaxValue);
                clone.PieceAt(move.X, move.Y, BlankPiece.Blank);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private (IPiece MyPiece, IPiece OpponentPiece, List<ILocation> AvailableMoves) ResolvePiecesAndMoves(IBoard board)
        {
            int xCount = 0;
            int oCount = 0;
            int bCount = 0;
            int wCount = 0;
            var availableMoves = new List<ILocation>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var p = board.PieceAt(x, y);
                    if (p == null || p.Value == BlankPiece.BlankValue)
                    {
                        availableMoves.Add(new Location(x, y));
                    }
                    else if (p.Value == XPiece.X.Value) xCount++;
                    else if (p.Value == OPiece.O.Value) oCount++;
                    else if (p.Value == BlackPiece.Black.Value) bCount++;
                    else if (p.Value == WhitePiece.White.Value) wCount++;
                }
            }

            IPiece myPiece;
            IPiece opponentPiece;

            if (AssignedPiece != null)
            {
                myPiece = AssignedPiece;
                if (myPiece.Value == XPiece.X.Value) opponentPiece = OPiece.O;
                else if (myPiece.Value == OPiece.O.Value) opponentPiece = XPiece.X;
                else if (myPiece.Value == BlackPiece.Black.Value) opponentPiece = WhitePiece.White;
                else opponentPiece = BlackPiece.Black;
            }
            else if ((bCount > 0 || wCount > 0) && xCount == 0 && oCount == 0)
            {
                myPiece = bCount <= wCount ? BlackPiece.Black : WhitePiece.White;
                opponentPiece = myPiece.Value == BlackPiece.Black.Value ? WhitePiece.White : BlackPiece.Black;
            }
            else
            {
                myPiece = xCount <= oCount ? XPiece.X : OPiece.O;
                opponentPiece = myPiece.Value == XPiece.X.Value ? OPiece.O : XPiece.X;
            }

            return (myPiece, opponentPiece, availableMoves);
        }

        private ILocation SelectLargeBoardMove(IBoard board, string mySymbol, string oppSymbol)
        {
            var candidates = GetCandidateMoves(board);
            if (candidates.Count == 0)
            {
                return new Location(0, 0);
            }

            if (candidates.Count == 1 && IsBoardEmpty(board))
            {
                return candidates[0];
            }

            int maxDim = Math.Max(board.Width, board.Height);
            int target = TargetLength > 0 ? TargetLength : (maxDim <= 5 ? 4 : 5);

            ILocation bestMove = candidates[0];
            int bestScore = int.MinValue;

            foreach (var move in candidates)
            {
                int attackScore = EvaluateCell(board, move.X, move.Y, mySymbol, target, ExactTargetOnly);
                int defenseScore = EvaluateCell(board, move.X, move.Y, oppSymbol, target, ExactTargetOnly);

                int totalScore;
                if (IsMisere)
                {
                    totalScore = attackScore >= 100_000
                        ? -1_000_000
                        : -attackScore * 2 - defenseScore / 2;
                }
                else
                {
                    totalScore = attackScore >= 100_000
                        ? 1_000_000
                        : defenseScore >= 100_000
                            ? 500_000
                            : attackScore * 2 + defenseScore;
                }

                if (totalScore > bestScore || (totalScore == bestScore && _random.Next(2) == 0))
                {
                    bestScore = totalScore;
                    bestMove = move;
                }
            }

            return bestMove;
        }

        private int Minimax(IBoard board, int depth, bool isMaximizing, IPiece myPiece, IPiece opponentPiece, int target, int alpha, int beta)
        {
            if (HasWon(board, myPiece.Value, target, ExactTargetOnly)) return IsMisere ? depth - 10 : 10 - depth;
            if (HasWon(board, opponentPiece.Value, target, ExactTargetOnly)) return IsMisere ? 10 - depth : depth - 10;
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
                            int eval = Minimax(board, depth + 1, false, myPiece, opponentPiece, target, alpha, beta);
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
                            int eval = Minimax(board, depth + 1, true, myPiece, opponentPiece, target, alpha, beta);
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

        private static bool HasWon(IBoard board, string value, int target, bool exactOnly)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y)?.Value != value) continue;
                    foreach (var (dx, dy) in Directions)
                    {
                        if (TicTacToeLogic.HasTargetInARow(board, x, y, dx, dy, value, target, exactOnly))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsBoardEmpty(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static List<ILocation> GetCandidateMoves(IBoard board)
        {
            var occupied = new List<(int x, int y)>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        occupied.Add((x, y));
                    }
                }
            }

            if (occupied.Count == 0)
            {
                return new List<ILocation> { new Location(board.Width / 2, board.Height / 2) };
            }

            var seen = new HashSet<(int, int)>();
            var candidates = new List<ILocation>();
            foreach (var (ox, oy) in occupied)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -2; dy <= 2; dy++)
                    {
                        int nx = ox + dx;
                        int ny = oy + dy;
                        if (board.CheckBounds(nx, ny) && seen.Add((nx, ny)))
                        {
                            var piece = board.PieceAt(nx, ny);
                            if (piece != null && piece.Value == BlankPiece.BlankValue)
                            {
                                candidates.Add(new Location(nx, ny));
                            }
                        }
                    }
                }
            }

            if (candidates.Count == 0)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (board.PieceAt(x, y)?.Value == BlankPiece.BlankValue)
                        {
                            candidates.Add(new Location(x, y));
                        }
                    }
                }
            }

            return candidates;
        }

        private static int EvaluateCell(IBoard board, int x, int y, string symbol, int target, bool exactOnly)
        {
            int score = 0;
            foreach (var (dx, dy) in Directions)
            {
                int count = 1;
                int openEnds = 0;

                int fx = x + dx;
                int fy = y + dy;
                while (board.CheckBounds(fx, fy) && board.PieceAt(fx, fy)?.Value == symbol)
                {
                    count++;
                    fx += dx;
                    fy += dy;
                }
                if (board.CheckBounds(fx, fy) && board.PieceAt(fx, fy)?.Value == BlankPiece.BlankValue)
                {
                    openEnds++;
                }

                int bx = x - dx;
                int by = y - dy;
                while (board.CheckBounds(bx, by) && board.PieceAt(bx, by)?.Value == symbol)
                {
                    count++;
                    bx -= dx;
                    by -= dy;
                }
                if (board.CheckBounds(bx, by) && board.PieceAt(bx, by)?.Value == BlankPiece.BlankValue)
                {
                    openEnds++;
                }

                bool isWin = exactOnly ? count == target : count >= target;
                if (isWin) score += 100_000;
                else if (count == target - 1 && openEnds == 2) score += 10_000;
                else if (count == target - 1 && openEnds == 1) score += 1_000;
                else if (target >= 3 && count == target - 2 && openEnds == 2) score += 500;
                else if (target >= 3 && count == target - 2 && openEnds == 1) score += 100;
                else if (count >= 2 && openEnds == 2) score += 50;
                else if (count >= 2 && openEnds == 1) score += 10;
            }

            return score;
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
