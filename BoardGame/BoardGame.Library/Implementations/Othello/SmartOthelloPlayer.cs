namespace BoardGame.Library.Implementations.Othello
{
    using Gomoku;
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class SmartOthelloPlayer : IPlayer
    {
        private static readonly int[,] Weights =
        {
            { 100, -25, 10,  5,  5, 10, -25, 100 },
            { -25, -50, -2, -2, -2, -2, -50, -25 },
            {  10,  -2,  5,  1,  1,  5,  -2,  10 },
            {   5,  -2,  1,  0,  0,  1,  -2,   5 },
            {   5,  -2,  1,  0,  0,  1,  -2,   5 },
            {  10,  -2,  5,  1,  1,  5,  -2,  10 },
            { -25, -50, -2, -2, -2, -2, -50, -25 },
            { 100, -25, 10,  5,  5, 10, -25, 100 }
        };

        private readonly Random _random = new Random();
        private readonly string _symbol;

        public string Name { get; }

        public SmartOthelloPlayer(string name = "Othello Bot (Smart)", string symbol = null)
        {
            Name = name;
            _symbol = symbol;
        }

        public ILocation GetMove(IBoard board)
        {
            var mySymbol = _symbol ?? RandomOthelloPlayer.InferMySymbol(board);
            ILocation bestMove = null;
            int bestScore = int.MinValue;

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece == null || piece.Value != BlankPiece.BlankValue) continue;

                    var flips = OthelloLogic.GetFlips(board, x, y, mySymbol);
                    if (flips.Count == 0) continue;

                    int posWeight = (board.Width == 8 && board.Height == 8) ? Weights[y, x] : 0;
                    int score = posWeight * 10 + flips.Count;

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
