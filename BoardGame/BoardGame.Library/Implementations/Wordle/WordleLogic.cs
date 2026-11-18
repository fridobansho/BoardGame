namespace BoardGame.Library.Implementations.Wordle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;
    using Scrabble;

    public class WordleLogic : IMoveGameLogic, IScoredGameLogic, ICustomStateGameLogic
    {
        public const int WordLength = 5;
        public const int MaxGuesses = 6;

        private static readonly List<string> FiveLetterWords = ScrabbleDictionary.Words
            .Where(w => w.Length == WordLength && w.All(c => c >= 'A' && c <= 'Z'))
            .OrderBy(w => w, StringComparer.Ordinal)
            .ToList();

        private readonly int? _fixedSeed;
        private readonly Dictionary<IPlayer, int> _scores = new();
        private string _secretWord = "CRANE";
        private int _currentRow = 0;
        private bool _isSolved = false;
        private IPlayer _winner;

        public bool AllowAnyLetters { get; set; } = false;
        public string SecretWord => _secretWord;
        public int CurrentRow => _currentRow;
        public bool IsSolved => _isSolved;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public WordleLogic(int? seed = null)
        {
            _fixedSeed = seed;
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public static int GetDailySeed(DateTime? utcDate = null)
        {
            var dt = utcDate ?? DateTime.UtcNow;
            return int.Parse(dt.ToString("yyyyMMdd"));
        }

        public static WordleLogic CreateDaily(DateTime? utcDate = null)
        {
            return new WordleLogic(GetDailySeed(utcDate));
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

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }

            _currentRow = 0;
            _isSolved = false;
            _winner = null;

            foreach (var key in _scores.Keys.ToList())
            {
                _scores[key] = 0;
            }

            if (FiveLetterWords.Count > 0)
            {
                var rng = _fixedSeed.HasValue ? new Random(_fixedSeed.Value) : new Random();
                _secretWord = FiveLetterWords[rng.Next(FiveLetterWords.Count)];
            }
            else
            {
                _secretWord = "CRANE";
            }
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            return PlayerPieces[player];
        }

        public bool IsValidMove(IBoard board, ILocation location) => false;

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location) => false;

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            if (board == null || player == null || _isSolved || _currentRow >= MaxGuesses) return false;
            if (move is not WordMove wm || wm.IsPass || wm.Placements.Count != WordLength) return false;

            var ordered = wm.Placements.OrderBy(p => p.X).ToList();
            for (int i = 0; i < WordLength; i++)
            {
                if (ordered[i].X != i) return false;
                char c = char.ToUpperInvariant(ordered[i].Letter);
                if (c < 'A' || c > 'Z') return false;
            }

            string guess = new string(ordered.Select(p => char.ToUpperInvariant(p.Letter)).ToArray());
            return AllowAnyLetters || ScrabbleDictionary.IsValidWord(guess);
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!IsValidMove(board, player, move)) return false;
            var ordered = ((WordMove)move).Placements.OrderBy(p => p.X).ToList();
            string guess = new string(ordered.Select(p => char.ToUpperInvariant(p.Letter)).ToArray());

            char[] status = new char[WordLength];
            int[] remainingCounts = new int[26];

            // First pass: exact matches ('G')
            for (int i = 0; i < WordLength; i++)
            {
                if (guess[i] == _secretWord[i])
                {
                    status[i] = 'G';
                }
                else
                {
                    remainingCounts[_secretWord[i] - 'A']++;
                }
            }

            // Second pass: present elsewhere ('Y') or absent ('B')
            for (int i = 0; i < WordLength; i++)
            {
                if (status[i] == 'G') continue;
                int idx = guess[i] - 'A';
                if (remainingCounts[idx] > 0)
                {
                    status[i] = 'Y';
                    remainingCounts[idx]--;
                }
                else
                {
                    status[i] = 'B';
                }
            }

            for (int i = 0; i < WordLength; i++)
            {
                board.PieceAt(i, _currentRow, new Piece($"{status[i]}{guess[i]}"));
            }

            _currentRow++;
            if (guess == _secretWord)
            {
                _isSolved = true;
                _winner = player;
                int pts = Math.Max(250, 1000 - (_currentRow - 1) * 150);
                _scores[player] = GetScore(player) + pts;
            }

            return true;
        }

        public bool HasValidMoves(IBoard board) => !_isSolved && _currentRow < MaxGuesses;

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (players == null) throw new ArgumentNullException(nameof(players));

            var list = players.ToList();
            if (list.Count != 2) return Enumerable.Empty<IPlayer>();

            if (_isSolved)
            {
                return _winner != null && list.Contains(_winner) ? new[] { _winner } : new[] { list[0] };
            }

            if (_currentRow >= MaxGuesses)
            {
                return list;
            }

            return Enumerable.Empty<IPlayer>();
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            return new Dictionary<string, string>
            {
                ["secretWord"] = _secretWord,
                ["currentRow"] = _currentRow.ToString(),
                ["isSolved"] = _isSolved ? "1" : "0"
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (state.TryGetValue("secretWord", out var word) && word.Length == WordLength)
            {
                _secretWord = word.ToUpperInvariant();
            }
            if (state.TryGetValue("currentRow", out var rowStr) && int.TryParse(rowStr, out int row))
            {
                _currentRow = Math.Clamp(row, 0, MaxGuesses);
            }
            if (state.TryGetValue("isSolved", out var solvedStr))
            {
                _isSolved = solvedStr == "1";
            }
        }
    }
}
