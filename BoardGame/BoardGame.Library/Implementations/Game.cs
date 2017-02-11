namespace BoardGame.Library.Implementations
{
    using System.Collections.Generic;
    using Enumerations;
    using Interfaces;

    public class Game : IGame
    {
        public Status Status { get; private set; }

        public IBoard Board { get; private set; }

        public IEnumerable<IPlayer> Players { get; private set; }

        public Game(IBoard board, IEnumerable<IPlayer> players)
        {
            Board = board;
            Players = players;
            Status = Status.InProgress;
        }
    }
}
