namespace BoardGame.Library.Implementations
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class RandomAiPlayer : IPlayer
    {
        private readonly Random _random = new();

        public string Name { get; }
        public IGameLogic Logic { get; set; }

        public RandomAiPlayer(string name = "Bot (Easy)", IGameLogic logic = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Logic = logic;
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(0, 0);

            if (Logic is Scrabble.ScrabbleLogic sl)
            {
                var candidates = Scrabble.ScrabbleMoveGenerator.GenerateCandidateMoves(board, sl, this, maxCandidates: 80);
                return candidates.Count == 0 ? WordMove.Pass : candidates[_random.Next(candidates.Count)];
            }

            if (Logic is Checkers.CheckersLogic cl)
            {
                string side = cl.PlayerPieces.TryGetValue(this, out var p)
                    ? p.Value
                    : Checkers.CheckersLogic.InferSide(board);
                var validMoves = Checkers.CheckersLogic.GetValidMovesForSide(board, side, cl.MandatoryCapture, cl.IsBreakthrough);
                return validMoves.Count == 0 ? new Location(-1, -1) : validMoves[_random.Next(validMoves.Count)];
            }

            if (Logic is ConnectFour.ConnectFourLogic c4)
            {
                var validCols = new List<int>();
                for (int x = 0; x < board.Width; x++)
                {
                    if (c4.IsValidMove(board, new Location(x, 0)))
                    {
                        validCols.Add(x);
                    }
                }
                return validCols.Count == 0 ? new Location(0, 0) : new Location(validCols[_random.Next(validCols.Count)], 0);
            }

            if (Logic is Chomp.ChompLogic chomp)
            {
                var allValid = new List<ILocation>();
                var nonPoison = new List<ILocation>();
                for (int x = 0; x < board.Width; x++)
                {
                    for (int y = 0; y < board.Height; y++)
                    {
                        var loc = new Location(x, y);
                        if (!chomp.IsValidMove(board, loc)) continue;
                        allValid.Add(loc);
                        if (x != 0 || y != 0) nonPoison.Add(loc);
                    }
                }
                if (chomp.IsMisere && !chomp.IsRowOnlyNim && nonPoison.Count > 0)
                {
                    return nonPoison[_random.Next(nonPoison.Count)];
                }
                return allValid.Count == 0 ? new Location(0, 0) : allValid[_random.Next(allValid.Count)];
            }

            var valid = new List<ILocation>();
            var moveLogic = Logic as IMoveGameLogic;
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var loc = new Location(x, y);
                    bool isValid = Logic == null
                        ? board.PieceAt(x, y)?.Value == BlankPiece.BlankValue
                        : (moveLogic != null
                            ? moveLogic.IsValidMove(board, this, loc)
                            : Logic.IsValidMove(board, loc));
                    if (isValid)
                    {
                        valid.Add(loc);
                    }
                }
            }

            return valid.Count == 0 ? new Location(0, 0) : valid[_random.Next(valid.Count)];
        }
    }
}
