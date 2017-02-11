namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;

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

        public IPlayer RunGame()
        {
            throw new NotImplementedException();
        }
    }
}
