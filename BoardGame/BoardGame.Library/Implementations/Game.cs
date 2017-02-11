namespace BoardGame.Library.Implementations
{
    using System.Collections.Generic;
    using Enumerations;
    using Interfaces;

    public class Game : IGame
    {
        public Status Status { get; }

        public IBoard Board { get; }

        public IEnumerable<IPlayer> Players { get; }

        public Game() : this(new Board(), new[] { new SillyPlayer(), new SillyPlayer() })
        {
        }

        public Game(IBoard board, IEnumerable<IPlayer> players)
        {
            Board = board;
            Players = players;
            Status = Status.InProgress;
        }

        public bool DoTurns()
        {
            bool validMoves = false;
            foreach(var player in Players)
            {
                bool validMove = false;
                int tries = 0;
                ILocation location;
                while ((!validMove) && (tries < 3))
                {
                    location = player.GetMove(Board);

                    if(Board.CheckBounds(location))
                    {
                        validMove = true;
                        validMoves = true;
                    }
                    tries++;
                }
            }
            return validMoves;
        }
    }
}
