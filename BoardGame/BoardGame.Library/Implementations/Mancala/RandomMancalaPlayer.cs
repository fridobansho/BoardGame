namespace BoardGame.Library.Implementations.Mancala
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class RandomMancalaPlayer : IPlayer
    {
        private readonly Random _random = new();

        public string Name { get; }
        public int PlayerIndex { get; set; } = 1;

        public RandomMancalaPlayer(string name = "Mancala Bot (Easy)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            int y = PlayerIndex == 0 ? 1 : 0;
            var valid = new List<Location>();
            for (int x = 0; x < MancalaLogic.PitCountPerSide; x++)
            {
                if (MancalaLogic.GetPitCount(board, x, y) > 0)
                {
                    valid.Add(new Location(x, y));
                }
            }
            if (valid.Count == 0) return new Location(0, y);
            return valid[_random.Next(valid.Count)];
        }
    }
}
