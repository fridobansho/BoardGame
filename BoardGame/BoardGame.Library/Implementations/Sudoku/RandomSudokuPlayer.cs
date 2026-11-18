namespace BoardGame.Library.Implementations.Sudoku
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class RandomSudokuPlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private SudokuLogic _logic;

        public string Name { get; }

        public RandomSudokuPlayer(string name = "Sudoku Bot (Easy)", SudokuLogic logic = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _logic = logic;
        }

        public void Bind(SudokuLogic logic)
        {
            _logic = logic;
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(-1, -1);

            var logic = _logic ?? SudokuLogic.LastInitializedLogic ?? new SudokuLogic();
            var candidates = new List<SudokuMove>();

            for (int y = 0; y < SudokuLogic.DefaultSize && y < board.Height; y++)
            {
                for (int x = 0; x < SudokuLogic.DefaultSize && x < board.Width; x++)
                {
                    if (SudokuLogic.GetDigitAt(board, x, y) != 0) continue;

                    for (int d = 1; d <= 9; d++)
                    {
                        var candidate = new SudokuMove(x, y, d);
                        if (logic.IsValidMove(board, this, candidate))
                        {
                            candidates.Add(candidate);
                        }
                    }
                }
            }

            if (candidates.Count == 0) return new Location(-1, -1);
            return candidates[_random.Next(candidates.Count)];
        }
    }
}
