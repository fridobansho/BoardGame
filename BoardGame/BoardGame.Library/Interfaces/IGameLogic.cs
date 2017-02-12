namespace BoardGame.Library.Interfaces
{
    using System.Collections.Generic;

    public interface IGameLogic
    {
        IDictionary<IPlayer, IPiece> PlayerPieces { get; }

        void MapPieces(IEnumerable<IPlayer> players);

        bool IsValidMove(IBoard board, ILocation location);

        IPiece GetPiece(IPlayer player);

        IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players);
    }
}
