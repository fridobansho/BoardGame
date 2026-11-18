namespace BoardGame.Library.Implementations.Checkers
{
    using System;
    using Interfaces;

    public class RandomCheckersPlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private readonly string _symbol;

        internal string AssignedSymbol { get; set; }

        public string Name { get; }

        public RandomCheckersPlayer(string name = "Checkers Bot (Easy)", string symbol = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _symbol = symbol;
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            string side = _symbol ?? AssignedSymbol ?? InferSide(board);
            var validMoves = CheckersLogic.GetValidMovesForSide(board, side);
            if (validMoves.Count == 0)
            {
                return new Location(-1, -1);
            }

            return validMoves[_random.Next(validMoves.Count)];
        }

        internal static string InferSide(IBoard board)
        {
            int redCount = 0;
            int blackCount = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (CheckersLogic.IsPlayerPiece(val, RedCheckerPiece.Red.Value)) redCount++;
                    else if (CheckersLogic.IsPlayerPiece(val, BlackCheckerPiece.Black.Value)) blackCount++;
                }
            }

            return redCount >= blackCount ? RedCheckerPiece.Red.Value : BlackCheckerPiece.Black.Value;
        }
    }
}
