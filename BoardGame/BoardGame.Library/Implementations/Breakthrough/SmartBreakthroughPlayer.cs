namespace BoardGame.Library.Implementations.Breakthrough
{
    using System;
    using System.Linq;
    using Interfaces;

    public class SmartBreakthroughPlayer : IPlayer
    {
        private readonly Random _random = new();

        public string Name { get; }
        public string SideSymbol { get; set; } = "B";

        public SmartBreakthroughPlayer(string name = "Breakthrough Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            var moves = BreakthroughLogic.GetValidMovesForSide(board, SideSymbol);
            if (moves.Count == 0) return new PieceMove(0, 0, 0, 0);

            string enemy = SideSymbol == "W" ? "B" : "W";
            int goalY = SideSymbol == "W" ? 0 : board.Height - 1;
            int enemyGoalY = SideSymbol == "W" ? board.Height - 1 : 0;

            int bestScore = int.MinValue;
            PieceMove bestMove = moves[0];

            foreach (var m in moves)
            {
                int score = _random.Next(0, 4);
                string targetVal = board.PieceAt(m.ToX, m.ToY)?.Value ?? BlankPiece.BlankValue;

                if (m.ToY == goalY)
                {
                    score += 10000;
                }

                if (targetVal == enemy)
                {
                    score += 80;
                    // Extra priority if capturing an enemy close to our home row
                    int enemyProgress = Math.Abs(m.ToY - (SideSymbol == "W" ? 0 : board.Height - 1));
                    score += Math.Max(0, (board.Height - enemyProgress) * 6);
                    if (Math.Abs(m.ToY - enemyGoalY) <= 1) score += 250;
                }

                // Prefer advancing toward goal
                int progress = SideSymbol == "W" ? (board.Height - 1 - m.ToY) : m.ToY;
                score += progress * 10;

                // Prefer moves that keep diagonal support behind
                int supportY = SideSymbol == "W" ? m.ToY + 1 : m.ToY - 1;
                if (supportY >= 0 && supportY < board.Height)
                {
                    if (m.ToX - 1 >= 0 && board.PieceAt(m.ToX - 1, supportY)?.Value == SideSymbol) score += 12;
                    if (m.ToX + 1 < board.Width && board.PieceAt(m.ToX + 1, supportY)?.Value == SideSymbol) score += 12;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = m;
                }
            }

            return bestMove;
        }
    }
}
