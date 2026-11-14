namespace BoardGame.Library.Implementations.Hex
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class RandomHexPlayer : IPlayer
    {
        private readonly Random _random = new Random();

        public string Name { get; }

        public RandomHexPlayer(string name = "Hex Bot (Easy)")
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
                    if (board.PieceAt(x, y)?.Value == BlankPiece.BlankValue)
                    {
                        validMoves.Add(new Location(x, y));
                    }
                }
            }

            return validMoves.Count == 0 ? null : validMoves[_random.Next(validMoves.Count)];
        }
    }
}
