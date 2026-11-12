namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System.Collections.Generic;
    using System.Threading;

    public class GameRunner : IGameRunner
    {
        public IGame Game { get; }

        public IBoardRenderer Renderer { get; }

        public int TurnDelayMs { get; }

        public GameRunner() : this(new Game())
        {
        }

        public GameRunner(IGame game) : this(game, null, 0)
        {
        }

        public GameRunner(IGame game, IBoardRenderer renderer) : this(game, renderer, 0)
        {
        }

        public GameRunner(IGame game, IBoardRenderer renderer, int turnDelayMs)
        {
            Game = game;
            Renderer = renderer;
            TurnDelayMs = turnDelayMs > 0 ? turnDelayMs : 0;
        }

        public IEnumerable<IPlayer> RunGame()
        {
            Renderer?.Render(Game.Board);
            if (TurnDelayMs > 0) Thread.Sleep(TurnDelayMs);

            int turnsWithNoValidMoves = 0;
            while((Game.Status != Enumerations.Status.Finished) && (turnsWithNoValidMoves < 3))
            {
                var validMoves = Game.DoTurns();
                Renderer?.Render(Game.Board);
                if (TurnDelayMs > 0 && Game.Status != Enumerations.Status.Finished)
                {
                    Thread.Sleep(TurnDelayMs);
                }

                if (!validMoves) turnsWithNoValidMoves++;
            }
            return Game.GetWinners();
        }
    }
}
