﻿namespace BoardGame.Library.Interfaces
{
    using System.Collections.Generic;
    using Enumerations;

    public interface IGame
    {
        IGameLogic GameLogic { get; }

        Status Status { get; }

        IBoard Board { get; }

        IEnumerable<IPlayer> Players { get; }

        IEnumerable<IPiece> PlayerPieces { get; }

        IEnumerable<IPlayer> DoTurns();
    }
}