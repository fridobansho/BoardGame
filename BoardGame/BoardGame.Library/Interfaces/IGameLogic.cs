namespace BoardGame.Library.Interfaces
{
    using Enumerations;
    using System.Collections.Generic;

    public interface IGameLogic
    {
        Status Status { get; }

        IEnumerable<IPlayer> Players { get; }

        IEnumerable<IPiece> PlayerPieces { get; }

        IEnumerable<IPlayer> DoTurn(IBoard board, IEnumerable<IPlayer> players);
    }
}
