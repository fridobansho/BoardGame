namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Enumerations;

    public class TicTacToeLogic : IGameLogic
    {
        public IEnumerable<IPlayer> Players { get; }

        public IEnumerable<IPiece> PlayerPieces { get; }

        public Status Status { get; private set; }
        
        public TicTacToeLogic(IEnumerable<IPlayer> players)
        {
            PlayerPieces = new[] { XPiece.X, OPiece.O };
            if (players.Count() != PlayerPieces.Count()) throw new ArgumentOutOfRangeException("players.Count()");
            Players = players;
            Status = Status.InProgress;
        }

        public IEnumerable<IPlayer> DoTurn(IBoard board, IEnumerable<IPlayer> players)
        {
            foreach(var player in players)
            {
                var location = player.GetMove(board);
            }

            return Enumerable.Empty<IPlayer>();
        }
    }
}
