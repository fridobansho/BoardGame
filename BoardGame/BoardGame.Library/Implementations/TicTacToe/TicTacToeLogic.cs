namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TicTacToeLogic : IGameLogic
    {
        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public TicTacToeLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Add(players.ElementAt(0), XPiece.X);
            PlayerPieces.Add(players.ElementAt(1), OPiece.O);
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException("players");
            if (players.Count() != 2) throw new ArgumentOutOfRangeException("players.Count()");
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board.CheckBounds(location))
            {
                var piece = board.PieceAt(location);
                if (piece == Piece.Blank)
                {
                    return true;
                }
            }
            return false;
        }

        public IPiece GetPiece(IPlayer player)
        {
            IPiece piece;
            if (PlayerPieces.TryGetValue(player, out piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException("player");
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            return Enumerable.Empty<IPlayer>();
        }
    }
}
