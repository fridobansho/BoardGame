namespace BoardGame.Library.Implementations.Checkers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class CheckersLogic : IMoveGameLogic, ICustomStateGameLogic
    {
        public const int DefaultSize = 8;
        public const int DrawMoveLimit = 80;

        private bool _mandatoryCapture = true;
        private bool _isGiveaway = false;
        private bool _isBreakthrough = false;
        private string _lastMovedSide;

        public bool MandatoryCapture
        {
            get => _mandatoryCapture;
            set
            {
                _mandatoryCapture = value;
                UpdateBoundPlayers();
            }
        }

        public bool IsGiveaway
        {
            get => _isGiveaway;
            set
            {
                _isGiveaway = value;
                UpdateBoundPlayers();
            }
        }

        public bool IsBreakthrough
        {
            get => _isBreakthrough;
            set
            {
                _isBreakthrough = value;
                UpdateBoundPlayers();
            }
        }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public int MovesWithoutCaptureOrPromotion { get; set; }

        public CheckersLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);
            PlayerPieces.Add(p1, RedCheckerPiece.Red);
            PlayerPieces.Add(p2, BlackCheckerPiece.Black);

            UpdateBoundPlayers();
        }

        private void UpdateBoundPlayers()
        {
            foreach (var kvp in PlayerPieces)
            {
                AssignPlayerSide(kvp.Key, kvp.Value.Value);
            }
        }

        private void AssignPlayerSide(IPlayer player, string sideSymbol)
        {
            if (player is SmartCheckersPlayer smartPlayer)
            {
                smartPlayer.AssignedSymbol = sideSymbol;
                smartPlayer.MandatoryCapture = MandatoryCapture;
                smartPlayer.IsGiveaway = IsGiveaway;
                smartPlayer.IsBreakthrough = IsBreakthrough;
            }
            else if (player is RandomAiPlayer randomPlayer)
            {
                randomPlayer.Logic = this;
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

            MovesWithoutCaptureOrPromotion = 0;
            _lastMovedSide = null;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }

            if (IsBreakthrough)
            {
                // Black ("B") on top 2 rows moving downward (+Y)
                for (int y = 0; y < 2 && y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.PieceAt(x, y, BlackCheckerPiece.Black);
                    }
                }

                // Red ("R") on bottom 2 rows moving upward (-Y)
                for (int y = Math.Max(0, board.Height - 2); y < board.Height; y++)
                {
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.PieceAt(x, y, RedCheckerPiece.Red);
                    }
                }
                return;
            }

            // Black starts on the top 3 rows and moves downward (+Y)
            for (int y = 0; y < 3 && y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if ((x + y) % 2 == 1)
                    {
                        board.PieceAt(x, y, BlackCheckerPiece.Black);
                    }
                }
            }

            // Red starts on the bottom 3 rows and moves upward (-Y)
            for (int y = Math.Max(0, board.Height - 3); y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if ((x + y) % 2 == 1)
                    {
                        board.PieceAt(x, y, RedCheckerPiece.Red);
                    }
                }
            }
        }

        public static string InferSide(IBoard board)
        {
            int redCount = 0;
            int blackCount = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (IsPlayerPiece(val, RedCheckerPiece.Red.Value)) redCount++;
                    else if (IsPlayerPiece(val, BlackCheckerPiece.Black.Value)) blackCount++;
                }
            }

            return redCount >= blackCount ? RedCheckerPiece.Red.Value : BlackCheckerPiece.Black.Value;
        }

        public static bool IsPlayerPiece(string pieceVal, string playerBaseSymbol)
        {
            if (string.IsNullOrEmpty(pieceVal) || string.IsNullOrEmpty(playerBaseSymbol)) return false;
            if (playerBaseSymbol == RedCheckerPiece.Red.Value)
            {
                return pieceVal == RedCheckerPiece.Red.Value || pieceVal == RedKingPiece.RedKing.Value;
            }
            if (playerBaseSymbol == BlackCheckerPiece.Black.Value)
            {
                return pieceVal == BlackCheckerPiece.Black.Value || pieceVal == BlackKingPiece.BlackKing.Value;
            }
            return false;
        }

        public static bool IsKing(string pieceVal)
        {
            return pieceVal == RedKingPiece.RedKing.Value || pieceVal == BlackKingPiece.BlackKing.Value;
        }

        public static string GetSideOfPiece(string pieceVal)
        {
            if (pieceVal == RedCheckerPiece.Red.Value || pieceVal == RedKingPiece.RedKing.Value)
            {
                return RedCheckerPiece.Red.Value;
            }
            if (pieceVal == BlackCheckerPiece.Black.Value || pieceVal == BlackKingPiece.BlackKing.Value)
            {
                return BlackCheckerPiece.Black.Value;
            }
            return null;
        }

        private static string GetOpponentSide(string sideSymbol)
        {
            return sideSymbol == RedCheckerPiece.Red.Value
                ? BlackCheckerPiece.Black.Value
                : RedCheckerPiece.Red.Value;
        }

        private static IEnumerable<(int dx, int dy)> GetMoveDirections(string pieceVal)
        {
            if (pieceVal == RedCheckerPiece.Red.Value)
            {
                yield return (-1, -1);
                yield return (1, -1);
            }
            else if (pieceVal == BlackCheckerPiece.Black.Value)
            {
                yield return (-1, 1);
                yield return (1, 1);
            }
            else if (IsKing(pieceVal))
            {
                yield return (-1, -1);
                yield return (1, -1);
                yield return (-1, 1);
                yield return (1, 1);
            }
        }

        public static bool IsValidBreakthroughMove(IBoard board, string sideSymbol, PieceMove pm)
        {
            if (board == null || pm == null || string.IsNullOrEmpty(sideSymbol)) return false;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return false;

            var source = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            if (!IsPlayerPiece(source, sideSymbol)) return false;

            int expectedDy = sideSymbol == RedCheckerPiece.Red.Value ? -1 : 1;
            int dy = pm.ToY - pm.FromY;
            int dx = pm.ToX - pm.FromX;
            if (dy != expectedDy || Math.Abs(dx) > 1) return false;

            var target = board.PieceAt(pm.ToX, pm.ToY)?.Value ?? BlankPiece.BlankValue;
            if (dx == 0)
            {
                return target == BlankPiece.BlankValue;
            }

            return !IsPlayerPiece(target, sideSymbol);
        }

        private static List<PieceMove> GetBreakthroughMovesForSide(IBoard board, string sideSymbol)
        {
            var moves = new List<PieceMove>();
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return moves;

            int dy = sideSymbol == RedCheckerPiece.Red.Value ? -1 : 1;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (!IsPlayerPiece(board.PieceAt(x, y)?.Value, sideSymbol)) continue;
                    int ny = y + dy;
                    if (ny < 0 || ny >= board.Height) continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        var pm = new PieceMove(x, y, x + dx, ny);
                        if (IsValidBreakthroughMove(board, sideSymbol, pm))
                        {
                            moves.Add(pm);
                        }
                    }
                }
            }
            return moves;
        }

        public static List<PieceMove> GetCaptureMovesForSide(IBoard board, string sideSymbol)
        {
            var moves = new List<PieceMove>();
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return moves;

            var workBoard = CloneBoard(board);
            for (int x = 0; x < workBoard.Width; x++)
            {
                for (int y = 0; y < workBoard.Height; y++)
                {
                    var val = workBoard.PieceAt(x, y)?.Value;
                    if (!IsPlayerPiece(val, sideSymbol)) continue;

                    var sequences = new List<List<ILocation>>();
                    var initialPath = new List<ILocation> { new Location(x, y) };
                    ExploreCaptureSequences(workBoard, x, y, val, sideSymbol, initialPath, sequences);

                    foreach (var seq in sequences)
                    {
                        var start = seq[0];
                        var end = seq[seq.Count - 1];
                        moves.Add(new PieceMove(start, end, seq));
                    }
                }
            }

            return moves;
        }

        private static void ExploreCaptureSequences(
            IBoard board,
            int x,
            int y,
            string pieceVal,
            string sideSymbol,
            List<ILocation> currentPath,
            List<List<ILocation>> results)
        {
            string oppSide = GetOpponentSide(sideSymbol);
            bool foundContinuation = false;

            foreach (var (dx, dy) in GetMoveDirections(pieceVal))
            {
                int midX = x + dx;
                int midY = y + dy;
                int landX = x + 2 * dx;
                int landY = y + 2 * dy;

                if (!board.CheckBounds(landX, landY) || !board.CheckBounds(midX, midY)) continue;

                var midVal = board.PieceAt(midX, midY)?.Value;
                var landVal = board.PieceAt(landX, landY)?.Value;

                if (!IsPlayerPiece(midVal, oppSide)) continue;
                if (landVal != BlankPiece.BlankValue) continue;

                foundContinuation = true;

                // Mutate board in place and restore on backtrack so intermediate captures cannot be jumped twice
                var origStart = board.PieceAt(x, y);
                var origMid = board.PieceAt(midX, midY);
                var origLand = board.PieceAt(landX, landY);

                board.PieceAt(x, y, BlankPiece.Blank);
                board.PieceAt(midX, midY, BlankPiece.Blank);

                bool reachesKingRow = (sideSymbol == RedCheckerPiece.Red.Value && landY == 0)
                                   || (sideSymbol == BlackCheckerPiece.Black.Value && landY == board.Height - 1);
                bool promotedNow = !IsKing(pieceVal) && reachesKingRow;
                IPiece movedPiece = ResolvePieceAfterMove(pieceVal, sideSymbol, landY, board.Height);
                board.PieceAt(landX, landY, movedPiece);

                var nextPath = new List<ILocation>(currentPath) { new Location(landX, landY) };

                if (promotedNow)
                {
                    results.Add(nextPath);
                }
                else
                {
                    ExploreCaptureSequences(board, landX, landY, movedPiece.Value, sideSymbol, nextPath, results);
                }

                board.PieceAt(x, y, origStart);
                board.PieceAt(midX, midY, origMid);
                board.PieceAt(landX, landY, origLand);
            }

            if (!foundContinuation && currentPath.Count > 1)
            {
                results.Add(currentPath);
            }
        }

        internal static bool HasStepMovesForSide(IBoard board, string sideSymbol)
        {
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return false;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (!IsPlayerPiece(val, sideSymbol)) continue;

                    foreach (var (dx, dy) in GetMoveDirections(val))
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (board.CheckBounds(nx, ny) && board.PieceAt(nx, ny)?.Value == BlankPiece.BlankValue)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static bool HasValidMovesForSide(IBoard board, string sideSymbol, bool mandatoryCapture = true, bool isBreakthrough = false)
        {
            if (isBreakthrough)
            {
                return GetBreakthroughMovesForSide(board, sideSymbol).Count > 0;
            }
            if (HasStepMovesForSide(board, sideSymbol) && !mandatoryCapture)
            {
                return true;
            }
            if (GetCaptureMovesForSide(board, sideSymbol).Count > 0)
            {
                return true;
            }
            return HasStepMovesForSide(board, sideSymbol);
        }

        public static List<PieceMove> GetValidMovesForSide(IBoard board, string sideSymbol, bool mandatoryCapture = true, bool isBreakthrough = false)
        {
            if (isBreakthrough)
            {
                return GetBreakthroughMovesForSide(board, sideSymbol);
            }

            var captures = GetCaptureMovesForSide(board, sideSymbol);
            if (mandatoryCapture && captures.Count > 0)
            {
                return captures;
            }

            var stepMoves = new List<PieceMove>(captures);
            if (board == null || string.IsNullOrEmpty(sideSymbol)) return stepMoves;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (!IsPlayerPiece(val, sideSymbol)) continue;

                    foreach (var (dx, dy) in GetMoveDirections(val))
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (board.CheckBounds(nx, ny) && board.PieceAt(nx, ny)?.Value == BlankPiece.BlankValue)
                        {
                            stepMoves.Add(new PieceMove(x, y, nx, ny));
                        }
                    }
                }
            }

            return stepMoves;
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board == null || !(location is PieceMove pm)) return false;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return false;

            var pieceVal = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            var side = GetSideOfPiece(pieceVal);
            if (side == null) return false;

            return FindMatchingValidMove(board, side, pm, MandatoryCapture, IsBreakthrough) != null;
        }

        public bool IsValidMove(IBoard board, IPlayer player, IMove move)
        {
            if (board == null || player == null || !(move is PieceMove pm)) return false;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return false;

            var side = GetPiece(player).Value;
            var pieceVal = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            if (!IsPlayerPiece(pieceVal, side)) return false;

            return FindMatchingValidMove(board, side, pm, MandatoryCapture, IsBreakthrough) != null;
        }

        private static PieceMove FindMatchingValidMove(IBoard board, string sideSymbol, PieceMove pm, bool mandatoryCapture = true, bool isBreakthrough = false)
        {
            if (isBreakthrough)
            {
                return IsValidBreakthroughMove(board, sideSymbol, pm)
                    ? new PieceMove(pm.FromX, pm.FromY, pm.ToX, pm.ToY)
                    : null;
            }

            var validMoves = GetValidMovesForSide(board, sideSymbol, mandatoryCapture);
            if (validMoves.Count == 0) return null;

            if (pm.Path != null && pm.Path.Count > 2)
            {
                foreach (var candidate in validMoves)
                {
                    if (candidate.Path.Count == pm.Path.Count &&
                        candidate.Path.Zip(pm.Path, (a, b) => a.X == b.X && a.Y == b.Y).All(eq => eq))
                    {
                        return candidate;
                    }
                }
                return null;
            }

            foreach (var candidate in validMoves)
            {
                if (candidate.FromX == pm.FromX && candidate.FromY == pm.FromY &&
                    candidate.ToX == pm.ToX && candidate.ToY == pm.ToY)
                {
                    return candidate;
                }
            }

            if (Math.Abs(pm.ToX - pm.FromX) == 2 && Math.Abs(pm.ToY - pm.FromY) == 2)
            {
                foreach (var candidate in validMoves)
                {
                    if (candidate.FromX == pm.FromX && candidate.FromY == pm.FromY &&
                        candidate.Path.Count >= 2 &&
                        candidate.Path[1].X == pm.ToX && candidate.Path[1].Y == pm.ToY)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        public PieceMove ResolveMove(IBoard board, IPlayer player, PieceMove pm)
        {
            if (board == null || player == null || pm == null) return null;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return null;

            var side = GetPiece(player).Value;
            var pieceVal = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            if (!IsPlayerPiece(pieceVal, side)) return null;

            return FindMatchingValidMove(board, side, pm, MandatoryCapture, IsBreakthrough);
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            return ApplyMove(board, player, (IMove)location);
        }

        public bool ApplyMove(IBoard board, IPlayer player, IMove move)
        {
            if (board == null || player == null || !(move is PieceMove pm)) return false;
            if (!board.CheckBounds(pm.FromX, pm.FromY) || !board.CheckBounds(pm.ToX, pm.ToY)) return false;

            var side = GetPiece(player).Value;
            var pieceVal = board.PieceAt(pm.FromX, pm.FromY)?.Value;
            if (!IsPlayerPiece(pieceVal, side)) return false;

            var resolvedMove = FindMatchingValidMove(board, side, pm, MandatoryCapture, IsBreakthrough);
            if (resolvedMove == null) return false;

            if (IsBreakthrough)
            {
                bool isDiagCapture = (board.PieceAt(resolvedMove.ToX, resolvedMove.ToY)?.Value ?? BlankPiece.BlankValue) != BlankPiece.BlankValue;
                board.PieceAt(resolvedMove.FromX, resolvedMove.FromY, BlankPiece.Blank);
                IPiece moved = side == RedCheckerPiece.Red.Value ? RedCheckerPiece.Red : BlackCheckerPiece.Black;
                board.PieceAt(resolvedMove.ToX, resolvedMove.ToY, moved);
                MovesWithoutCaptureOrPromotion = isDiagCapture ? 0 : MovesWithoutCaptureOrPromotion + 1;
                _lastMovedSide = side;
                return true;
            }

            bool isCapture = Math.Abs(resolvedMove.Path[1].X - resolvedMove.Path[0].X) == 2;
            string currentVal = pieceVal;

            if (isCapture)
            {
                for (int i = 0; i < resolvedMove.Path.Count - 1; i++)
                {
                    var cur = resolvedMove.Path[i];
                    var next = resolvedMove.Path[i + 1];
                    int midX = (cur.X + next.X) / 2;
                    int midY = (cur.Y + next.Y) / 2;

                    board.PieceAt(cur.X, cur.Y, BlankPiece.Blank);
                    board.PieceAt(midX, midY, BlankPiece.Blank);
                    var nextPiece = ResolvePieceAfterMove(currentVal, side, next.Y, board.Height);
                    board.PieceAt(next.X, next.Y, nextPiece);
                    currentVal = nextPiece.Value;
                }
                MovesWithoutCaptureOrPromotion = 0;
            }
            else
            {
                board.PieceAt(resolvedMove.FromX, resolvedMove.FromY, BlankPiece.Blank);
                var nextPiece = ResolvePieceAfterMove(currentVal, side, resolvedMove.ToY, board.Height);
                board.PieceAt(resolvedMove.ToX, resolvedMove.ToY, nextPiece);

                bool promoted = nextPiece.Value != currentVal;
                MovesWithoutCaptureOrPromotion = promoted ? 0 : MovesWithoutCaptureOrPromotion + 1;
            }

            _lastMovedSide = side;
            return true;
        }

        private static IPiece ResolvePieceAfterMove(string pieceVal, string sideSymbol, int toY, int boardHeight)
        {
            if (sideSymbol == RedCheckerPiece.Red.Value)
            {
                if (pieceVal == RedKingPiece.RedKing.Value || toY == 0)
                {
                    return RedKingPiece.RedKing;
                }
                return RedCheckerPiece.Red;
            }

            if (pieceVal == BlackKingPiece.BlackKing.Value || toY == boardHeight - 1)
            {
                return BlackKingPiece.BlackKing;
            }
            return BlackCheckerPiece.Black;
        }

        public IPiece GetPiece(IPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (PlayerPieces.TryGetValue(player, out var piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException(nameof(player));
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            ValidatePlayers(players);

            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);

            int redPieces = 0;
            int blackPieces = 0;
            int redMaterial = 0;
            int blackMaterial = 0;
            bool redReachedHome = false;
            bool blackReachedHome = false;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val == RedCheckerPiece.Red.Value)
                    {
                        redPieces++;
                        redMaterial += 1;
                        if (y == 0) redReachedHome = true;
                    }
                    else if (val == RedKingPiece.RedKing.Value)
                    {
                        redPieces++;
                        redMaterial += 2;
                    }
                    else if (val == BlackCheckerPiece.Black.Value)
                    {
                        blackPieces++;
                        blackMaterial += 1;
                        if (y == board.Height - 1) blackReachedHome = true;
                    }
                    else if (val == BlackKingPiece.BlackKing.Value)
                    {
                        blackPieces++;
                        blackMaterial += 2;
                    }
                }
            }

            if (IsBreakthrough)
            {
                if (redReachedHome || blackPieces == 0)
                {
                    return IsGiveaway ? new[] { p2 } : new[] { p1 };
                }
                if (blackReachedHome || redPieces == 0)
                {
                    return IsGiveaway ? new[] { p1 } : new[] { p2 };
                }
            }
            else
            {
                if (blackPieces == 0)
                {
                    return IsGiveaway ? new[] { p2 } : new[] { p1 };
                }

                if (redPieces == 0)
                {
                    return IsGiveaway ? new[] { p1 } : new[] { p2 };
                }
            }

            if (_lastMovedSide == RedCheckerPiece.Red.Value)
            {
                if (!HasValidMovesForSide(board, BlackCheckerPiece.Black.Value, MandatoryCapture, IsBreakthrough))
                {
                    return IsGiveaway ? new[] { p2 } : new[] { p1 };
                }
            }
            else if (_lastMovedSide == BlackCheckerPiece.Black.Value)
            {
                if (!HasValidMovesForSide(board, RedCheckerPiece.Red.Value, MandatoryCapture, IsBreakthrough))
                {
                    return IsGiveaway ? new[] { p1 } : new[] { p2 };
                }
            }
            else
            {
                if (!HasValidMovesForSide(board, BlackCheckerPiece.Black.Value, MandatoryCapture, IsBreakthrough))
                {
                    return IsGiveaway ? new[] { p2 } : new[] { p1 };
                }
                if (!HasValidMovesForSide(board, RedCheckerPiece.Red.Value, MandatoryCapture, IsBreakthrough))
                {
                    return IsGiveaway ? new[] { p1 } : new[] { p2 };
                }
            }

            if (MovesWithoutCaptureOrPromotion >= DrawMoveLimit)
            {
                if (redMaterial > blackMaterial) return IsGiveaway ? new[] { p2 } : new[] { p1 };
                if (blackMaterial > redMaterial) return IsGiveaway ? new[] { p1 } : new[] { p2 };
                return players;
            }

            return Enumerable.Empty<IPlayer>();
        }

        internal static IBoard CloneBoard(IBoard board)
        {
            var clone = new Board(board.Width, board.Height);
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    clone.PieceAt(x, y, board.PieceAt(x, y));
                }
            }
            return clone;
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            return new Dictionary<string, string>
            {
                ["noProgressMoves"] = MovesWithoutCaptureOrPromotion.ToString(),
                ["lastMovedSide"] = _lastMovedSide ?? ""
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (state.TryGetValue("noProgressMoves", out var val) && int.TryParse(val, out var count))
            {
                MovesWithoutCaptureOrPromotion = Math.Max(0, count);
            }
            if (state.TryGetValue("lastMovedSide", out var side))
            {
                _lastMovedSide = string.IsNullOrEmpty(side) ? null : side;
            }
        }
    }
}
