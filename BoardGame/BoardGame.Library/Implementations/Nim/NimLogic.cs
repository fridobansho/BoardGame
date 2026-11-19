namespace BoardGame.Library.Implementations.Nim
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class NimLogic : IGameLogic, ICustomStateGameLogic
    {
        private IPlayer _lastPlayer;
        private bool _isMisere = false;

        public bool IsMisere
        {
            get => _isMisere;
            set
            {
                _isMisere = value;
                foreach (var player in PlayerPieces.Keys)
                {
                    if (player is SmartNimPlayer sp)
                    {
                        sp.IsMisere = value;
                    }
                }
            }
        }

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public NimLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            var p1 = players.ElementAt(0);
            var p2 = players.ElementAt(1);
            PlayerPieces.Add(p1, StonePiece.Stone);
            PlayerPieces.Add(p2, StonePiece.Stone);

            if (p1 is SmartNimPlayer s1) s1.IsMisere = IsMisere;
            if (p2 is SmartNimPlayer s2) s2.IsMisere = IsMisere;
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (players.Count() != 2) throw new ArgumentOutOfRangeException(nameof(players));
        }

        public void InitializeBoard(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            _lastPlayer = null;

            for (int y = 0; y < board.Height; y++)
            {
                int stonesInRow = Math.Min(board.Width, 1 + y * 2);
                for (int x = 0; x < board.Width; x++)
                {
                    board.PieceAt(x, y, x < stonesInRow ? StonePiece.Stone : BlankPiece.Blank);
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
            return piece != null && piece.Value == StonePiece.Stone.Value;
        }

        public bool ApplyMove(IBoard board, IPlayer player, ILocation location)
        {
            if (!IsValidMove(board, location))
            {
                return false;
            }

            for (int x = location.X; x < board.Width; x++)
            {
                board.PieceAt(x, location.Y, BlankPiece.Blank);
            }

            _lastPlayer = player;
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

            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        return Enumerable.Empty<IPlayer>();
                    }
                }
            }

            if (_lastPlayer == null) return Enumerable.Empty<IPlayer>();
            if (IsMisere)
            {
                return players.Where(p => !ReferenceEquals(p, _lastPlayer));
            }
            return new[] { _lastPlayer };
        }

        public Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players)
        {
            int idx = -1;
            if (players != null && _lastPlayer != null)
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (ReferenceEquals(players[i], _lastPlayer))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            return new Dictionary<string, string>
            {
                ["lastPlayerIdx"] = idx.ToString()
            };
        }

        public void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state)
        {
            if (state != null && state.TryGetValue("lastPlayerIdx", out var idxStr) && int.TryParse(idxStr, out int idx))
            {
                _lastPlayer = (players != null && idx >= 0 && idx < players.Count) ? players[idx] : null;
            }
        }
    }
}
