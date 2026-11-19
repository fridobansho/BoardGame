namespace BoardGame.Library.Implementations.Othello
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class OthelloLogic : IMoveGameLogic
    {
        private static readonly (int dx, int dy)[] Directions =
        {
            (-1, -1), (0, -1), (1, -1),
            (-1,  0),          (1,  0),
            (-1,  1), (0,  1), (1,  1)
        };

        private bool _fewerDiscsWins = false;

        public bool FewerDiscsWins
        {
            get => _fewerDiscsWins;
            set
            {
                _fewerDiscsWins = value;
                foreach (var player in PlayerPieces.Keys)
                {
                    if (player is SmartOthelloPlayer sp)
                    {
                        sp.FewerDiscsWins = value;
                    }
                }
            }
        }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public OthelloLogic()
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

            BindPlayer(p1, BlackPiece.Black.Value);
            BindPlayer(p2, WhitePiece.White.Value);
        }

        private void BindPlayer(IPlayer player, string symbol)
        {
            if (player is SmartOthelloPlayer sp)
            {
                sp.AssignedSymbol = symbol;
                sp.FewerDiscsWins = FewerDiscsWins;
            }
            else if (player is RandomAiPlayer rp)
            {
                rp.Logic = this;
            }
        }

        internal static string InferSymbol(IBoard board)
        {
            int black = 0, white = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val == BlackPiece.Black.Value) black++;
                    else if (val == WhitePiece.White.Value) white++;
                }
            }
            return black <= white ? BlackPiece.Black.Value : WhitePiece.White.Value;
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

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            return move is ILocation loc && IsValidMove(board, player, loc);
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
            return HasAnyFlip(board, location.X, location.Y, myPiece.Value);
        }

        public bool HasValidMoves(IBoard board, IPlayer player)
        {
            if (board == null || player == null) return false;
            var myVal = GetPiece(player).Value;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value == BlankPiece.BlankValue && HasAnyFlip(board, x, y, myVal))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            return move is ILocation loc && ApplyMove(board, player, loc);
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

        private static bool HasAnyFlip(IBoard board, int startX, int startY, string myValue)
        {
            string oppValue = myValue == BlackPiece.Black.Value ? WhitePiece.White.Value : BlackPiece.Black.Value;
            foreach (var (dx, dy) in Directions)
            {
                int x = startX + dx;
                int y = startY + dy;
                bool sawOpp = false;
                while (board.CheckBounds(x, y))
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value == BlankPiece.BlankValue) break;
                    if (piece.Value == oppValue) sawOpp = true;
                    else if (piece.Value == myValue)
                    {
                        if (sawOpp) return true;
                        break;
                    }
                    x += dx;
                    y += dy;
                }
            }
            return false;
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
