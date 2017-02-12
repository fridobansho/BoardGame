namespace BoardGame.Library.Implementations
{
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
            int turnsWithNoValidMoves = 0;
            while((Game.Status != Enumerations.Status.Finished) && (turnsWithNoValidMoves < 3))
            {
                var validMoves = Game.DoTurns();

                if (!validMoves) turnsWithNoValidMoves++;
            }
            return Game.GetWinners();
        }
    }
}
