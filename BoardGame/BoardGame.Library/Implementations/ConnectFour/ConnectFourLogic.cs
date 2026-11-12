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

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public ConnectFourLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces[players.ElementAt(0)] = RedPiece.Red;
            PlayerPieces[players.ElementAt(1)] = YellowPiece.Yellow;
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

            // Column location.X must have at least one blank spot (checked at top row y=0)
            var topPiece = board.PieceAt(location.X, 0);
            return topPiece != null && topPiece.Value == BlankPiece.BlankValue;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location)) return false;

            var piece = GetPiece(player);

            // Gravity: drop piece into the lowest available row in column location.X
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

            // Check for 4 in a row across the board
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue) continue;

                    // 1. Horizontal right (x, x+1, x+2, x+3)
                    if (x + 3 < board.Width &&
                        piece.Value == board.PieceAt(x + 1, y).Value &&
                        piece.Value == board.PieceAt(x + 2, y).Value &&
                        piece.Value == board.PieceAt(x + 3, y).Value)
                    {
                        return new[] { players.First(p => PlayerPieces[p].Value == piece.Value) };
                    }

                    // 2. Vertical down (y, y+1, y+2, y+3)
                    if (y + 3 < board.Height &&
                        piece.Value == board.PieceAt(x, y + 1).Value &&
                        piece.Value == board.PieceAt(x, y + 2).Value &&
                        piece.Value == board.PieceAt(x, y + 3).Value)
                    {
                        return new[] { players.First(p => PlayerPieces[p].Value == piece.Value) };
                    }

                    // 3. Diagonal down-right (x+i, y+i)
                    if (x + 3 < board.Width && y + 3 < board.Height &&
                        piece.Value == board.PieceAt(x + 1, y + 1).Value &&
                        piece.Value == board.PieceAt(x + 2, y + 2).Value &&
                        piece.Value == board.PieceAt(x + 3, y + 3).Value)
                    {
                        return new[] { players.First(p => PlayerPieces[p].Value == piece.Value) };
                    }

                    // 4. Diagonal up-right (x+i, y-i)
                    if (x + 3 < board.Width && y - 3 >= 0 &&
                        piece.Value == board.PieceAt(x + 1, y - 1).Value &&
                        piece.Value == board.PieceAt(x + 2, y - 2).Value &&
                        piece.Value == board.PieceAt(x + 3, y - 3).Value)
                    {
                        return new[] { players.First(p => PlayerPieces[p].Value == piece.Value) };
                    }
                }
            }

            // Check for draw: top row full with no blank pieces
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
