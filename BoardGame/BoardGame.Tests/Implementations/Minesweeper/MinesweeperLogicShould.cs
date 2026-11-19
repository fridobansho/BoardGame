namespace BoardGame.Tests.Implementations.Minesweeper
{
    using System.Collections.Generic;
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Minesweeper;
    using BoardGame.Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class MinesweeperLogicShould
    {
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void Setup()
        {
            _p1 = new Mock<IPlayer>().Object;
            _p2 = new Mock<IPlayer>().Object;
        }

        [Test]
        public void GuaranteeFirstClickSafetyEvenWithSeededHighMineDensity()
        {
            for (int testY = 0; testY < 3; testY++)
            {
                var logic = new MinesweeperLogic(seed: 42, mineCount: 80);
                var board = new Board(9, 9);
                logic.MapPieces(new[] { _p1, _p2 });
                logic.InitializeBoard(board);

                logic.ApplyMove(board, _p1, new Location(testY, testY)).ShouldBeTrue();
                logic.IsExploded.ShouldBeFalse();
                board.PieceAt(testY, testY).Value.ShouldNotBe("M");
            }
        }

        [Test]
        public void ToggleFlagOnlyWhenDigitEqualsOne()
        {
            var logic = new MinesweeperLogic(seed: 7, mineCount: 10);
            var board = new Board(9, 9);
            logic.MapPieces(new[] { _p1, _p2 });
            logic.InitializeBoard(board);

            // Digit != 1 is rejected
            logic.IsValidMove(board, _p1, new SudokuMove(0, 0, 2)).ShouldBeFalse();

            // Digit == 1 toggles "H" <-> "F"
            logic.ApplyMove(board, _p1, new SudokuMove(0, 0, 1)).ShouldBeTrue();
            board.PieceAt(0, 0).Value.ShouldBe("F");

            logic.ApplyMove(board, _p1, new SudokuMove(0, 0, 1)).ShouldBeTrue();
            board.PieceAt(0, 0).Value.ShouldBe("H");
        }

        [Test]
        public void PreserveFlaggedCellsDuringFloodFill()
        {
            var logic = new MinesweeperLogic(seed: 1, mineCount: 1);
            var board = new Board(9, 9);
            var players = new[] { _p1, _p2 };
            logic.MapPieces(players);
            logic.InitializeBoard(board);

            // Inject a deterministic layout with 1 mine at (8, 8) so revealing (0, 0) floods the board
            var mineChars = new char[81];
            for (int i = 0; i < 81; i++) mineChars[i] = '0';
            mineChars[8 * 9 + 8] = '1';

            logic.ImportCustomState(players, new Dictionary<string, string>
            {
                ["w"] = "9",
                ["h"] = "9",
                ["mines"] = new string(mineChars),
                ["minesPlaced"] = "1",
                ["exploded"] = "0",
                ["won"] = "0"
            });

            // Flag (1, 1) before revealing (0, 0)
            logic.ApplyMove(board, _p1, new SudokuMove(1, 1, 1)).ShouldBeTrue();
            board.PieceAt(1, 1).Value.ShouldBe("F");

            logic.ApplyMove(board, _p1, new Location(0, 0)).ShouldBeTrue();
            board.PieceAt(1, 1).Value.ShouldBe("F");
            board.PieceAt(0, 0).Value.ShouldBe("0");
        }

        [Test]
        public void EndInLossWhenMineRevealedOrWinWhenAllSafeCellsCleared()
        {
            var players = new[] { _p1, _p2 };
            var mineChars = new char[9] { '1', '0', '0', '0', '0', '0', '0', '0', '0' };

            // Explosion branch: click mine at (0, 0)
            var loseLogic = new MinesweeperLogic(seed: 1, mineCount: 1);
            var loseBoard = new Board(3, 3);
            loseLogic.MapPieces(players);
            loseLogic.InitializeBoard(loseBoard);
            loseLogic.ImportCustomState(players, new Dictionary<string, string>
            {
                ["w"] = "3",
                ["h"] = "3",
                ["mines"] = new string(mineChars),
                ["minesPlaced"] = "1",
                ["exploded"] = "0",
                ["won"] = "0"
            });

            loseLogic.ApplyMove(loseBoard, _p1, new Location(0, 0)).ShouldBeTrue();
            loseLogic.IsExploded.ShouldBeTrue();
            loseLogic.HasValidMoves(loseBoard).ShouldBeFalse();
            loseLogic.GetWinners(loseBoard, players).Count().ShouldBe(2);

            // Win branch: reveal corner (2, 2) which flood-fills all 8 safe cells on 3x3 with mine at (0, 0)
            var winLogic = new MinesweeperLogic(seed: 1, mineCount: 1);
            var winBoard = new Board(3, 3);
            winLogic.MapPieces(players);
            winLogic.InitializeBoard(winBoard);
            winLogic.ImportCustomState(players, new Dictionary<string, string>
            {
                ["w"] = "3",
                ["h"] = "3",
                ["mines"] = new string(mineChars),
                ["minesPlaced"] = "1",
                ["exploded"] = "0",
                ["won"] = "0"
            });

            winLogic.ApplyMove(winBoard, _p1, new Location(2, 2)).ShouldBeTrue();
            winLogic.IsWon.ShouldBeTrue();
            winLogic.HasValidMoves(winBoard).ShouldBeFalse();
            winLogic.GetScore(_p1).ShouldBeGreaterThan(0);
            winLogic.GetWinners(winBoard, players).Single().ShouldBeSameAs(_p1);
        }

        [Test]
        public void ExportAndImportCustomStateIncludingWinner()
        {
            var players = new[] { _p1, _p2 };
            var mineChars = new char[9] { '1', '0', '0', '0', '0', '0', '0', '0', '0' };
            var logic = new MinesweeperLogic(seed: 1, mineCount: 1);
            var board = new Board(3, 3);
            logic.MapPieces(players);
            logic.InitializeBoard(board);
            logic.ImportCustomState(players, new Dictionary<string, string>
            {
                ["w"] = "3",
                ["h"] = "3",
                ["mines"] = new string(mineChars),
                ["minesPlaced"] = "1",
                ["exploded"] = "0",
                ["won"] = "0"
            });

            logic.ApplyMove(board, _p2, new Location(2, 2)).ShouldBeTrue();
            var exported = logic.ExportCustomState(players);
            exported["won"].ShouldBe("1");
            exported["winnerIdx"].ShouldBe("1");

            var restored = new MinesweeperLogic(seed: 1, mineCount: 1);
            restored.MapPieces(players);
            restored.InitializeBoard(new Board(3, 3));
            restored.ImportCustomState(players, exported);

            restored.IsWon.ShouldBeTrue();
            restored.GetWinners(board, players).Single().ShouldBeSameAs(_p2);
        }
    }
}
