namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class SmartChompPlayer : IPlayer
    {
        private static readonly Dictionary<string, bool> Memo = new Dictionary<string, bool>();

        public string Name { get; }

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

        private static bool IsWinningState(int[] heights)
        {
            // If only poison block remains at (0,0), next player must eat poison and lose!
            // Therefore, state where only (0,0) remains is NOT a winning state for the player to move (it is losing).
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

            // If board is empty, nothing left
            if (heights[0] == 0) return false;

            string key = string.Join(",", heights);
            if (Memo.TryGetValue(key, out bool cached))
            {
                return cached;
            }

            // Check all valid moves from this state (excluding (0,0) which is an instant loss)
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
