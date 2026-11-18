namespace BoardGame.Library.Implementations.Othello
{
    using Gomoku;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class OthelloLogic : IGameLogic
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (-1, -1), (0, -1), (1, -1),
            (-1,  0),          (1,  0),
            (-1,  1), (0,  1), (1,  1)
        };

        public bool FewerDiscsWins { get; set; } = false;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public OthelloLogic()
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
            if (board == null) throw new ArgumentNullException(nameof(board));
            int midX = board.Width / 2;
            int midY = board.Height / 2;

            board.PieceAt(midX - 1, midY - 1, WhitePiece.White);
            board.PieceAt(midX, midY, WhitePiece.White);
            board.PieceAt(midX, midY - 1, BlackPiece.Black);
            board.PieceAt(midX - 1, midY, BlackPiece.Black);
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board == null || location == null || !board.CheckBounds(location.X, location.Y))
            {
                return false;
            }

            var piece = board.PieceAt(location.X, location.Y);
            if (piece == null || piece.Value != BlankPiece.BlankValue)
            {
                return false;
            }

            return GetFlips(board, location.X, location.Y, BlackPiece.Black.Value).Count > 0
                || GetFlips(board, location.X, location.Y, WhitePiece.White.Value).Count > 0;
        }

        public bool IsValidMove(IBoard board, IPlayer player, ILocation location)
        {
            if (board == null || player == null || location == null || !board.CheckBounds(location.X, location.Y))
            {
                return false;
            }

            var piece = board.PieceAt(location.X, location.Y);
            if (piece == null || piece.Value != BlankPiece.BlankValue)
            {
                return false;
            }

            var myPiece = GetPiece(player);
            return GetFlips(board, location.X, location.Y, myPiece.Value).Count > 0;
        }

        public bool HasValidMoves(IBoard board, IPlayer player)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (IsValidMove(board, player, new Location(x, y)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, player, location))
            {
                return false;
            }

            var myPiece = GetPiece(player);
            var flips = GetFlips(board, location.X, location.Y, myPiece.Value);
            board.PieceAt(location.X, location.Y, myPiece);
            foreach (var (fx, fy) in flips)
            {
                board.PieceAt(fx, fy, myPiece);
            }

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

            if (HasValidMoves(board, p1) || HasValidMoves(board, p2))
            {
                return Enumerable.Empty<IPlayer>();
            }

            int p1Count = 0;
            int p2Count = 0;
            var p1Val = GetPiece(p1).Value;
            var p2Val = GetPiece(p2).Value;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val == p1Val) p1Count++;
                    else if (val == p2Val) p2Count++;
                }
            }

            if (FewerDiscsWins)
            {
                if (p1Count < p2Count) return new[] { p1 };
                if (p2Count < p1Count) return new[] { p2 };
                return players;
            }

            if (p1Count > p2Count) return new[] { p1 };
            if (p2Count > p1Count) return new[] { p2 };
            return players;
        }

        public static List<(int x, int y)> GetFlips(IBoard board, int startX, int startY, string myValue)
        {
            var allFlips = new List<(int x, int y)>();
            string oppValue = myValue == BlackPiece.Black.Value ? WhitePiece.White.Value : BlackPiece.Black.Value;

            foreach (var (dx, dy) in Directions)
            {
                var line = new List<(int x, int y)>();
                int x = startX + dx;
                int y = startY + dy;

                while (board.CheckBounds(x, y))
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue)
                    {
                        break;
                    }

                    if (piece.Value == oppValue)
                    {
                        line.Add((x, y));
                    }
                    else if (piece.Value == myValue)
                    {
                        allFlips.AddRange(line);
                        break;
                    }

                    x += dx;
                    y += dy;
                }
            }

            return allFlips;
        }
    }
}
