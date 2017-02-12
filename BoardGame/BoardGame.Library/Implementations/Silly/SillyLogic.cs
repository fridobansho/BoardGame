namespace BoardGame.Library.Implementations.Silly
{
    using Interfaces;
    using System.Collections.Generic;
    using System.Linq;
    using Enumerations;

    public class SillyLogic : IGameLogic
    {
        public IEnumerable<IPlayer> Players { get; }

        public IEnumerable<IPiece> PlayerPieces { get; }

        public Status Status { get; private set; }

        public SillyLogic() : this(new[] { new SillyPlayer() })
        {
        }

        public SillyLogic(IEnumerable<IPlayer> players)
        {
            Players = players;
            PlayerPieces = Enumerable.Repeat(Piece.Blank, players.Count());
        }

        public IEnumerable<IPlayer> DoTurn(IBoard board, IEnumerable<IPlayer> players)
        {
            return Enumerable.Repeat(players.First(), 1);
        }
    }
}
