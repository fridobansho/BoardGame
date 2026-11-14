namespace BoardGame.Library.Implementations.Nim
{
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class RandomNimPlayer : IPlayer
    {
        private readonly Random _random = new Random();

        public string Name { get; }

        public RandomNimPlayer(string name = "Nim Bot (Easy)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var validMoves = new List<ILocation>();
            for (int y = 0; y < board.Height; y++)
            {
                for (int x = 0; x < board.Width; x++)
                {
                    if (board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        validMoves.Add(new Location(x, y));
                    }
                }
            }

            return validMoves.Count == 0 ? null : validMoves[_random.Next(validMoves.Count)];
        }
    }
}
