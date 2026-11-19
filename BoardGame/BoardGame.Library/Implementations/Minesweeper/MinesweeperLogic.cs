namespace BoardGame.Library.Implementations.Minesweeper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class MinesweeperLogic : IMoveGameLogic, IScoredGameLogic, ICustomStateGameLogic
    {
        public const int DefaultSize = 9;
        public const int DefaultMines = 10;

        private readonly int? _fixedSeed;
        private readonly Dictionary<IPlayer, int> _scores = new();
        private bool[,] _mines = new bool[DefaultSize, DefaultSize];
        private bool _minesPlaced = false;
        private bool _isExploded = false;
        private bool _isWon = false;
        private IPlayer _winner;

        public int MineCount { get; set; } = DefaultMines;
        public bool IsExploded => _isExploded;
        public bool IsWon => _isWon;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public MinesweeperLogic(int? seed = null, int mineCount = DefaultMines)
        {
            _fixedSeed = seed;
            MineCount = Math.Max(1, mineCount);
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public static int GetDailySeed(DateTime? utcDate = null)
        {
            var dt = utcDate ?? DateTime.UtcNow;
            return int.Parse(dt.ToString("yyyyMMdd")) + 777;
        }

        public static MinesweeperLogic CreateDaily(DateTime? utcDate = null)
        {
            return new MinesweeperLogic(GetDailySeed(utcDate), DefaultMines);
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

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            var list = players.ToList();
            if (list.Count != 2) throw new ArgumentOutOfRangeException(nameof(players));

            PlayerPieces.Clear();
            _scores.Clear();
            PlayerPieces[list[0]] = new Piece("P1");
            PlayerPieces[list[1]] = new Piece("P2");
            _scores[list[0]] = 0;
            _scores[list[1]] = 0;
        }

        public void InitializeBoard(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            _mines = new bool[board.Width, board.Height];
            _minesPlaced = false;
            _isExploded = false;
            _isWon = false;
            _winner = null;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, new Piece("H"));
                }
            }

            foreach (var key in _scores.Keys.ToList())
            {
                _scores[key] = 0;
            }
        }

        private void PlaceMines(IBoard board, int safeX, int safeY)
        {
            int totalCells = board.Width * board.Height;
            int targetMines = Math.Min(MineCount, Math.Max(1, totalCells - 1));
            var candidates = new List<(int X, int Y)>(totalCells);

            if (_fixedSeed.HasValue)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        candidates.Add((x, y));
                    }
                }

                var rng = new Random(_fixedSeed.Value);
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }

                bool safeHadMine = false;
                for (int i = 0; i < targetMines && i < candidates.Count; i++)
                {
                    var (mx, my) = candidates[i];
                    if (mx == safeX && my == safeY)
                    {
                        safeHadMine = true;
                    }
                    else
                    {
                        _mines[mx, my] = true;
                    }
                }

                if (safeHadMine && targetMines < candidates.Count)
                {
                    // Deterministically relocate only the mine that landed on the first-clicked cell
                    var (rx, ry) = candidates[targetMines];
                    _mines[rx, ry] = true;
                }
            }
            else
            {
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (x == safeX && y == safeY) continue;
                        candidates.Add((x, y));
                    }
                }

                var rng = new Random();
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }

                for (int i = 0; i < targetMines && i < candidates.Count; i++)
                {
                    _mines[candidates[i].X, candidates[i].Y] = true;
                }
            }

            _minesPlaced = true;
        }

        private int CountAdjacentMines(IBoard board, int cx, int cy)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (board.CheckBounds(nx, ny) && _mines[nx, ny])
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            return PlayerPieces[player];
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board == null || location == null || _isExploded || _isWon) return false;
            if (!board.CheckBounds(location.X, location.Y)) return false;
            return board.PieceAt(location.X, location.Y)?.Value == "H";
        }

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            if (board == null || player == null || _isExploded || _isWon) return false;

            if (move is SudokuMove sm)
            {
                if (sm.Digit != 1 || !board.CheckBounds(sm.X, sm.Y)) return false;
                string v = board.PieceAt(sm.X, sm.Y)?.Value ?? "";
                return v == "H" || v == "F";
            }

            if (move is ILocation loc)
            {
                return IsValidMove(board, loc);
            }

            return false;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
            => ApplyMove(board, player, (IMove)location);

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!IsValidMove(board, player, move)) return false;

            // Digit == 1 toggles flag
            if (move is SudokuMove sm && sm.Digit == 1)
            {
                string curr = board.PieceAt(sm.X, sm.Y)?.Value ?? "H";
                board.PieceAt(sm.X, sm.Y, new Piece(curr == "F" ? "H" : "F"));
                return true;
            }

            var loc = (ILocation)move;
            if (!_minesPlaced)
            {
                PlaceMines(board, loc.X, loc.Y);
            }

            if (_mines[loc.X, loc.Y])
            {
                _isExploded = true;
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        if (_mines[x, y])
                        {
                            board.PieceAt(x, y, new Piece(x == loc.X && y == loc.Y ? "M" : "*"));
                        }
                    }
                }
                return true;
            }

            // Flood-fill reveal safe squares
            int newlyRevealed = 0;
            var visited = new bool[board.Width, board.Height];
            var queue = new Queue<(int X, int Y)>();
            queue.Enqueue((loc.X, loc.Y));
            visited[loc.X, loc.Y] = true;

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                string val = board.PieceAt(cx, cy)?.Value ?? "";
                if (val != "H") continue;
                if (_mines[cx, cy]) continue;

                int adj = CountAdjacentMines(board, cx, cy);
                board.PieceAt(cx, cy, new Piece(adj.ToString()));
                newlyRevealed++;

                if (adj == 0)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        for (int dy = -1; dy <= 1; dy++)
                        {
                            if (dx == 0 && dy == 0) continue;
                            int nx = cx + dx;
                            int ny = cy + dy;
                            if (board.CheckBounds(nx, ny) && !visited[nx, ny])
                            {
                                visited[nx, ny] = true;
                                queue.Enqueue((nx, ny));
                            }
                        }
                    }
                }
            }

            int pts = newlyRevealed * 10;
            if (CountRevealedSafeCells(board) >= board.Width * board.Height - Math.Min(MineCount, board.Width * board.Height - 1))
            {
                _isWon = true;
                _winner = player;
                pts += 200;
            }

            _scores[player] = GetScore(player) + pts;
            return true;
        }

        private int CountRevealedSafeCells(IBoard board)
        {
            int count = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    string v = board.PieceAt(x, y)?.Value ?? "";
                    if (v.Length == 1 && v[0] >= '0' && v[0] <= '8')
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public bool HasValidMoves(IBoard board) => !_isExploded && !_isWon;

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (players == null) throw new ArgumentNullException(nameof(players));

            var list = players.ToList();
            if (list.Count != 2) return Enumerable.Empty<IPlayer>();

            if (_isWon)
            {
                return _winner != null && list.Contains(_winner) ? new[] { _winner } : new[] { list[0] };
            }

            if (_isExploded)
            {
                return list;
            }

            return Enumerable.Empty<IPlayer>();
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            int w = _mines.GetLength(0);
            int h = _mines.GetLength(1);
            var chars = new char[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    chars[y * w + x] = _mines[x, y] ? '1' : '0';
                }
            }
            int winnerIdx = -1;
            if (players != null && _winner != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (ReferenceEquals(players[i], _winner))
                    {
                        winnerIdx = i;
                        break;
                    }
                }
            }
            return new Dictionary<string, string>
            {
                ["mines"] = new string(chars),
                ["w"] = w.ToString(),
                ["h"] = h.ToString(),
                ["minesPlaced"] = _minesPlaced ? "1" : "0",
                ["exploded"] = _isExploded ? "1" : "0",
                ["won"] = _isWon ? "1" : "0",
                ["winnerIdx"] = winnerIdx.ToString()
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (state.TryGetValue("w", out var wStr) && state.TryGetValue("h", out var hStr) &&
                int.TryParse(wStr, out int w) && int.TryParse(hStr, out int h) &&
                state.TryGetValue("mines", out var minesStr) && minesStr.Length == w * h)
            {
                _mines = new bool[w, h];
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        _mines[x, y] = minesStr[y * w + x] == '1';
                    }
                }
            }
            if (state.TryGetValue("minesPlaced", out var mp)) _minesPlaced = mp == "1";
            if (state.TryGetValue("exploded", out var exp)) _isExploded = exp == "1";
            if (state.TryGetValue("won", out var won)) _isWon = won == "1";
            if (state.TryGetValue("winnerIdx", out var winStr) && int.TryParse(winStr, out int winIdx))
            {
                _winner = (players != null && winIdx >= 0 && winIdx < players.Count) ? players[winIdx] : null;
            }
        }
    }
}
