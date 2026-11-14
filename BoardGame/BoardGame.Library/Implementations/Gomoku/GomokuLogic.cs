namespace BoardGame.Library.Implementations.Gomoku
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class GomokuLogic : IGameLogic
    {
        public const int WinLength = 5;

        private static readonly (int dx, int dy)[] Directions =
        {
            (1, 0),  // Horizontal
            (0, 1),  // Vertical
            (1, 1),  // Diagonal down-right
            (1, -1)  // Diagonal up-right
        };

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public GomokuLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            PlayerPieces.Add(players.ElementAt(0), BlackPiece.Black);
            PlayerPieces.Add(players.ElementAt(1), WhitePiece.White);
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (players.Count() != 2) throw new ArgumentOutOfRangeException(nameof(players));
        }

        public void InitializeBoard(IBoard board)
        {
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (location != null && board != null && board.CheckBounds(location.X, location.Y))
            {
                var piece = board.PieceAt(location.X, location.Y);
                return piece != null && piece.Value == BlankPiece.BlankValue;
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
            if (PlayerPieces.TryGetValue(player, out var piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        public bool ValidMoves(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (IsValidMove(board, new Location(x, y)))
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

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue)
                    {
                        continue;
                    }

                    foreach (var (dx, dy) in Directions)
                    {
                        if (HasFiveInARow(board, x, y, dx, dy, piece.Value))
                        {
                            var winner = PlayerPieces.FirstOrDefault(kvp => kvp.Value.Value == piece.Value).Key;
                            if (winner != null)
                            {
                                return new[] { winner };
                            }
                        }
                    }
                }
            }

            if (!ValidMoves(board))
            {
                return players;
            }

            return Enumerable.Empty<IPlayer>();
        }

        private static bool HasFiveInARow(IBoard board, int startX, int startY, int dx, int dy, string value)
        {
            int endX = startX + (WinLength - 1) * dx;
            int endY = startY + (WinLength - 1) * dy;
            if (!board.CheckBounds(endX, endY))
            {
                return false;
            }

            for (int step = 1; step < WinLength; step++)
            {
                var next = board.PieceAt(startX + step * dx, startY + step * dy);
                if (next == null || next.Value != value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
