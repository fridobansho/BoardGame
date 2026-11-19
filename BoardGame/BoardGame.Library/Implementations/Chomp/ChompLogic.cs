namespace BoardGame.Library.Implementations.Chomp
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ChompLogic : IGameLogic, ICustomStateGameLogic
    {
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
            // In Chomp, pieces belong to the board (chocolate / poison), not individual players.
            // Map each player to the shared Chocolate piece.
            foreach (var player in players)
            {
                PlayerPieces[player] = ChocolatePiece.Chocolate;
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
            // Any uneaten block (chocolate or poison) is a valid move
            return piece != null && piece.Value != BlankPiece.BlankValue;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location))
            {
                return false;
            }

            LastPlayerWhoMoved = player;

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

            var topLeft = board?.PieceAt(0, 0);
            bool poisonMissing = topLeft == null || topLeft.Value == BlankPiece.BlankValue;
            if (PoisonEaten && poisonMissing && LastPlayerWhoMoved != null)
            {
                // The player forced to eat (0, 0) loses; the other player wins!
                var winner = players.First(p => !ReferenceEquals(p, LastPlayerWhoMoved));
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
