namespace BoardGame.Library.Implementations.Silly
{
    using Interfaces;
    using System.Collections.Generic;
    using System;

    public class SillyLogic : IGameLogic
    {
        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public SillyLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException("players");
            foreach (var player in players)
            {
                PlayerPieces.Add(player, Piece.Blank);
            }
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            return true;
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
            return players;
        }
    }
}
