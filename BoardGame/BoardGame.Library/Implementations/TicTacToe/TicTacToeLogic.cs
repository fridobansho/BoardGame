namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TicTacToeLogic : IGameLogic
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (1, 0),
            (0, 1),
            (1, 1),
            (1, -1)
        };

        private int _targetLength = 0;
        private bool _exactTargetOnly = false;
        private bool _isMisere = false;

        public int TargetLength
        {
            get => _targetLength;
            set
            {
                _targetLength = value;
                UpdateBoundPlayers();
            }
        }

        public bool ExactTargetOnly
        {
            get => _exactTargetOnly;
            set
            {
                _exactTargetOnly = value;
                UpdateBoundPlayers();
            }
        }

        public bool IsMisere
        {
            get => _isMisere;
            set
            {
                _isMisere = value;
                UpdateBoundPlayers();
            }
        }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public TicTacToeLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);
            PlayerPieces.Add(p1, XPiece.X);
            PlayerPieces.Add(p2, OPiece.O);

            UpdateBoundPlayers();
        }

        private void UpdateBoundPlayers()
        {
            foreach (var kvp in PlayerPieces)
            {
                if (kvp.Key is MinimaxPlayer mp)
                {
                    mp.AssignedPiece = kvp.Value;
                    mp.TargetLength = TargetLength;
                    mp.ExactTargetOnly = ExactTargetOnly;
                    mp.IsMisere = IsMisere;
                }
                else if (kvp.Key is RandomAiPlayer rp)
                {
                    rp.Logic = this;
                }
            }
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException("players");
            if (players.Count() != 2) throw new ArgumentOutOfRangeException("players.Count()");
        }

        public int GetEffectiveTargetLength(IBoard board)
        {
            if (TargetLength > 0) return TargetLength;
            int maxDim = Math.Max(board?.Width ?? 3, board?.Height ?? 3);
            if (maxDim <= 3) return maxDim;
            if (maxDim <= 5) return 4;
            return 5;
        }

        public void InitializeBoard(IBoard board)
        {
            if (board == null) return;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (location != null && board != null && board.CheckBounds(location.X, location.Y))
            {
                var piece = board.PieceAt(location.X, location.Y);
                if (piece != null && piece.Value == BlankPiece.BlankValue)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location))
            {
                return false;
            }

            var piece = GetPiece(player);
            board.PieceAt(location.X, location.Y, piece);
            return true;
        }

        public IPiece GetPiece(IPlayer player)
        {
            IPiece piece;
            if (PlayerPieces.TryGetValue(player, out piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException("player");
        }

        public bool ValidMoves(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var location = new Location(x, y);
                    if (IsValidMove(board, location))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            var playerList = players.ToList();

            if (board.Width <= 3 && board.Height <= 3 && TargetLength == 0 && !ExactTargetOnly)
            {
                return GetStandard3x3Winners(board, players, playerList);
            }

            int target = GetEffectiveTargetLength(board);
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue) continue;

                    foreach (var (dx, dy) in Directions)
                    {
                        if (HasTargetInARow(board, x, y, dx, dy, piece.Value, target, ExactTargetOnly))
                        {
                            var winner = PlayerPieces.FirstOrDefault(kvp => kvp.Value.Value == piece.Value).Key;
                            if (winner != null)
                            {
                                return new[] { ResolveWinner(winner, playerList) };
                            }
                        }
                    }
                }
            }

            if (!ValidMoves(board)) return players;

            return Enumerable.Empty<IPlayer>();
        }

        private IEnumerable<IPlayer> GetStandard3x3Winners(IBoard board, IEnumerable<IPlayer> players, List<IPlayer> playerList)
        {
            // Check verticals
            for (int x = 0; x < board.Width; x++)
            {
                var pieces = new List<IPiece>();
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        pieces.Add(piece);
                    }
                }
                if (TryGetLineWinner(pieces, board.Height, out var winner))
                {
                    return Enumerable.Repeat(ResolveWinner(winner, playerList), 1);
                }
            }

            // Check horizontals
            for (int y = 0; y < board.Height; y++)
            {
                var pieces = new List<IPiece>();
                for (int x = 0; x < board.Width; x++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        pieces.Add(piece);
                    }
                }
                if (TryGetLineWinner(pieces, board.Width, out var winner))
                {
                    return Enumerable.Repeat(ResolveWinner(winner, playerList), 1);
                }
            }

            // Check diagonals when board is square
            if (board.Width > 0 && board.Width == board.Height)
            {
                var mainDiagonal = new List<IPiece>();
                for (int i = 0; i < board.Width; i++)
                {
                    var piece = board.PieceAt(i, i);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        mainDiagonal.Add(piece);
                    }
                }
                if (TryGetLineWinner(mainDiagonal, board.Width, out var mainWinner))
                {
                    return Enumerable.Repeat(ResolveWinner(mainWinner, playerList), 1);
                }

                var antiDiagonal = new List<IPiece>();
                for (int i = 0; i < board.Width; i++)
                {
                    var piece = board.PieceAt(i, board.Height - 1 - i);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        antiDiagonal.Add(piece);
                    }
                }
                if (TryGetLineWinner(antiDiagonal, board.Width, out var antiWinner))
                {
                    return Enumerable.Repeat(ResolveWinner(antiWinner, playerList), 1);
                }
            }

            if (!ValidMoves(board)) return players;

            return Enumerable.Empty<IPlayer>();
        }

        private IPlayer ResolveWinner(IPlayer lineMaker, List<IPlayer> playerList)
        {
            if (!IsMisere) return lineMaker;
            return playerList.First(p => !ReferenceEquals(p, lineMaker));
        }

        internal static bool HasTargetInARow(
            IBoard board,
            int startX,
            int startY,
            int dx,
            int dy,
            string value,
            int target,
            bool exactOnly)
        {
            int endX = startX + (target - 1) * dx;
            int endY = startY + (target - 1) * dy;
            if (!board.CheckBounds(endX, endY))
            {
                return false;
            }

            for (int step = 1; step < target; step++)
            {
                var next = board.PieceAt(startX + step * dx, startY + step * dy);
                if (next == null || next.Value != value)
                {
                    return false;
                }
            }

            if (exactOnly)
            {
                int prevX = startX - dx;
                int prevY = startY - dy;
                if (board.CheckBounds(prevX, prevY) && board.PieceAt(prevX, prevY)?.Value == value)
                {
                    return false;
                }

                int afterX = startX + target * dx;
                int afterY = startY + target * dy;
                if (board.CheckBounds(afterX, afterY) && board.PieceAt(afterX, afterY)?.Value == value)
                {
                    return false;
                }
            }

            return true;
        }

        private bool TryGetLineWinner(IReadOnlyCollection<IPiece> pieces, int expectedLength, out IPlayer winner)
        {
            winner = null;
            if (expectedLength <= 0 || pieces.Count != expectedLength)
            {
                return false;
            }

            var distinct = pieces.Distinct().ToList();
            if (distinct.Count == 1)
            {
                var winningValue = distinct[0].Value;
                var match = PlayerPieces.FirstOrDefault(pair => pair.Value.Value == winningValue);
                if (match.Key != null)
                {
                    winner = match.Key;
                    return true;
                }
            }

            return false;
        }
    }
}
