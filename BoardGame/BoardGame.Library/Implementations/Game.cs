namespace BoardGame.Library.Implementations
{
    using System.Collections.Generic;
    using Enumerations;
    using Interfaces;
    using Silly;
    using System.Linq;
    using System;

    public class Game : IGame
    {
        public IGameLogic GameLogic { get; }

        public Status Status { get; private set; }

        public IBoard Board { get; }

        public IEnumerable<IPlayer> Players { get; }

        public Game() : this(new Board(), new[] { new SillyPlayer(), new SillyPlayer() }, new SillyLogic())
        {
        }

        public Game(IBoard board, IEnumerable<IPlayer> players, IGameLogic gameLogic)
        {
            Board = board;
            Players = players;
            GameLogic = gameLogic;
            GameLogic.MapPieces(players);
            Status = Status.InProgress;
        }

        public bool DoTurns()
        {
            bool validMoves = false;
            foreach(var player in Players)
            {
                var location = player.GetMove(Board);

                if(GameLogic.IsValidMove(Board, location))
                {
                    var piece = GameLogic.GetPiece(player);
                    Board.PieceAt(location, piece);
                    validMoves = true;
                }
                if (GetWinners().Any())
                {
                    break;
                }

            }
            if (GetWinners().Any())
            {
                Status = Status.Finished;
                return false;
            }
            return validMoves;
        }

        public IEnumerable<IPlayer> GetWinners()
        {
            return GameLogic.GetWinners(Board, Players);
        }
    }
}
