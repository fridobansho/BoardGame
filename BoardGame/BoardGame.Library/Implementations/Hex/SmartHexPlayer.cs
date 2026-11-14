namespace BoardGame.Library.Implementations.Hex
{
    using Gomoku;
    using Interfaces;
    using System;

    public class SmartHexPlayer : IPlayer
    {
        private static readonly (int dx, int dy)[] HexNeighbors =
        {
            (-1, 0), (1, 0), (0, -1), (0, 1), (1, -1), (-1, 1)
        };

        private readonly Random _random = new Random();

        public string Name { get; }

        public SmartHexPlayer(string name = "Hex Bot (Smart)")
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            int centerX = board.Width / 2;
            int centerY = board.Height / 2;
            ILocation bestMove = null;
            int bestScore = int.MinValue;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    if (board.PieceAt(x, y)?.Value != BlankPiece.BlankValue) continue;

                    int neighborScore = 0;
                    foreach (var (dx, dy) in HexNeighbors)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (board.CheckBounds(nx, ny))
                        {
                            var val = board.PieceAt(nx, ny)?.Value;
                            if (val == BlackPiece.Black.Value || val == WhitePiece.White.Value)
                            {
                                neighborScore += 10;
                            }
                        }
                    }

                    int distFromCenter = Math.Abs(x - centerX) + Math.Abs(y - centerY);
                    int score = neighborScore - distFromCenter;

                    if (score > bestScore || (score == bestScore && _random.Next(2) == 0))
                    {
                        bestScore = score;
                        bestMove = new Location(x, y);
                    }
                }
            }

            return bestMove;
        }
    }
}
