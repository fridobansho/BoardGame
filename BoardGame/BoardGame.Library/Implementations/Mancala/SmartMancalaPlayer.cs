namespace BoardGame.Library.Implementations.Mancala
{
    using System;
    using Interfaces;

    public class SmartMancalaPlayer : IPlayer
    {
        private readonly Random _random = new();

        public string Name { get; }
        public int PlayerIndex { get; set; } = 1;

        public SmartMancalaPlayer(string name = "Mancala Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            int y = PlayerIndex == 0 ? 1 : 0;
            int bestScore = int.MinValue;
            Location bestMove = new Location(0, y);

            for (int x = 0; x < MancalaLogic.PitCountPerSide; x++)
            {
                int stones = MancalaLogic.GetPitCount(board, x, y);
                if (stones <= 0) continue;

                int score = _random.Next(0, 3);
                // Distance to own store: for P1 (y=1) is (6 - x), for P2 (y=0) is (x + 1)
                int distToStore = PlayerIndex == 0 ? (6 - x) : (x + 1);

                // Extra turn landing directly in store
                if (stones == distToStore || (stones > distToStore && (stones - distToStore) % 13 == 0))
                {
                    score += 500;
                }

                // Store gain
                if (stones >= distToStore)
                {
                    score += 40;
                }

                // Simple capture check (within own row before wrapping)
                int targetX = PlayerIndex == 0 ? (x + stones) : (x - stones);
                if (targetX >= 0 && targetX < 6 && MancalaLogic.GetPitCount(board, targetX, y) == 0)
                {
                    int opp = MancalaLogic.GetPitCount(board, targetX, 1 - y);
                    if (opp > 0)
                    {
                        score += 100 + opp * 25;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = new Location(x, y);
                }
            }

            return bestMove;
        }
    }
}
