namespace BoardGame.Library.Implementations.ConnectFour
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ConnectFourLogic : IGameLogic
    {
        public const int DefaultWidth = 7;
        public const int DefaultHeight = 6;

        private int _targetLength = 4;
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

        public ConnectFourLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);
            PlayerPieces[p1] = RedPiece.Red;
            PlayerPieces[p2] = YellowPiece.Yellow;

            UpdateBoundPlayers();
        }

        private void UpdateBoundPlayers()
        {
            foreach (var kvp in PlayerPieces)
            {
                if (kvp.Key is ConnectFourPlayer c4)
                {
                    c4.AssignedSymbol = kvp.Value.Value;
                    c4.TargetLength = TargetLength;
                    c4.IsMisere = IsMisere;
                }
                else if (kvp.Key is RandomAiPlayer rp)
                {
                    rp.Logic = this;
                }
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
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board == null || location == null) return false;
            if (!board.CheckBounds(location.X, 0)) return false;

            var topPiece = board.PieceAt(location.X, 0);
            return topPiece != null && topPiece.Value == BlankPiece.BlankValue;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location)) return false;

            var piece = GetPiece(player);

            for (int y = board.Height - 1; y >= 0; y--)
            {
                var existing = board.PieceAt(location.X, y);
                if (existing != null && existing.Value == BlankPiece.BlankValue)
                {
                    board.PieceAt(location.X, y, piece);
                    return true;
                }
            }

            return false;
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player != null && PlayerPieces.TryGetValue(player, out var piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            var playerList = players.ToList();

            int target = Math.Max(2, TargetLength);
            (int dx, int dy)[] directions = { (1, 0), (0, 1), (1, 1), (1, -1) };

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue) continue;

                    foreach (var (dx, dy) in directions)
                    {
                        if (TicTacToe.TicTacToeLogic.HasTargetInARow(board, x, y, dx, dy, piece.Value, target, exactOnly: false))
                        {
                            var maker = playerList.First(p => PlayerPieces[p].Value == piece.Value);
                            var winner = IsMisere ? playerList.First(p => !ReferenceEquals(p, maker)) : maker;
                            return new[] { winner };
                        }
                    }
                }
            }

            bool isFull = true;
            for (int x = 0; x < board.Width; x++)
            {
                if (board.PieceAt(x, 0).Value == BlankPiece.BlankValue)
                {
                    isFull = false;
                    break;
                }
            }

            if (isFull)
            {
                return players;
            }

            return Enumerable.Empty<IPlayer>();
        }
    }
}
