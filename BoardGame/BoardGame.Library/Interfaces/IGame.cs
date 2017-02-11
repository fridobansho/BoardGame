namespace BoardGame.Library.Interfaces
{
    using System.Collections.Generic;
    using Enumerations;

    public interface IGame
    {
        Status Status { get; }

        IBoard Board { get; }

        IEnumerable<IPlayer> Players { get; }

        bool DoTurns();
    }
}