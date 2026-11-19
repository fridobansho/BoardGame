namespace BoardGame.Library.Implementations.Othello
{
    using Gomoku;
    using Interfaces;
    using System;
    using System.Collections.Generic;

    public class RandomOthelloPlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private readonly string _symbol;

        internal string AssignedSymbol { get; set; }

        public string Name { get; }

        public RandomOthelloPlayer(string name = "Othello Bot (Easy)", string symbol = null)
        {
            Name = name;
            _symbol = symbol;
        }

        public ILocation GetMove(IBoard board)
        {
            var mySymbol = _symbol ?? AssignedSymbol ?? InferMySymbol(board);
            var validMoves = new List<ILocation>();

            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value == BlankPiece.BlankValue &&
                        OthelloLogic.GetFlips(board, x, y, mySymbol).Count > 0)
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

        internal static string InferMySymbol(IBoard board)
        {
            int black = 0, white = 0;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var val = board.PieceAt(x, y)?.Value;
                    if (val == BlackPiece.Black.Value) black++;
                    else if (val == WhitePiece.White.Value) white++;
                }
            }
            return black <= white ? BlackPiece.Black.Value : WhitePiece.White.Value;
        }
    }
}
