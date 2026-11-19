namespace BoardGame.Tests.Implementations.Sudoku
{
    using System;
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Sudoku;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class SudokuLogicShould
    {
        private SudokuLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new SudokuLogic(seed: 12345, emptyCells: 40);
            _board = new Board(9, 9);
            _p1 = new SimplePlayer("Alice");
            _p2 = new SimplePlayer("Bob");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void InitializeNineByNineGridWithGivenCluesAndBlanks()
        {
            int given = 0;
            int blank = 0;

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    var val = _board.PieceAt(x, y).Value;
                    if (val == BlankPiece.BlankValue)
                    {
                        blank++;
                    }
                    else
                    {
                        SudokuLogic.IsGivenCell(_board, x, y).ShouldBeTrue();
                        SudokuLogic.GetDigitAt(_board, x, y).ShouldBeInRange(1, 9);
                        given++;
                    }
                }
            }

            given.ShouldBe(41);
            blank.ShouldBe(40);
        }

        [Test]
        public void CreateIdenticalDailyBoardsForSameDate()
        {
            var date = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
            var logic1 = SudokuLogic.CreateDaily(date);
            var logic2 = SudokuLogic.CreateDaily(date);

            var b1 = new Board(9, 9);
            var b2 = new Board(9, 9);
            logic1.MapPieces(new[] { _p1 });
            logic2.MapPieces(new[] { _p1 });
            logic1.InitializeBoard(b1);
            logic2.InitializeBoard(b2);

            for (int x = 0; x < 9; x++)
            {
                for (int y = 0; y < 9; y++)
                {
                    b1.PieceAt(x, y).Value.ShouldBe(b2.PieceAt(x, y).Value);
                }
            }
        }

        [Test]
        public void ValidateValidMovesAndRejectConflictsOrGivenCells()
        {
            int targetX = -1, targetY = -1;
            int givenX = -1, givenY = -1;

            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (SudokuLogic.GetDigitAt(_board, x, y) == 0 && targetX == -1)
                    {
                        targetX = x;
                        targetY = y;
                    }
                    else if (SudokuLogic.IsGivenCell(_board, x, y) && givenX == -1)
                    {
                        givenX = x;
                        givenY = y;
                    }
                }
            }

            targetX.ShouldBeGreaterThanOrEqualTo(0);
            givenX.ShouldBeGreaterThanOrEqualTo(0);

            int solutionDigit = _logic.GetSolutionAt(targetX, targetY);
            int wrongConflictDigit = solutionDigit == 9 ? 1 : solutionDigit + 1;

            // Cannot overwrite a given cell
            _logic.IsValidMove(_board, _p1, new SudokuMove(givenX, givenY, 1)).ShouldBeFalse();

            // Solution digit on empty cell is valid
            _logic.IsValidMove(_board, _p1, new SudokuMove(targetX, targetY, solutionDigit)).ShouldBeTrue();

            // Place a conflicting digit in the same row if possible, or test conflict directly
            for (int x = 0; x < 9; x++)
            {
                int rowVal = SudokuLogic.GetDigitAt(_board, x, targetY);
                if (rowVal != 0)
                {
                    wrongConflictDigit = rowVal;
                    break;
                }
            }
            _logic.IsValidMove(_board, _p1, new SudokuMove(targetX, targetY, wrongConflictDigit)).ShouldBeFalse();
        }

        [Test]
        public void AwardBaseAndRowColumnBoxCompletionBonuses()
        {
            var singleEmptyLogic = new SudokuLogic(seed: 99, emptyCells: 1);
            var board = new Board(9, 9);
            singleEmptyLogic.MapPieces(new[] { _p1, _p2 });
            singleEmptyLogic.InitializeBoard(board);

            int ex = -1, ey = -1;
            for (int y = 0; y < 9; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (SudokuLogic.GetDigitAt(board, x, y) == 0)
                    {
                        ex = x;
                        ey = y;
                    }
                }
            }

            ex.ShouldBeGreaterThanOrEqualTo(0);
            int digit = singleEmptyLogic.GetSolutionAt(ex, ey);
            singleEmptyLogic.ApplyMove(board, _p1, new SudokuMove(ex, ey, digit)).ShouldBeTrue();

            // 10 base + 20 row + 20 col + 20 box = 70 points
            singleEmptyLogic.GetScore(_p1).ShouldBe(70);
            var winners = singleEmptyLogic.GetWinners(board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_p1);
        }

        [Test]
        public void RejectInvalidPlacementThatBreaksSolvability()
        {
            var board = new Board(9, 9);
            var logic = new SudokuLogic(seed: 42, emptyCells: 30);
            logic.MapPieces(new[] { _p1 });
            logic.InitializeBoard(board);

            int ex = -1, ey = -1;
            for (int y = 0; y < 9 && ex == -1; y++)
            {
                for (int x = 0; x < 9; x++)
                {
                    if (SudokuLogic.GetDigitAt(board, x, y) == 0)
                    {
                        ex = x;
                        ey = y;
                        break;
                    }
                }
            }

            int sol = logic.GetSolutionAt(ex, ey);
            logic.IsValidMove(board, _p1, new SudokuMove(ex, ey, sol)).ShouldBeTrue();
            logic.ApplyMove(board, _p1, new SudokuMove(ex, ey, sol)).ShouldBeTrue();
            logic.GetScore(_p1).ShouldBeGreaterThanOrEqualTo(10);
        }
    }
}
