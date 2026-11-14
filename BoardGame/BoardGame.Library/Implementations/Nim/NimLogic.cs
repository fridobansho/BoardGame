namespace BoardGame.Library.Implementations.Nim
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class NimLogic : IGameLogic
    {
        private IPlayer _lastPlayer;

        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public NimLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Clear();
            PlayerPieces.Add(players.ElementAt(0), StonePiece.Stone);
            PlayerPieces.Add(players.ElementAt(1), StonePiece.Stone);
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
                for (int x = 0; x < stonesInRow; x++)
                {
                    board.PieceAt(x, y, StonePiece.Stone);
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

            return _lastPlayer != null ? new[] { _lastPlayer } : Enumerable.Empty<IPlayer>();
        }
    }
}
