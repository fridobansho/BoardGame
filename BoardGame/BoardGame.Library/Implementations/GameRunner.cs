namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;
    using System.Collections.Generic;

    public class GameRunner : IGameRunner
    {
        public IGame Game { get; }

        public GameRunner() : this(new Game())
        {
        }

        public GameRunner(IGame game)
        {
            Game = game;
        }

        public IEnumerable<IPlayer> RunGame()
        {
            throw new NotImplementedException();
        }
    }
}
