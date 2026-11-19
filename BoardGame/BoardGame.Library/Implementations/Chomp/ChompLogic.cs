namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ChompLogic : IGameLogic, ICustomStateGameLogic
    {
        private bool _isRowOnlyNim = false;
        private bool _isMisere = true;

        public bool IsRowOnlyNim
        {
            get => _isRowOnlyNim;
            set
            {
                _isRowOnlyNim = value;
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

        public IPlayer LastPlayerWhoMoved { get; private set; }

        public bool PoisonEaten { get; private set; }

        public ChompLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
            PoisonEaten = false;
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var sharedPiece = IsRowOnlyNim ? StonePiece.Stone : ChocolatePiece.Chocolate;
            foreach (var player in players)
            {
                PlayerPieces[player] = sharedPiece;
            }
            UpdateBoundPlayers();
        }

        private void UpdateBoundPlayers()
        {
            foreach (var player in PlayerPieces.Keys)
            {
                if (player is SmartChompPlayer sp)
                {
                    sp.IsRowOnlyNim = IsRowOnlyNim;
                    sp.IsMisere = IsMisere;
                }
                else if (player is RandomAiPlayer rp)
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

            PoisonEaten = false;
            LastPlayerWhoMoved = null;

            if (IsRowOnlyNim)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    int stonesInRow = Math.Min(board.Width, 1 + y * 2);
                    for (int x = 0; x < board.Width; x++)
                    {
                        board.PieceAt(x, y, x < stonesInRow ? StonePiece.Stone : BlankPiece.Blank);
                    }
                }
                return;
            }

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (x == 0 && y == 0)
                    {
                        board.PieceAt(x, y, PoisonPiece.Poison);
                    }
                    else
                    {
                        board.PieceAt(x, y, ChocolatePiece.Chocolate);
                    }
                }
            }
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (location == null || board == null) return false;
            if (!board.CheckBounds(location.X, location.Y)) return false;

            var piece = board.PieceAt(location.X, location.Y);
            // Any uneaten block/stone (chocolate, poison, or stone) is a valid move
            return piece != null && piece.Value != BlankPiece.BlankValue;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location))
            {
                return false;
            }

            LastPlayerWhoMoved = player;

            if (IsRowOnlyNim)
            {
                for (int x = location.X; x < board.Width; x++)
                {
                    board.PieceAt(x, location.Y, BlankPiece.Blank);
                }
                PoisonEaten = !HasAnyPiece(board);
                return true;
            }

            PoisonEaten = (location.X == 0 && location.Y == 0) || board.PieceAt(0, 0)?.Value == BlankPiece.BlankValue;

            // Chomp: eating at (X, Y) removes all blocks at (x' >= X, y' >= Y)
            for (int x = location.X; x < board.Width; x++)
            {
                for (int y = location.Y; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }

            return true;
        }

        private static bool HasAnyPiece(IBoard board)
        {
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val != null && val != BlankPiece.BlankValue)
                    {
                        return true;
                    }
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

            if (IsRowOnlyNim)
            {
                if (HasAnyPiece(board) || LastPlayerWhoMoved == null)
                {
                    return Enumerable.Empty<IPlayer>();
                }

                if (IsMisere)
                {
                    return players.Where(p => !ReferenceEquals(p, LastPlayerWhoMoved));
                }
                return new[] { LastPlayerWhoMoved };
            }

            var topLeft = board?.PieceAt(0, 0);
            bool poisonMissing = topLeft == null || topLeft.Value == BlankPiece.BlankValue;
            if (PoisonEaten && poisonMissing && LastPlayerWhoMoved != null)
            {
                var winner = IsMisere
                    ? players.First(p => !ReferenceEquals(p, LastPlayerWhoMoved))
                    : LastPlayerWhoMoved;
                return Enumerable.Repeat(winner, 1);
            }

            return Enumerable.Empty<IPlayer>();
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            int idx = -1;
            if (players != null && LastPlayerWhoMoved != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (ReferenceEquals(players[i], LastPlayerWhoMoved))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            return new Dictionary<string, string>
            {
                ["poisonEaten"] = PoisonEaten ? "1" : "0",
                ["lastPlayerIdx"] = idx.ToString()
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state == null) return;
            if (state.TryGetValue("poisonEaten", out var pe))
            {
                PoisonEaten = pe == "1";
            }
            if (state.TryGetValue("lastPlayerIdx", out var idxStr) && int.TryParse(idxStr, out int idx))
            {
                LastPlayerWhoMoved = (players != null && idx >= 0 && idx < players.Count) ? players[idx] : null;
            }
        }
    }
}
