namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class SmartChompPlayer : IPlayer
    {
        private static readonly ConcurrentDictionary<string, bool> Memo = new ConcurrentDictionary<string, bool>();

        public string Name { get; }
        public bool IsRowOnlyNim { get; set; } = false;
        public bool IsMisere { get; set; } = true;

        public SmartChompPlayer() : this(nameof(SmartChompPlayer))
        {
        }

        public SmartChompPlayer(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            if (IsRowOnlyNim || BoardHasStones(board))
            {
                return GetNimMove(board);
            }

            if (!IsMisere && board.PieceAt(0, 0)?.Value != BlankPiece.BlankValue)
            {
                // Normal Chomp: eating (0, 0) clears the bar and wins immediately
                return new Location(0, 0);
            }

            // Compute current column heights
            int[] heights = new int[board.Width];
            for (int x = 0; x < board.Width; x++)
            {
                int h = 0;
                for (int y = 0; y < board.Height; y++)
                {
                    var p = board.PieceAt(x, y);
                    if (p != null && p.Value != BlankPiece.BlankValue)
                    {
                        h = y + 1;
                    }
                }
                heights[x] = h;
            }

            // If only poison block left at (0, 0), forced to eat it
            if (heights[0] == 1)
            {
                bool onlyPoison = true;
                for (int x = 1; x < board.Width; x++)
                {
                    if (heights[x] > 0)
                    {
                        onlyPoison = false;
                        break;
                    }
                }
                if (onlyPoison) return new Location(0, 0);
            }

            // Find all valid non-poison moves
            var candidateMoves = new List<ILocation>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < heights[x]; y++)
                {
                    if (x == 0 && y == 0) continue; // skip poison block
                    candidateMoves.Add(new Location(x, y));
                }
            }

            if (candidateMoves.Count == 0) return new Location(0, 0);

            // Check if any move leads to a losing state for the opponent
            foreach (var move in candidateMoves)
            {
                var nextHeights = ApplyMoveToHeights(heights, move.X, move.Y);
                if (!IsWinningState(nextHeights))
                {
                    return move;
                }
            }

            // If in a losing position, pick a safe block (chomp furthest block to maximize game length)
            return candidateMoves[candidateMoves.Count - 1];
        }

        private static bool BoardHasStones(IBoard board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ILocation GetNimMove(IBoard board)
        {
            var rowCounts = new int[board.Height];
            int nimSum = 0;
            int rowsWithGt1 = 0;
            int rowsWith1 = 0;
            int bigRow = -1;

            for (int y = 0; y < board.Height; y++)
            {
                int count = 0;
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        count++;
                    }
                }
                rowCounts[y] = count;
                nimSum ^= count;
                if (count > 1)
                {
                    rowsWithGt1++;
                    bigRow = y;
                }
                else if (count == 1)
                {
                    rowsWith1++;
                }
            }

            if (IsMisere && rowsWithGt1 == 1)
            {
                // Misere endgame: leave an odd number of size-1 heaps
                int target = (rowsWith1 % 2 == 0) ? 1 : 0;
                return new Location(target, bigRow);
            }

            if (nimSum != 0)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    int target = rowCounts[y] ^ nimSum;
                    if (target < rowCounts[y])
                    {
                        return new Location(target, y);
                    }
                }
            }

            // Losing position (nimSum == 0): remove 1 stone from the largest row
            int bestRow = -1;
            int maxCount = 0;
            for (int y = 0; y < board.Height; y++)
            {
                if (rowCounts[y] > maxCount)
                {
                    maxCount = rowCounts[y];
                    bestRow = y;
                }
            }

            return bestRow >= 0 ? new Location(maxCount - 1, bestRow) : new Location(0, 0);
        }

        private static bool IsWinningState(int[] heights)
        {
            bool onlyPoison = (heights[0] == 1);
            if (onlyPoison)
            {
                for (int x = 1; x < heights.Length; x++)
                {
                    if (heights[x] > 0)
                    {
                        onlyPoison = false;
                        break;
                    }
                }
                if (onlyPoison) return false;
            }

            if (heights[0] == 0) return false;

            string key = string.Join(",", heights);
            if (Memo.TryGetValue(key, out bool cached))
            {
                return cached;
            }

            for (int x = 0; x < heights.Length; x++)
            {
                for (int y = 0; y < heights[x]; y++)
                {
                    if (x == 0 && y == 0) continue;

                    var nextHeights = ApplyMoveToHeights(heights, x, y);
                    if (!IsWinningState(nextHeights))
                    {
                        Memo[key] = true;
                        return true;
                    }
                }
            }

            Memo[key] = false;
            return false;
        }

        private static int[] ApplyMoveToHeights(int[] heights, int moveX, int moveY)
        {
            var next = (int[])heights.Clone();
            for (int x = moveX; x < next.Length; x++)
            {
                if (next[x] > moveY)
                {
                    next[x] = moveY;
                }
            }
            return next;
        }
    }
}
