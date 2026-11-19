namespace BoardGame.Library.Implementations.Hex
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HexLogic : IGameLogic
    {
        private static readonly (int dx, int dy)[] HexNeighbors =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1), (1, -1), (-1, 1)
        };

        private bool _isMisere = false;

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

        public HexLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);
            PlayerPieces.Add(p1, BlackPiece.Black);
            PlayerPieces.Add(p2, WhitePiece.White);

            UpdateBoundPlayers();
        }

        private void UpdateBoundPlayers()
        {
            foreach (var kvp in PlayerPieces)
            {
                if (kvp.Key is SmartHexPlayer h)
                {
                    h.AssignedSymbol = kvp.Value.Value;
                    h.IsMisere = IsMisere;
                }
                else if (kvp.Key is RandomAiPlayer r)
                {
                    r.Logic = this;
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
            if (board == null || location == null || !board.CheckBounds(location.X, location.Y))
            {
                return false;
            }

            var piece = board.PieceAt(location.X, location.Y);
            return piece != null && piece.Value == BlankPiece.BlankValue;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location))
            {
                return false;
            }

            board.PieceAt(location.X, location.Y, GetPiece(player));
            return true;
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (PlayerPieces.TryGetValue(player, out var piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);

            if (HasConnectedPath(board, GetPiece(p1).Value, topToBottom: true))
            {
                return IsMisere ? new[] { p2 } : new[] { p1 };
            }

            if (HasConnectedPath(board, GetPiece(p2).Value, topToBottom: false))
            {
                return IsMisere ? new[] { p1 } : new[] { p2 };
            }

            return Enumerable.Empty<IPlayer>();
        }

        internal static bool HasConnectedPath(IBoard board, string symbol, bool topToBottom)
        {
            var queue = new Queue<(int x, int y)>();
            var visited = new HashSet<(int x, int y)>();

            if (topToBottom)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, 0)?.Value == symbol)
                    {
                        queue.Enqueue((x, 0));
                        visited.Add((x, 0));
                    }
                }
            }
            else
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(0, y)?.Value == symbol)
                    {
                        queue.Enqueue((0, y));
                        visited.Add((0, y));
                    }
                }
            }

            while (queue.Count > 0)
            {
                var (cx, cy) = queue.Dequeue();
                if (topToBottom && cy == board.Height - 1) return true;
                if (!topToBottom && cx == board.Width - 1) return true;

                foreach (var (dx, dy) in HexNeighbors)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (board.CheckBounds(nx, ny) && !visited.Contains((nx, ny)) &&
                        board.PieceAt(nx, ny)?.Value == symbol)
                    {
                        visited.Add((nx, ny));
                        queue.Enqueue((nx, ny));
                    }
                }
            }

            return false;
        }
    }
}
