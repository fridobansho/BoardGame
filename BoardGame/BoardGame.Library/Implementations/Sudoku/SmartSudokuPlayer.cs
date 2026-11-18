namespace BoardGame.Library.Implementations.Sudoku
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class SmartSudokuPlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private SudokuLogic _logic;

        public string Name { get; }

        public SmartSudokuPlayer(string name = "Sudoku Bot (Smart)", SudokuLogic logic = null)
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

            SudokuMove bestMove = null;
            int bestScore = int.MinValue;

            for (int y = 0; y < SudokuLogic.DefaultSize && y < board.Height; y++)
            {
                for (int x = 0; x < SudokuLogic.DefaultSize && x < board.Width; x++)
                {
                    if (SudokuLogic.GetDigitAt(board, x, y) != 0) continue;

                    var cellCandidates = new List<SudokuMove>();
                    for (int d = 1; d <= 9; d++)
                    {
                        var move = new SudokuMove(x, y, d);
                        if (logic.IsValidMove(board, this, move))
                        {
                            cellCandidates.Add(move);
                        }
                    }

                    if (cellCandidates.Count == 0) continue;

                    int nakedSingleBonus = cellCandidates.Count == 1 ? 100 : Math.Max(0, 20 - cellCandidates.Count * 4);
                    int completionScore = EvaluateCompletionBonus(board, x, y);

                    foreach (var move in cellCandidates)
                    {
                        int score = nakedSingleBonus + completionScore;
                        if (score > bestScore || (score == bestScore && _random.Next(2) == 0))
                        {
                            bestScore = score;
                            bestMove = move;
                        }
                    }
                }
            }

            return bestMove ?? (ILocation)new Location(-1, -1);
        }

        private static int EvaluateCompletionBonus(IBoard board, int targetX, int targetY)
        {
            int bonus = 0;

            int rowBlanks = 0;
            int colBlanks = 0;
            for (int i = 0; i < SudokuLogic.DefaultSize; i++)
            {
                if (SudokuLogic.GetDigitAt(board, i, targetY) == 0) rowBlanks++;
                if (SudokuLogic.GetDigitAt(board, targetX, i) == 0) colBlanks++;
            }
            if (rowBlanks == 1) bonus += 40;
            else if (rowBlanks == 2) bonus -= 10;

            if (colBlanks == 1) bonus += 40;
            else if (colBlanks == 2) bonus -= 10;

            int boxBlanks = 0;
            int boxStartX = (targetX / 3) * 3;
            int boxStartY = (targetY / 3) * 3;
            for (int dy = 0; dy < 3; dy++)
            {
                for (int dx = 0; dx < 3; dx++)
                {
                    if (SudokuLogic.GetDigitAt(board, boxStartX + dx, boxStartY + dy) == 0) boxBlanks++;
                }
            }
            if (boxBlanks == 1) bonus += 40;
            else if (boxBlanks == 2) bonus -= 10;

            return bonus;
        }
    }
}
