namespace BoardGame.Library.Implementations.Mancala
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class MancalaLogic : IMoveGameLogic, IScoredGameLogic, ICustomStateGameLogic
    {
        public const int PitCountPerSide = 6;

        private bool _isMisere = false;

        public int InitialStonesPerPit { get; set; } = 4;
        public bool IsMisere
        {
            get => _isMisere;
            set
            {
                _isMisere = value;
                foreach (var player in PlayerPieces.Keys)
                {
                    if (player is SmartMancalaPlayer sp) sp.IsMisere = value;
                }
            }
        }
        public bool LastMoveEarnedExtraTurn { get; private set; } = false;

        private readonly Dictionary<IPlayer, int> _scores = new();

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public MancalaLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
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

            BindPlayer(list[0], 0);
            BindPlayer(list[1], 1);
        }

        private void BindPlayer(IPlayer player, int playerIndex)
        {
            if (player is SmartMancalaPlayer sp)
            {
                sp.PlayerIndex = playerIndex;
                sp.IsMisere = IsMisere;
            }
            else if (player is RandomAiPlayer rp)
            {
                rp.Logic = this;
            }
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

        public void InitializeBoard(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            LastMoveEarnedExtraTurn = false;

            string initStr = Math.Clamp(InitialStonesPerPit, 1, 12).ToString();
            for (int x = 0; x < PitCountPerSide && x < board.Width; x++)
            {
                for (int y = 0; y < 2 && y < board.Height; y++)
                {
                    board.PieceAt(x, y, new Piece(initStr));
                }
            }

            foreach (var key in _scores.Keys.ToList())
            {
                _scores[key] = 0;
            }
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            return PlayerPieces[player];
        }

        public static int GetPitCount(IBoard board, int x, int y)
        {
            if (board == null || !board.CheckBounds(x, y)) return 0;
            return int.TryParse(board.PieceAt(x, y)?.Value, out int count) ? Math.Max(0, count) : 0;
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board == null || location == null) return false;
            return board.CheckBounds(location.X, location.Y) && GetPitCount(board, location.X, location.Y) > 0;
        }

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            if (board == null || player == null || move is not ILocation loc) return false;
            if (!PlayerPieces.TryGetValue(player, out var piece)) return false;
            int expectedY = piece.Value == "P1" ? 1 : 0;
            return loc.Y == expectedY && board.CheckBounds(loc.X, loc.Y) && GetPitCount(board, loc.X, loc.Y) > 0;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
            => ApplyMove(board, player, (IMove)location);

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!IsValidMove(board, player, move)) return false;
            var loc = (ILocation)move;
            bool isP1 = PlayerPieces[player].Value == "P1";
            var players = PlayerPieces.Keys.ToList();
            var p1 = players[0];
            var p2 = players[1];

            int stones = GetPitCount(board, loc.X, loc.Y);
            board.PieceAt(loc.X, loc.Y, new Piece("0"));

            // Ring indices: 0..5 = P1 pits (x=0..5,y=1), 6 = P1 Store, 7..12 = P2 pits (x=5..0,y=0), 13 = P2 Store
            int pos = isP1 ? loc.X : (12 - loc.X);
            int skipStore = isP1 ? 13 : 6;
            int ownStore = isP1 ? 6 : 13;

            while (stones > 0)
            {
                pos = (pos + 1) % 14;
                if (pos == skipStore) continue;

                stones--;
                if (pos == 6)
                {
                    _scores[p1] = GetScore(p1) + 1;
                }
                else if (pos == 13)
                {
                    _scores[p2] = GetScore(p2) + 1;
                }
                else if (pos <= 5)
                {
                    int c = GetPitCount(board, pos, 1) + 1;
                    board.PieceAt(pos, 1, new Piece(c.ToString()));
                }
                else
                {
                    int pitX = 12 - pos;
                    int c = GetPitCount(board, pitX, 0) + 1;
                    board.PieceAt(pitX, 0, new Piece(c.ToString()));
                }
            }

            LastMoveEarnedExtraTurn = (pos == ownStore);

            // Capture rule: last stone in own empty pit (count now == 1) with non-empty opposite pit
            if (isP1 && pos >= 0 && pos <= 5 && GetPitCount(board, pos, 1) == 1)
            {
                int opp = GetPitCount(board, pos, 0);
                if (opp > 0)
                {
                    board.PieceAt(pos, 1, new Piece("0"));
                    board.PieceAt(pos, 0, new Piece("0"));
                    _scores[p1] = GetScore(p1) + 1 + opp;
                }
            }
            else if (!isP1 && pos >= 7 && pos <= 12)
            {
                int pitX = 12 - pos;
                if (GetPitCount(board, pitX, 0) == 1)
                {
                    int opp = GetPitCount(board, pitX, 1);
                    if (opp > 0)
                    {
                        board.PieceAt(pitX, 0, new Piece("0"));
                        board.PieceAt(pitX, 1, new Piece("0"));
                        _scores[p2] = GetScore(p2) + 1 + opp;
                    }
                }
            }

            // Sweep remaining stones if either row is empty
            int p1Sum = Enumerable.Range(0, PitCountPerSide).Sum(x => GetPitCount(board, x, 1));
            int p2Sum = Enumerable.Range(0, PitCountPerSide).Sum(x => GetPitCount(board, x, 0));
            if (p1Sum == 0 || p2Sum == 0)
            {
                _scores[p1] = GetScore(p1) + p1Sum;
                _scores[p2] = GetScore(p2) + p2Sum;
                for (int x = 0; x < PitCountPerSide; x++)
                {
                    board.PieceAt(x, 1, new Piece("0"));
                    board.PieceAt(x, 0, new Piece("0"));
                }
                LastMoveEarnedExtraTurn = false;
            }

            return true;
        }

        public bool HasValidMoves(IBoard board)
        {
            for (int x = 0; x < PitCountPerSide; x++)
            {
                if (GetPitCount(board, x, 0) > 0 || GetPitCount(board, x, 1) > 0) return true;
            }
            return false;
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (players == null) throw new ArgumentNullException(nameof(players));

            var list = players.ToList();
            if (list.Count != 2) return Enumerable.Empty<IPlayer>();

            if (HasValidMoves(board)) return Enumerable.Empty<IPlayer>();

            int s1 = GetScore(list[0]);
            int s2 = GetScore(list[1]);
            if (s1 == s2) return list;

            bool p1Wins = IsMisere ? s1 < s2 : s1 > s2;
            return p1Wins ? new[] { list[0] } : new[] { list[1] };
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            return new Dictionary<string, string>
            {
                ["extraTurn"] = LastMoveEarnedExtraTurn ? "1" : "0"
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state != null && state.TryGetValue("extraTurn", out var val))
            {
                LastMoveEarnedExtraTurn = val == "1";
            }
        }
    }
}
