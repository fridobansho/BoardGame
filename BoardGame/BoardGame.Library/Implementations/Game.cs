namespace BoardGame.Library.Implementations
{
    using System.Collections.Generic;
    using Enumerations;
    using Interfaces;
    using Silly;

    public class Game : IGame
    {
        public IGameLogic GameLogic { get; }

        public Status Status { get { return GameLogic.Status; } }

        public IBoard Board { get; }

        public IEnumerable<IPlayer> Players { get; }

        public IEnumerable<IPiece> PlayerPieces { get; }

        public Game() : this(new Board(), new[] { new SillyPlayer(), new SillyPlayer() }, new SillyLogic())
        {
        }

        public Game(IBoard board, IEnumerable<IPlayer> players, IGameLogic gameLogic)
        {
            Board = board;
            Players = players;
            GameLogic = gameLogic;
        }

        public IEnumerable<IPlayer> DoTurns()
        {
            IEnumerable<IPlayer> winners = null;
            foreach(var player in Players)
            {
                winners = GameLogic.DoTurn(Board, Players);
            }
            return winners;
        }
    }
}
