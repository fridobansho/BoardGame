namespace BoardGame.Library.Implementations.Breakthrough
{
    using System;
    using Interfaces;

    public class RandomBreakthroughPlayer : IPlayer
    {
        private readonly Random _random = new();

        public string Name { get; }
        public string SideSymbol { get; set; } = "B";

        public RandomBreakthroughPlayer(string name = "Breakthrough Bot (Easy)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var moves = BreakthroughLogic.GetValidMovesForSide(board, SideSymbol);
            if (moves.Count == 0) return new PieceMove(0, 0, 0, 0);
            return moves[_random.Next(moves.Count)];
        }
    }
}
