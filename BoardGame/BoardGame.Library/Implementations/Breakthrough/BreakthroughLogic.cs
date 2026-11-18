namespace BoardGame.Library.Implementations.Breakthrough
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class BreakthroughLogic : IMoveGameLogic
    {
        public const int DefaultSize = 8;

        public bool IsMisere { get; set; } = false;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public BreakthroughLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            var list = players.ToList();
            if (list.Count != 2) throw new ArgumentOutOfRangeException(nameof(players));

            PlayerPieces.Clear();
            PlayerPieces[list[0]] = new Piece("W");
            PlayerPieces[list[1]] = new Piece("B");

            BindPlayer(list[0], "W");
            BindPlayer(list[1], "B");
        }

        private static void BindPlayer(IPlayer player, string side)
        {
            if (player is RandomBreakthroughPlayer rp) rp.SideSymbol = side;
            else if (player is SmartBreakthroughPlayer sp) sp.SideSymbol = side;
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

            // Player 2 ("B") on top 2 rows moving downward (+Y)
            for (int y = 0; y < 2 && y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.PieceAt(x, y, new Piece("B"));
                }
            }

            // Player 1 ("W") on bottom 2 rows moving upward (-Y)
            for (int y = Math.Max(0, board.Height - 2); y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    board.PieceAt(x, y, new Piece("W"));
                }
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
            if (board == null || player == null || move is not PieceMove pm) return false;
            if (!PlayerPieces.TryGetValue(player, out var piece)) return false;
            return IsValidPieceMove(board, piece.Value, pm);
        }

        public static bool IsValidPieceMove(IBoard board, string sideSymbol, PieceMove pm)
        {
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return false;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return false;

            var source = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            if (source != sideSymbol) return false;

            int expectedDy = sideSymbol == "W" ? -1 : 1;
            int dy = pm.ToY - pm.FromY;
            int dx = pm.ToX - pm.FromX;
            if (dy != expectedDy || Math.Abs(dx) > 1) return false;

            var target = board.PieceAt(pm.ToX, pm.ToY)?.Value ?? BlankPiece.BlankValue;
            if (dx == 0)
            {
                return target == BlankPiece.BlankValue;
            }

            return target != sideSymbol;
        }

        public static List<PieceMove> GetValidMovesForSide(IBoard board, string sideSymbol)
        {
            var moves = new List<PieceMove>();
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return moves;

            int dy = sideSymbol == "W" ? -1 : 1;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y)?.Value != sideSymbol) continue;
                    int ny = y + dy;
                    if (ny < 0 || ny >= board.Height) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var pm = new PieceMove(x, y, x + dx, ny);
                        if (IsValidPieceMove(board, sideSymbol, pm))
                        {
                            moves.Add(pm);
                        }
                    }
                }
            }
            return moves;
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (!IsValidMove(board, player, move)) return false;
            var pm = (PieceMove)move;
            var piece = PlayerPieces[player];
            board.PieceAt(pm.FromX, pm.FromY, BlankPiece.Blank);
            board.PieceAt(pm.ToX, pm.ToY, piece);
            return true;
        }

        public bool HasValidMoves(IBoard board)
        {
            return GetValidMovesForSide(board, "W").Count > 0 || GetValidMovesForSide(board, "B").Count > 0;
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (players == null) throw new ArgumentNullException(nameof(players));

            var list = players.ToList();
            if (list.Count != 2) return Enumerable.Empty<IPlayer>();

            var p1 = list[0];
            var p2 = list[1];
            bool wReached = false;
            bool bReached = false;
            int wCount = 0;
            int bCount = 0;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    string val = board.PieceAt(x, y)?.Value ?? BlankPiece.BlankValue;
                    if (val == "W")
                    {
                        wCount++;
                        if (y == 0) wReached = true;
                    }
                    else if (val == "B")
                    {
                        bCount++;
                        if (y == board.Height - 1) bReached = true;
                    }
                }
            }

            if (wReached || bCount == 0)
            {
                return IsMisere ? new[] { p2 } : new[] { p1 };
            }

            if (bReached || wCount == 0)
            {
                return IsMisere ? new[] { p1 } : new[] { p2 };
            }

            return Enumerable.Empty<IPlayer>();
        }
    }
}
