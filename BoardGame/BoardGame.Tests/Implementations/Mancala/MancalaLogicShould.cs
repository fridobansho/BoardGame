namespace BoardGame.Tests.Implementations.Mancala
{
    using System.Collections.Generic;
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Mancala;
    using BoardGame.Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class MancalaLogicShould
    {
        private MancalaLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void Setup()
        {
            _logic = new MancalaLogic();
            _board = new Board(MancalaLogic.PitCountPerSide, 2);
            _p1 = new Mock<IPlayer>().Object;
            _p2 = new Mock<IPlayer>().Object;
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        private void ClearBoardToZero()
        {
            for (int x = 0; x < MancalaLogic.PitCountPerSide; x++)
            {
                _board.PieceAt(x, 0, new Piece("0"));
                _board.PieceAt(x, 1, new Piece("0"));
            }
        }

        [Test]
        public void InitializeAllPitsAndZeroScores()
        {
            var custom = new MancalaLogic { InitialStonesPerPit = 5 };
            custom.MapPieces(new[] { _p1, _p2 });
            custom.InitializeBoard(_board);

            for (int x = 0; x < 6; x++)
            {
                MancalaLogic.GetPitCount(_board, x, 0).ShouldBe(5);
                MancalaLogic.GetPitCount(_board, x, 1).ShouldBe(5);
            }
            custom.GetScore(_p1).ShouldBe(0);
            custom.GetScore(_p2).ShouldBe(0);
        }

        [Test]
        public void SowStonesIntoStoreAndEarnExtraTurnWhenLandingInOwnStore()
        {
            // P1 row is y = 1. Pit (2, 1) has 4 stones: lands in 3, 4, 5, and own store (6).
            _logic.ApplyMove(_board, _p1, new Location(2, 1)).ShouldBeTrue();

            MancalaLogic.GetPitCount(_board, 2, 1).ShouldBe(0);
            MancalaLogic.GetPitCount(_board, 3, 1).ShouldBe(5);
            MancalaLogic.GetPitCount(_board, 4, 1).ShouldBe(5);
            MancalaLogic.GetPitCount(_board, 5, 1).ShouldBe(5);
            _logic.GetScore(_p1).ShouldBe(1);
            _logic.LastMoveEarnedExtraTurn.ShouldBeTrue();
        }

        [Test]
        public void CaptureOppositePitWhenLandingInOwnEmptyPit()
        {
            ClearBoardToZero();
            _board.PieceAt(0, 1, new Piece("2")); // lands at (2, 1), which is empty
            _board.PieceAt(2, 0, new Piece("6")); // opposite pit on P2's row
            _board.PieceAt(5, 0, new Piece("3")); // keep opponent row non-empty so sweep doesn't trigger

            _logic.ApplyMove(_board, _p1, new Location(0, 1)).ShouldBeTrue();

            _logic.LastMoveEarnedExtraTurn.ShouldBeFalse();
            MancalaLogic.GetPitCount(_board, 2, 1).ShouldBe(0);
            MancalaLogic.GetPitCount(_board, 2, 0).ShouldBe(0);
            _logic.GetScore(_p1).ShouldBe(7); // 1 landed + 6 opposite
        }

        [Test]
        public void SkipOpponentStoreWhenSowingAroundBoard()
        {
            ClearBoardToZero();
            // P1 pit 5 has 8 stones: P1 store (1), P2 pits 5..0 (6 stones), skips P2 store (13), lands at P1 pit (0, 1) which already has 1 stone (no capture)
            _board.PieceAt(5, 1, new Piece("8"));
            _board.PieceAt(0, 1, new Piece("1"));
            _board.PieceAt(1, 0, new Piece("1"));

            _logic.ApplyMove(_board, _p1, new Location(5, 1)).ShouldBeTrue();

            _logic.GetScore(_p1).ShouldBe(1);
            _logic.GetScore(_p2).ShouldBe(0);
            MancalaLogic.GetPitCount(_board, 0, 1).ShouldBe(2);
        }

        [Test]
        public void SweepRemainingStonesAndDetermineWinnerStandardAndMisere()
        {
            ClearBoardToZero();
            _board.PieceAt(5, 1, new Piece("1")); // P1 drops into store, emptying P1 row
            _board.PieceAt(0, 0, new Piece("10")); // Swept to P2 store

            _logic.ApplyMove(_board, _p1, new Location(5, 1)).ShouldBeTrue();
            _logic.GetScore(_p1).ShouldBe(1);
            _logic.GetScore(_p2).ShouldBe(10);
            _logic.LastMoveEarnedExtraTurn.ShouldBeFalse();

            // Standard: higher store (P2) wins
            _logic.GetWinners(_board, new[] { _p1, _p2 }).Single().ShouldBeSameAs(_p2);

            // Misere: lower store (P1) wins
            _logic.IsMisere = true;
            _logic.GetWinners(_board, new[] { _p1, _p2 }).Single().ShouldBeSameAs(_p1);
        }

        [Test]
        public void RoundTripCustomStateAndSelectExtraTurnInSmartAi()
        {
            var ai1 = new SmartMancalaPlayer("AI-1");
            var ai2 = new SmartMancalaPlayer("AI-2");
            _logic.MapPieces(new[] { (IPlayer)ai1, ai2 });
            _logic.InitializeBoard(_board);

            var aiMove = ai1.GetMove(_board);
            // Pit (2, 1) has 4 stones and earns an extra turn
            aiMove.X.ShouldBe(2);
            aiMove.Y.ShouldBe(1);

            _logic.ApplyMove(_board, ai1, aiMove).ShouldBeTrue();
            var exported = _logic.ExportCustomState(new[] { (IPlayer)ai1, ai2 });
            exported["extraTurn"].ShouldBe("1");

            var restored = new MancalaLogic();
            restored.ImportCustomState(new[] { (IPlayer)ai1, ai2 }, exported);
            restored.LastMoveEarnedExtraTurn.ShouldBeTrue();

            restored.ImportCustomState(new[] { (IPlayer)ai1, ai2 }, new Dictionary<string, string> { ["extraTurn"] = "0" });
            restored.LastMoveEarnedExtraTurn.ShouldBeFalse();
        }
    }
}
