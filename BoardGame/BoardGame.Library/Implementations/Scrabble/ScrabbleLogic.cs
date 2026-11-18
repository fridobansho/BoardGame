namespace BoardGame.Library.Implementations.Scrabble
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class ScrabbleLogic : IMoveGameLogic, IScoredGameLogic, ICustomStateGameLogic
    {
        public const int DefaultSize = 15;
        public const int RackSize = 7;

        public bool IsShortBag { get; set; } = false;
        public bool DoubleBonuses { get; set; } = false;

        private static readonly (char Letter, int Count, int Points)[] TileDistribution =
        {
            ('A', 9, 1), ('B', 2, 3), ('C', 2, 3), ('D', 4, 2), ('E', 12, 1),
            ('F', 2, 4), ('G', 3, 2), ('H', 2, 4), ('I', 9, 1), ('J', 1, 8),
            ('K', 1, 5), ('L', 4, 1), ('M', 2, 3), ('N', 6, 1), ('O', 8, 1),
            ('P', 2, 3), ('Q', 1, 10), ('R', 6, 1), ('S', 4, 1), ('T', 6, 1),
            ('U', 4, 1), ('V', 2, 4), ('W', 2, 4), ('X', 1, 8), ('Y', 2, 4),
            ('Z', 1, 10)
        };

        private static readonly Dictionary<char, int> LetterPointsMap = BuildPointsMap();
        private static readonly string[,] StandardBonusGrid = BuildStandardBonusGrid();

        private readonly Dictionary<IPlayer, List<char>> _racks = new();
        private readonly Dictionary<IPlayer, int> _scores = new();
        private readonly List<char> _tileBag = new();
        private readonly Random _random = new Random();
        private int _consecutivePasses;

        internal static ScrabbleLogic LastInitializedLogic { get; private set; }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public int TileBagCount => _tileBag.Count;

        public int ConsecutivePasses => _consecutivePasses;

        public ScrabbleLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        private static Dictionary<char, int> BuildPointsMap()
        {
            var map = new Dictionary<char, int>();
            foreach (var (letter, _, pts) in TileDistribution)
            {
                map[letter] = pts;
            }
            return map;
        }

        private static string[,] BuildStandardBonusGrid()
        {
            var grid = new string[DefaultSize, DefaultSize];
            for (int x = 0; x < DefaultSize; x++)
            {
                for (int y = 0; y < DefaultSize; y++)
                {
                    grid[x, y] = string.Empty;
                }
            }

            void SetSymmetric(int x, int y, string bonus)
            {
                int mx = DefaultSize - 1 - x;
                int my = DefaultSize - 1 - y;
                grid[x, y] = bonus;
                grid[mx, y] = bonus;
                grid[x, my] = bonus;
                grid[mx, my] = bonus;
                grid[y, x] = bonus;
                grid[my, x] = bonus;
                grid[y, mx] = bonus;
                grid[my, mx] = bonus;
            }

            // Triple Word
            SetSymmetric(0, 0, "TW");
            SetSymmetric(7, 0, "TW");

            // Double Word
            for (int i = 1; i <= 4; i++) SetSymmetric(i, i, "DW");
            grid[7, 7] = "DW";

            // Triple Letter
            SetSymmetric(5, 1, "TL");
            SetSymmetric(5, 5, "TL");

            // Double Letter
            SetSymmetric(3, 0, "DL");
            SetSymmetric(6, 2, "DL");
            SetSymmetric(7, 3, "DL");
            SetSymmetric(6, 6, "DL");

            return grid;
        }

        public static int GetLetterPoints(char letter)
        {
            char upper = char.ToUpperInvariant(letter);
            return LetterPointsMap.TryGetValue(upper, out int pts) ? pts : 0;
        }

        public static string GetBonusAt(int x, int y, int width = DefaultSize, int height = DefaultSize)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return string.Empty;
            if (width == DefaultSize && height == DefaultSize)
            {
                return StandardBonusGrid[x, y];
            }
            if (x == width / 2 && y == height / 2)
            {
                return "DW";
            }
            return string.Empty;
        }

        public IReadOnlyList<char> GetRack(IPlayer player)
        {
            if (player != null && _racks.TryGetValue(player, out var rack))
            {
                return rack;
            }
            return Array.Empty<char>();
        }

        public void SetRack(IPlayer player, IEnumerable<char> tiles)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            _racks[player] = tiles != null
                ? tiles.Select(char.ToUpperInvariant).Where(c => c >= 'A' && c <= 'Z').ToList()
                : new List<char>();
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

        public IReadOnlyList<char> GetTileBag() => _tileBag;

        public void SetTileBag(IEnumerable<char> tiles)
        {
            _tileBag.Clear();
            if (tiles != null)
            {
                _tileBag.AddRange(tiles.Select(char.ToUpperInvariant).Where(c => c >= 'A' && c <= 'Z'));
            }
        }

        public void SetConsecutivePasses(int passes)
        {
            _consecutivePasses = Math.Max(0, passes);
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            var state = new Dictionary<string, string>
            {
                ["tileBag"] = new string(GetTileBag().ToArray()),
                ["consecutivePasses"] = ConsecutivePasses.ToString()
            };
            if (players != null && players.Count > 0)
            {
                state["p1Rack"] = new string(GetRack(players[0]).ToArray());
            }
            if (players != null && players.Count > 1)
            {
                state["p2Rack"] = new string(GetRack(players[1]).ToArray());
            }
            return state;
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (players != null && players.Count > 0 && state.TryGetValue("p1Rack", out var p1Rack))
            {
                SetRack(players[0], p1Rack);
            }
            if (players != null && players.Count > 1 && state.TryGetValue("p2Rack", out var p2Rack))
            {
                SetRack(players[1], p2Rack);
            }
            if (state.TryGetValue("tileBag", out var tileBag))
            {
                SetTileBag(tileBag);
            }
            if (state.TryGetValue("consecutivePasses", out var passesStr) && int.TryParse(passesStr, out var passes))
            {
                SetConsecutivePasses(passes);
            }
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            _racks.Clear();
            _scores.Clear();

            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);

            PlayerPieces.Add(p1, new Piece("P1"));
            PlayerPieces.Add(p2, new Piece("P2"));

            _racks[p1] = new List<char>();
            _racks[p2] = new List<char>();
            _scores[p1] = 0;
            _scores[p2] = 0;

            BindPlayer(p1);
            BindPlayer(p2);

            LastInitializedLogic = this;
        }

        private void BindPlayer(IPlayer player)
        {
            if (player is RandomScrabblePlayer randomPlayer)
            {
                randomPlayer.Bind(this);
            }
            else if (player is SmartScrabblePlayer smartPlayer)
            {
                smartPlayer.Bind(this);
            }
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (players.Count() != 2) throw new ArgumentOutOfRangeException(nameof(players));
        }

        public void InitializeBoard(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }

            _consecutivePasses = 0;
            _tileBag.Clear();

            foreach (var (letter, count, _) in TileDistribution)
            {
                for (int i = 0; i < count; i++)
                {
                    _tileBag.Add(letter);
                }
            }

            // Fisher-Yates shuffle
            for (int i = _tileBag.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (_tileBag[i], _tileBag[j]) = (_tileBag[j], _tileBag[i]);
            }

            foreach (var player in PlayerPieces.Keys.ToList())
            {
                _scores[player] = 0;
                _racks[player] = new List<char>();
                DrawTiles(player, RackSize);
            }

            if (IsShortBag && _tileBag.Count > 22)
            {
                _tileBag.RemoveRange(22, _tileBag.Count - 22);
            }

            LastInitializedLogic = this;
        }

        private void DrawTiles(IPlayer player, int targetRackSize)
        {
            if (!_racks.TryGetValue(player, out var rack))
            {
                rack = new List<char>();
                _racks[player] = rack;
            }

            while (rack.Count < targetRackSize && _tileBag.Count > 0)
            {
                int lastIdx = _tileBag.Count - 1;
                rack.Add(_tileBag[lastIdx]);
                _tileBag.RemoveAt(lastIdx);
            }
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            return IsValidMoveInternal(board, null, location as WordMove);
        }

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            return IsValidMoveInternal(board, player, move as WordMove);
        }

        private bool IsValidMoveInternal(IBoard board, IPlayer player, WordMove wm)
        {
            if (board == null || wm == null) return false;
            if (wm.IsPass) return true;
            if (wm.Placements == null || wm.Placements.Count == 0) return false;

            var placementMap = new Dictionary<(int x, int y), char>();
            foreach (var p in wm.Placements)
            {
                if (!board.CheckBounds(p.X, p.Y)) return false;
                if (p.Letter < 'A' || p.Letter > 'Z') return false;
                if (!placementMap.TryAdd((p.X, p.Y), p.Letter)) return false;

                var existing = board.PieceAt(p.X, p.Y);
                if (existing != null && existing.Value != BlankPiece.BlankValue)
                {
                    return false;
                }
            }

            // Verify rack contains placed letters (if player rack is initialized and populated)
            if (player != null && _racks.TryGetValue(player, out var rack) && rack.Count > 0)
            {
                var available = new int[26];
                foreach (char c in rack)
                {
                    if (c >= 'A' && c <= 'Z') available[c - 'A']++;
                }
                foreach (var p in wm.Placements)
                {
                    if (--available[p.Letter - 'A'] < 0)
                    {
                        return false;
                    }
                }
            }

            bool allSameY = wm.Placements.All(p => p.Y == wm.Placements[0].Y);
            bool allSameX = wm.Placements.All(p => p.X == wm.Placements[0].X);
            if (!allSameY && !allSameX) return false;

            // Contiguity along the placement line
            if (wm.Placements.Count > 1)
            {
                if (allSameY)
                {
                    int y = wm.Placements[0].Y;
                    int minX = wm.Placements.Min(p => p.X);
                    int maxX = wm.Placements.Max(p => p.X);
                    for (int x = minX; x <= maxX; x++)
                    {
                        if (!placementMap.ContainsKey((x, y)) && IsSquareBlank(board, x, y))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    int x = wm.Placements[0].X;
                    int minY = wm.Placements.Min(p => p.Y);
                    int maxY = wm.Placements.Max(p => p.Y);
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (!placementMap.ContainsKey((x, y)) && IsSquareBlank(board, x, y))
                        {
                            return false;
                        }
                    }
                }
            }

            bool boardIsEmpty = IsBoardEmpty(board);
            if (boardIsEmpty)
            {
                if (wm.Placements.Count < 2) return false;
                int centerX = board.Width / 2;
                int centerY = board.Height / 2;
                if (!placementMap.ContainsKey((centerX, centerY))) return false;
            }
            else
            {
                bool touchesExisting = false;
                foreach (var p in wm.Placements)
                {
                    if (!IsSquareBlank(board, p.X - 1, p.Y) ||
                        !IsSquareBlank(board, p.X + 1, p.Y) ||
                        !IsSquareBlank(board, p.X, p.Y - 1) ||
                        !IsSquareBlank(board, p.X, p.Y + 1))
                    {
                        touchesExisting = true;
                        break;
                    }
                }
                if (!touchesExisting) return false;
            }

            var formedWords = ExtractFormedWords(board, wm.Placements, placementMap);
            if (formedWords.Count == 0) return false;

            foreach (var wordCells in formedWords)
            {
                var wordStr = new string(wordCells.Select(c => c.letter).ToArray());
                if (!ScrabbleDictionary.IsValidWord(wordStr))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsSquareBlank(IBoard board, int x, int y)
        {
            if (!board.CheckBounds(x, y)) return true;
            var piece = board.PieceAt(x, y);
            return piece == null || piece.Value == BlankPiece.BlankValue;
        }

        public static bool IsBoardEmpty(IBoard board)
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

        private static List<List<(int x, int y, char letter, bool isNew)>> ExtractFormedWords(
            IBoard board,
            IReadOnlyList<TilePlacement> placements,
            Dictionary<(int x, int y), char> placementMap)
        {
            var words = new List<List<(int x, int y, char letter, bool isNew)>>();
            if (placements.Count == 0) return words;

            if (placements.Count == 1)
            {
                var single = placements[0];
                var hWord = ExtractLineWord(board, placementMap, single.X, single.Y, 1, 0);
                if (hWord.Count >= 2) words.Add(hWord);
                var vWord = ExtractLineWord(board, placementMap, single.X, single.Y, 0, 1);
                if (vWord.Count >= 2) words.Add(vWord);
                return words;
            }

            bool isHorizontal = placements.All(p => p.Y == placements[0].Y);
            int primaryDx = isHorizontal ? 1 : 0;
            int primaryDy = isHorizontal ? 0 : 1;
            int crossDx = isHorizontal ? 0 : 1;
            int crossDy = isHorizontal ? 1 : 0;

            var primary = ExtractLineWord(board, placementMap, placements[0].X, placements[0].Y, primaryDx, primaryDy);
            if (primary.Count >= 2)
            {
                words.Add(primary);
            }

            foreach (var p in placements)
            {
                var cross = ExtractLineWord(board, placementMap, p.X, p.Y, crossDx, crossDy);
                if (cross.Count >= 2)
                {
                    words.Add(cross);
                }
            }

            return words;
        }

        private static List<(int x, int y, char letter, bool isNew)> ExtractLineWord(
            IBoard board,
            Dictionary<(int x, int y), char> placementMap,
            int startX,
            int startY,
            int dx,
            int dy)
        {
            int x = startX;
            int y = startY;

            while (TryGetLetterAt(board, placementMap, x - dx, y - dy, out _, out _))
            {
                x -= dx;
                y -= dy;
            }

            var cells = new List<(int x, int y, char letter, bool isNew)>();
            while (TryGetLetterAt(board, placementMap, x, y, out char letter, out bool isNew))
            {
                cells.Add((x, y, letter, isNew));
                x += dx;
                y += dy;
            }

            return cells;
        }

        private static bool TryGetLetterAt(
            IBoard board,
            Dictionary<(int x, int y), char> placementMap,
            int x,
            int y,
            out char letter,
            out bool isNew)
        {
            if (placementMap.TryGetValue((x, y), out letter))
            {
                isNew = true;
                return true;
            }

            if (board.CheckBounds(x, y))
            {
                var piece = board.PieceAt(x, y);
                if (piece != null && piece.Value != BlankPiece.BlankValue && piece.Value.Length > 0)
                {
                    letter = char.ToUpperInvariant(piece.Value[0]);
                    isNew = false;
                    return true;
                }
            }

            letter = '\0';
            isNew = false;
            return false;
        }

        public int CalculateMoveScore(IBoard board, WordMove wm)
        {
            if (board == null || wm == null || wm.IsPass || wm.Placements.Count == 0)
            {
                return 0;
            }

            var placementMap = new Dictionary<(int x, int y), char>();
            foreach (var p in wm.Placements)
            {
                placementMap[(p.X, p.Y)] = p.Letter;
            }

            var formedWords = ExtractFormedWords(board, wm.Placements, placementMap);
            int totalScore = 0;

            foreach (var wordCells in formedWords)
            {
                int wordScore = 0;
                int wordMultiplier = 1;

                foreach (var (x, y, letter, isNew) in wordCells)
                {
                    int letterPts = GetLetterPoints(letter);
                    if (isNew)
                    {
                        string bonus = GetBonusAt(x, y, board.Width, board.Height);
                        switch (bonus)
                        {
                            case "DL":
                                letterPts *= DoubleBonuses ? 3 : 2;
                                break;
                            case "TL":
                                letterPts *= DoubleBonuses ? 5 : 3;
                                break;
                            case "DW":
                                wordMultiplier *= DoubleBonuses ? 3 : 2;
                                break;
                            case "TW":
                                wordMultiplier *= DoubleBonuses ? 4 : 3;
                                break;
                        }
                    }
                    wordScore += letterPts;
                }

                totalScore += wordScore * wordMultiplier;
            }

            if (wm.Placements.Count == RackSize)
            {
                totalScore += 50;
            }

            return totalScore;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            return ApplyMove(board, player, (IMove)location);
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!(move is WordMove wm)) return false;
            if (!IsValidMoveInternal(board, player, wm)) return false;

            if (wm.IsPass)
            {
                _consecutivePasses++;
                return true;
            }

            _consecutivePasses = 0;
            int movePoints = CalculateMoveScore(board, wm);

            foreach (var p in wm.Placements)
            {
                board.PieceAt(p.X, p.Y, new Piece(p.Letter.ToString()));
            }

            if (player != null)
            {
                _scores[player] = GetScore(player) + movePoints;
                if (_racks.TryGetValue(player, out var rack))
                {
                    foreach (var p in wm.Placements)
                    {
                        rack.Remove(p.Letter);
                    }
                    DrawTiles(player, RackSize);
                }
            }

            return true;
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

            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);

            bool rackEmptied = _tileBag.Count == 0 &&
                ((_racks.TryGetValue(p1, out var r1) && r1.Count == 0) ||
                 (_racks.TryGetValue(p2, out var r2) && r2.Count == 0));

            if (_consecutivePasses < 2 && !rackEmptied)
            {
                return Enumerable.Empty<IPlayer>();
            }

            int s1 = GetScore(p1);
            int s2 = GetScore(p2);

            if (s1 > s2) return new[] { p1 };
            if (s2 > s1) return new[] { p2 };
            return players;
        }
    }
}
