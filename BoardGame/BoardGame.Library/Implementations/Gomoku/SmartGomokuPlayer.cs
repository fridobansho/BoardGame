namespace BoardGame.Library.Implementations.Gomoku
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class SmartGomokuPlayer : IPlayer
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        private readonly Random _random = new Random();

        internal string AssignedSymbol { get; set; }
        internal bool ExactFiveOnly { get; set; }

        public string Name { get; }

        public SmartGomokuPlayer(string name = "Gomoku Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var candidates = GetCandidateMoves(board);
            if (candidates.Count == 0)
            {
                return null;
            }

            // If board is empty, play center
            if (candidates.Count == 1 && IsBoardEmpty(board))
            {
                return candidates[0];
            }

            var mySymbol = AssignedSymbol ?? InferMySymbol(board);
            var oppSymbol = mySymbol == BlackPiece.Black.Value ? WhitePiece.White.Value : BlackPiece.Black.Value;

            ILocation bestMove = candidates[0];
            int bestScore = int.MinValue;

            foreach (var move in candidates)
            {
                int attackScore = EvaluateCell(board, move.X, move.Y, mySymbol, ExactFiveOnly);
                int defenseScore = EvaluateCell(board, move.X, move.Y, oppSymbol, ExactFiveOnly);

                // Prioritize immediate win, then blocking opponent's 4-in-a-row, then building threats
                int totalScore = attackScore >= 100_000
                    ? 1_000_000
                    : defenseScore >= 100_000
                        ? 500_000
                        : attackScore * 2 + defenseScore;

                if (totalScore > bestScore || (totalScore == bestScore && _random.Next(2) == 0))
                {
                    bestScore = totalScore;
                    bestMove = move;
                }
            }

            return bestMove;
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

        private static string InferMySymbol(IBoard board)
        {
            int blackCount = 0;
            int whiteCount = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue) continue;
                    if (piece.Value == BlackPiece.Black.Value) blackCount++;
                    else if (piece.Value == WhitePiece.White.Value) whiteCount++;
                }
            }
            return blackCount <= whiteCount ? BlackPiece.Black.Value : WhitePiece.White.Value;
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

            return candidates;
        }

        private static int EvaluateCell(IBoard board, int x, int y, string symbol, bool exactFiveOnly)
        {
            int score = 0;
            foreach (var (dx, dy) in Directions)
            {
                int count = 1;
                int openEnds = 0;

                // Forward
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

                // Backward
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

                bool isWin = exactFiveOnly ? count == 5 : count >= 5;
                if (isWin) score += 100_000;
                else if (count == 4 && openEnds == 2) score += 10_000;
                else if (count == 4 && openEnds == 1) score += 1_000;
                else if (count == 3 && openEnds == 2) score += 500;
                else if (count == 3 && openEnds == 1) score += 100;
                else if (count == 2 && openEnds == 2) score += 50;
                else if (count == 2 && openEnds == 1) score += 10;
            }

            return score;
        }
    }
}
