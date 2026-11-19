namespace BoardGame.Library.Implementations.Wordle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class WordleLogic : IMoveGameLogic, IScoredGameLogic, ICustomStateGameLogic
    {
        public const int WordLength = 5;
        public const int MaxGuesses = 6;

        private static readonly DateTime DailyEpoch = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly List<string> ShuffledAnswers = BuildShuffledAnswers();

        private readonly int? _fixedSeed;
        private readonly DateTime? _dailyDate;
        private readonly Dictionary<IPlayer, int> _scores = new();
        private string _secretWord = "CRANE";
        private bool _hasInitialized = false;
        private int _currentRow = 0;
        private bool _isSolved = false;
        private IPlayer _winner;

        public bool AllowAnyLetters { get; set; } = false;
        public string SecretWord => _secretWord;
        public int CurrentRow => _currentRow;
        public bool IsSolved => _isSolved;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public WordleLogic(int? seed = null)
            : this(seed, null)
        {
        }

        private WordleLogic(int? seed, DateTime? dailyDate)
        {
            _fixedSeed = seed;
            _dailyDate = dailyDate;
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public static int GetDailySeed(DateTime? utcDate = null)
        {
            var raw = utcDate ?? DateTime.UtcNow;
            var dt = raw.Kind == DateTimeKind.Local ? raw.ToUniversalTime() : raw;
            return dt.Year * 10000 + dt.Month * 100 + dt.Day;
        }

        public static WordleLogic CreateDaily(DateTime? utcDate = null)
        {
            var raw = utcDate ?? DateTime.UtcNow;
            var dt = raw.Kind == DateTimeKind.Local ? raw.ToUniversalTime() : raw;
            return new WordleLogic(GetDailySeed(dt), dt.Date);
        }

        private static List<string> BuildShuffledAnswers()
        {
            var list = WordleDictionary.AnswerWords.ToList();
            if (list.Count == 0)
            {
                list.Add("CRANE");
                return list;
            }

            uint state = 0x574F5244u;
            for (int i = list.Count - 1; i > 0; i--)
            {
                state += 0x9E3779B9u;
                int j = (int)(Mix32(state) % (uint)(i + 1));
                (list[i], list[j]) = (list[j], list[i]);
            }
            return list;
        }

        private static uint Mix32(uint x)
        {
            x ^= x >> 16;
            x *= 0x85EBCA6Bu;
            x ^= x >> 13;
            x *= 0xC2B2AE35u;
            x ^= x >> 16;
            return x;
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

            if (_dailyDate.HasValue)
            {
                int dayOffset = (int)(_dailyDate.Value.Date - DailyEpoch).TotalDays;
                int idx = ((dayOffset % ShuffledAnswers.Count) + ShuffledAnswers.Count) % ShuffledAnswers.Count;
                _secretWord = ShuffledAnswers[idx];
            }
            else if (_fixedSeed.HasValue)
            {
                int idx = (int)(Mix32((uint)_fixedSeed.Value) % (uint)ShuffledAnswers.Count);
                _secretWord = ShuffledAnswers[idx];
            }
            else if (WordleDictionary.AnswerWords.Count > 0)
            {
                string nextWord;
                int attempts = 0;
                do
                {
                    nextWord = WordleDictionary.AnswerWords[Random.Shared.Next(WordleDictionary.AnswerWords.Count)];
                    attempts++;
                }
                while (_hasInitialized && WordleDictionary.AnswerWords.Count > 1 && nextWord == _secretWord && attempts < 10);
                _secretWord = nextWord;
            }
            else
            {
                _secretWord = "CRANE";
            }
            _hasInitialized = true;
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
            if (board.Width < WordLength || _currentRow >= board.Height) return false;
            if (move is not WordMove wm || wm.IsPass || wm.Placements.Count != WordLength) return false;

            var ordered = wm.Placements.OrderBy(p => p.X).ToList();
            for (int i = 0; i < WordLength; i++)
            {
                if (ordered[i].X != i || ordered[i].Y != _currentRow) return false;
                char c = char.ToUpperInvariant(ordered[i].Letter);
                if (c < 'A' || c > 'Z') return false;
            }

            string guess = new string(ordered.Select(p => char.ToUpperInvariant(p.Letter)).ToArray());
            return AllowAnyLetters || WordleDictionary.IsValidGuess(guess);
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

        public bool HasValidMoves(IBoard board) => board != null && !_isSolved && _currentRow < MaxGuesses && _currentRow < board.Height;

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
                ["secretWord"] = _secretWord,
                ["currentRow"] = _currentRow.ToString(),
                ["isSolved"] = _isSolved ? "1" : "0",
                ["winnerIdx"] = winnerIdx.ToString()
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (state.TryGetValue("secretWord", out var word) && !string.IsNullOrWhiteSpace(word))
            {
                string normalized = word.Trim().ToUpperInvariant();
                if (normalized.Length == WordLength && normalized.All(c => c >= 'A' && c <= 'Z'))
                {
                    _secretWord = normalized;
                    _hasInitialized = true;
                }
            }
            if (state.TryGetValue("currentRow", out var rowStr) && int.TryParse(rowStr, out int row))
            {
                _currentRow = Math.Clamp(row, 0, MaxGuesses);
            }
            if (state.TryGetValue("isSolved", out var solvedStr))
            {
                _isSolved = solvedStr == "1";
            }
            if (state.TryGetValue("winnerIdx", out var winStr) && int.TryParse(winStr, out int winIdx) &&
                players != null && winIdx >= 0 && winIdx < players.Count)
            {
                _winner = players[winIdx];
            }
            else
            {
                _winner = null;
            }
        }
    }
}
