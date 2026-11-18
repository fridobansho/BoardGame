namespace BoardGame.Library.Implementations.Sudoku
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class SudokuLogic : IMoveGameLogic, IScoredGameLogic
    {
        public const int DefaultSize = 9;

        private readonly int? _fixedSeed;
        private readonly int _emptyCells;
        private readonly int[,] _solution = new int[DefaultSize, DefaultSize];
        private readonly Dictionary<IPlayer, int> _scores = new();

        internal static SudokuLogic LastInitializedLogic { get; private set; }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public SudokuLogic(int? seed = null, int emptyCells = 40)
        {
            _fixedSeed = seed;
            _emptyCells = Math.Clamp(emptyCells, 1, 60);
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public static int GetDailySeed(DateTime? utcDate = null)
        {
            var dt = utcDate ?? DateTime.UtcNow;
            return int.Parse(dt.ToString("yyyyMMdd"));
        }

        public static SudokuLogic CreateDaily(DateTime? utcDate = null)
        {
            return new SudokuLogic(GetDailySeed(utcDate), emptyCells: 40);
        }

        public int GetScore(IPlayer player)
        {
            if (player != null && _scores.TryGetValue(player, out int score))
            {
                return score;
            }
            return 0;
        }

        public void SetScore(IPlayer player, int score)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            _scores[player] = score;
        }

        public int GetSolutionAt(int x, int y)
        {
            if (x < 0 || x >= DefaultSize || y < 0 || y >= DefaultSize)
            {
                throw new ArgumentOutOfRangeException();
            }
            return _solution[x, y];
        }

        public static int GetDigitAt(IBoard board, int x, int y)
        {
            if (board == null || !board.CheckBounds(x, y)) return 0;
            var val = board.PieceAt(x, y)?.Value;
            if (string.IsNullOrEmpty(val)) return 0;

            if (val.Length == 1 && val[0] >= '1' && val[0] <= '9')
            {
                return val[0] - '0';
            }

            if (val.Length == 2 && val[0] == 'G' && val[1] >= '1' && val[1] <= '9')
            {
                return val[1] - '0';
            }

            return 0;
        }

        public static bool IsGivenCell(IBoard board, int x, int y)
        {
            if (board == null || !board.CheckBounds(x, y)) return false;
            var val = board.PieceAt(x, y)?.Value;
            return !string.IsNullOrEmpty(val) && val.StartsWith("G", StringComparison.Ordinal);
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            _scores.Clear();

            int idx = 1;
            foreach (var p in players)
            {
                PlayerPieces[p] = new Piece($"P{idx++}");
                _scores[p] = 0;
                BindPlayer(p);
            }

            LastInitializedLogic = this;
        }

        private void BindPlayer(IPlayer player)
        {
            if (player is RandomSudokuPlayer randomPlayer)
            {
                randomPlayer.Bind(this);
            }
            else if (player is SmartSudokuPlayer smartPlayer)
            {
                smartPlayer.Bind(this);
            }
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            int count = players.Count();
            if (count < 1 || count > 2) throw new ArgumentOutOfRangeException(nameof(players));
        }

        public void InitializeBoard(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var rng = _fixedSeed.HasValue ? new Random(_fixedSeed.Value) : new Random();

            int[] digits = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            Shuffle(digits, rng);

            int[] rowOrder = BuildGroupedOrder(rng);
            int[] colOrder = BuildGroupedOrder(rng);

            for (int y = 0; y < DefaultSize; y++)
            {
                for (int x = 0; x < DefaultSize; x++)
                {
                    int srcY = rowOrder[y];
                    int srcX = colOrder[x];
                    int baseVal = (srcY * 3 + srcY / 3 + srcX) % 9;
                    int digit = digits[baseVal];
                    _solution[x, y] = digit;

                    if (board.CheckBounds(x, y))
                    {
                        board.PieceAt(x, y, new Piece($"G{digit}"));
                    }
                }
            }

            var coords = new List<(int x, int y)>(81);
            for (int y = 0; y < Math.Min(board.Height, DefaultSize); y++)
            {
                for (int x = 0; x < Math.Min(board.Width, DefaultSize); x++)
                {
                    coords.Add((x, y));
                }
            }
            Shuffle(coords, rng);

            for (int i = 0; i < _emptyCells && i < coords.Count; i++)
            {
                var (x, y) = coords[i];
                board.PieceAt(x, y, BlankPiece.Blank);
            }

            foreach (var p in PlayerPieces.Keys.ToList())
            {
                _scores[p] = 0;
            }

            LastInitializedLogic = this;
        }

        private static int[] BuildGroupedOrder(Random rng)
        {
            int[] groups = { 0, 1, 2 };
            Shuffle(groups, rng);

            var result = new int[DefaultSize];
            int idx = 0;
            foreach (int g in groups)
            {
                int[] inner = { 0, 1, 2 };
                Shuffle(inner, rng);
                foreach (int i in inner)
                {
                    result[idx++] = g * 3 + i;
                }
            }
            return result;
        }

        private static void Shuffle<T>(IList<T> items, Random rng)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (items[i], items[j]) = (items[j], items[i]);
            }
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            return IsValidMoveInternal(board, location as SudokuMove);
        }

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            return IsValidMoveInternal(board, move as SudokuMove);
        }

        private bool IsValidMoveInternal(IBoard board, SudokuMove sm)
        {
            if (board == null || sm == null) return false;
            if (!board.CheckBounds(sm.X, sm.Y)) return false;
            if (sm.X >= DefaultSize || sm.Y >= DefaultSize) return false;
            if (sm.Digit < 1 || sm.Digit > 9) return false;

            if (GetDigitAt(board, sm.X, sm.Y) != 0)
            {
                return false;
            }

            if (HasPlacementConflict(board, sm.X, sm.Y, sm.Digit))
            {
                return false;
            }

            // Fast path: matches the generated solution and existing placements match it
            if (_solution[sm.X, sm.Y] == sm.Digit && BoardConsistentWithSolution(board))
            {
                return true;
            }

            return CanSolveWithPlacement(board, sm.X, sm.Y, sm.Digit);
        }

        private bool BoardConsistentWithSolution(IBoard board)
        {
            for (int y = 0; y < DefaultSize; y++)
            {
                for (int x = 0; x < DefaultSize; x++)
                {
                    int val = GetDigitAt(board, x, y);
                    if (val != 0 && val != _solution[x, y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static bool HasPlacementConflict(IBoard board, int targetX, int targetY, int digit)
        {
            for (int i = 0; i < DefaultSize; i++)
            {
                if (GetDigitAt(board, i, targetY) == digit) return true;
                if (GetDigitAt(board, targetX, i) == digit) return true;
            }

            int boxStartX = (targetX / 3) * 3;
            int boxStartY = (targetY / 3) * 3;
            for (int dy = 0; dy < 3; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    if (GetDigitAt(board, boxStartX + dx, boxStartY + dy) == digit)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool CanSolveWithPlacement(IBoard board, int targetX, int targetY, int digit)
        {
            int[,] grid = new int[DefaultSize, DefaultSize];
            int[] rowMask = new int[DefaultSize];
            int[] colMask = new int[DefaultSize];
            int[] boxMask = new int[DefaultSize];

            for (int y = 0; y < DefaultSize; y++)
            {
                for (int x = 0; x < DefaultSize; x++)
                {
                    int v = (x == targetX && y == targetY) ? digit : GetDigitAt(board, x, y);
                    grid[x, y] = v;
                    if (v != 0)
                    {
                        int bit = 1 << (v - 1);
                        int box = (y / 3) * 3 + (x / 3);
                        if ((rowMask[y] & bit) != 0 || (colMask[x] & bit) != 0 || (boxMask[box] & bit) != 0)
                        {
                            return false;
                        }
                        rowMask[y] |= bit;
                        colMask[x] |= bit;
                        boxMask[box] |= bit;
                    }
                }
            }

            return SolveMrv(grid, rowMask, colMask, boxMask);
        }

        private static bool SolveMrv(int[,] grid, int[] rowMask, int[] colMask, int[] boxMask)
        {
            int bestX = -1;
            int bestY = -1;
            int bestCandMask = 0;
            int bestCount = 10;

            for (int y = 0; y < DefaultSize; y++)
            {
                for (int x = 0; x < DefaultSize; x++)
                {
                    if (grid[x, y] != 0) continue;

                    int box = (y / 3) * 3 + (x / 3);
                    int used = rowMask[y] | colMask[x] | boxMask[box];
                    int allowed = (~used) & 0x1FF;
                    if (allowed == 0) return false;

                    int count = BitCount(allowed);
                    if (count < bestCount)
                    {
                        bestCount = count;
                        bestCandMask = allowed;
                        bestX = x;
                        bestY = y;
                        if (count == 1) goto found;
                    }
                }
            }

        found:
            if (bestX == -1) return true;

            int bestBox = (bestY / 3) * 3 + (bestX / 3);
            for (int d = 1; d <= 9; d++)
            {
                int bit = 1 << (d - 1);
                if ((bestCandMask & bit) == 0) continue;

                grid[bestX, bestY] = d;
                rowMask[bestY] |= bit;
                colMask[bestX] |= bit;
                boxMask[bestBox] |= bit;

                if (SolveMrv(grid, rowMask, colMask, boxMask))
                {
                    return true;
                }

                grid[bestX, bestY] = 0;
                rowMask[bestY] &= ~bit;
                colMask[bestX] &= ~bit;
                boxMask[bestBox] &= ~bit;
            }

            return false;
        }

        private static int BitCount(int mask)
        {
            int c = 0;
            while (mask != 0)
            {
                mask &= mask - 1;
                c++;
            }
            return c;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            return ApplyMove(board, player, (IMove)location);
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!(move is SudokuMove sm)) return false;
            if (!IsValidMoveInternal(board, sm)) return false;

            board.PieceAt(sm.X, sm.Y, new Piece(sm.Digit.ToString()));

            int earned = CalculatePlacementPoints(board, sm.X, sm.Y);
            if (player != null)
            {
                _scores[player] = GetScore(player) + earned;
            }

            return true;
        }

        internal static int CalculatePlacementPoints(IBoard board, int placedX, int placedY)
        {
            int points = 10;

            bool rowFull = true;
            for (int x = 0; x < DefaultSize; x++)
            {
                if (GetDigitAt(board, x, placedY) == 0)
                {
                    rowFull = false;
                    break;
                }
            }
            if (rowFull) points += 20;

            bool colFull = true;
            for (int y = 0; y < DefaultSize; y++)
            {
                if (GetDigitAt(board, placedX, y) == 0)
                {
                    colFull = false;
                    break;
                }
            }
            if (colFull) points += 20;

            bool boxFull = true;
            int boxStartX = (placedX / 3) * 3;
            int boxStartY = (placedY / 3) * 3;
            for (int dy = 0; dy < 3 && boxFull; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    if (GetDigitAt(board, boxStartX + dx, boxStartY + dy) == 0)
                    {
                        boxFull = false;
                        break;
                    }
                }
            }
            if (boxFull) points += 20;

            return points;
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (PlayerPieces.TryGetValue(player, out var piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            ValidatePlayers(players);

            for (int y = 0; y < DefaultSize; y++)
            {
                for (int x = 0; x < DefaultSize; x++)
                {
                    if (GetDigitAt(board, x, y) == 0)
                    {
                        return Enumerable.Empty<IPlayer>();
                    }
                }
            }

            var list = players.ToList();
            if (list.Count == 1)
            {
                return list;
            }

            int s1 = GetScore(list[0]);
            int s2 = GetScore(list[1]);

            if (s1 == 0 && s2 == 0)
            {
                return new[] { list[0] };
            }

            if (s1 > s2) return new[] { list[0] };
            if (s2 > s1) return new[] { list[1] };
            return list;
        }
    }
}
