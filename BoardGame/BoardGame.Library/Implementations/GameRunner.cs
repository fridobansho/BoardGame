namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System.Collections.Generic;

    public class GameRunner : IGameRunner
    {
        public IGame Game { get; }

        public IBoardRenderer Renderer { get; }

        public GameRunner() : this(new Game())
        {
        }

        public GameRunner(IGame game) : this(game, null)
        {
        }

        public GameRunner(IGame game, IBoardRenderer renderer)
        {
            Game = game;
            Renderer = renderer;
        }

        public IEnumerable<IPlayer> RunGame()
        {
            Renderer?.Render(Game.Board);
            int turnsWithNoValidMoves = 0;
            while((Game.Status != Enumerations.Status.Finished) && (turnsWithNoValidMoves < 3))
            {
                var validMoves = Game.DoTurns();
                Renderer?.Render(Game.Board);

                if (!validMoves) turnsWithNoValidMoves++;
            }
            return Game.GetWinners();
        }
    }
}
