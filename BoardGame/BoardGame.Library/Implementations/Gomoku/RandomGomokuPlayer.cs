namespace BoardGame.Library.Implementations.Gomoku
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class RandomGomokuPlayer : IPlayer
    {
        private readonly Random _random = new Random();

        public string Name { get; }

        public RandomGomokuPlayer(string name = "Gomoku Bot (Easy)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var validMoves = new List<ILocation>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value == BlankPiece.BlankValue)
                    {
                        validMoves.Add(new Location(x, y));
                    }
                }
            }

            if (validMoves.Count == 0)
            {
                return null;
            }

            return validMoves[_random.Next(validMoves.Count)];
        }
    }
}
