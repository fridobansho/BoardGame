namespace BoardGame.Tests.Implementations.Breakthrough
{
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Breakthrough;
    using BoardGame.Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class BreakthroughLogicShould
    {
        private BreakthroughLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void Setup()
        {
            _logic = new BreakthroughLogic();
            _board = new Board(BreakthroughLogic.DefaultSize, BreakthroughLogic.DefaultSize);
            _p1 = new Mock<IPlayer>().Object;
            _p2 = new Mock<IPlayer>().Object;
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void InitializeBoardWithTwoRowsPerSide()
        {
            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 2; y++)
                    _board.PieceAt(x, y).Value.ShouldBe("B");
                for (int y = 2; y < 6; y++)
                    _board.PieceAt(x, y).Value.ShouldBe(BlankPiece.BlankValue);
                for (int y = 6; y < 8; y++)
                    _board.PieceAt(x, y).Value.ShouldBe("W");
            }
        }

        [Test]
        public void ValidateForwardAndDiagonalMovesCorrectly()
        {
            // P1 ("W") at (2, 6) can move forward (-1 Y) or diagonally forward
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 2, 5)).ShouldBeTrue();
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 1, 5)).ShouldBeTrue();
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 3, 5)).ShouldBeTrue();

            // Cannot move sideways or backwards or jump 2 squares
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 2, 7)).ShouldBeFalse();
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 3, 6)).ShouldBeFalse();
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 6, 2, 4)).ShouldBeFalse();

            // P2 ("B") at (2, 1) moves forward (+1 Y)
            _logic.IsValidMove(_board, _p2, new PieceMove(2, 1, 2, 2)).ShouldBeTrue();
            _logic.IsValidMove(_board, _p2, new PieceMove(2, 1, 3, 2)).ShouldBeTrue();
            _logic.IsValidMove(_board, _p2, new PieceMove(2, 1, 2, 0)).ShouldBeFalse();
        }

        [Test]
        public void ProhibitForwardCaptureButAllowDiagonalCapture()
        {
            _board.PieceAt(3, 5, new Piece("B"));
            _board.PieceAt(4, 5, new Piece("B"));

            // Directly ahead (dx == 0) is blocked even by enemy piece
            _logic.IsValidMove(_board, _p1, new PieceMove(3, 6, 3, 5)).ShouldBeFalse();

            // Diagonal (dx == 1) captures enemy piece
            IMove diagCapture = new PieceMove(3, 6, 4, 5);
            _logic.IsValidMove(_board, _p1, diagCapture).ShouldBeTrue();
            _logic.ApplyMove(_board, _p1, diagCapture).ShouldBeTrue();

            _board.PieceAt(3, 6).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(4, 5).Value.ShouldBe("W");

            // Diagonal into friendly piece is prohibited
            _logic.IsValidMove(_board, _p1, new PieceMove(4, 7, 5, 6)).ShouldBeFalse();
        }

        [Test]
        public void DeclareWinnerWhenPlayerReachesOpponentHomeRow()
        {
            _board.PieceAt(0, 1, new Piece("W"));
            _board.PieceAt(0, 0, BlankPiece.Blank);

            _logic.ApplyMove(_board, _p1, (IMove)new PieceMove(0, 1, 0, 0)).ShouldBeTrue();
            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBeSameAs(_p1);
        }

        [Test]
        public void InvertWinnerInMisereVariant()
        {
            var misere = new BreakthroughLogic { IsMisere = true };
            var board = new Board(8, 8);
            misere.MapPieces(new[] { _p1, _p2 });
            misere.InitializeBoard(board);

            board.PieceAt(1, 1, new Piece("W"));
            board.PieceAt(1, 0, BlankPiece.Blank);

            misere.ApplyMove(board, _p1, (IMove)new PieceMove(1, 1, 1, 0)).ShouldBeTrue();
            var winners = misere.GetWinners(board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBeSameAs(_p2);
        }

        [Test]
        public void SmartBreakthroughPlayerSelectsLegalHomeRowAdvance()
        {
            var p1 = new SmartBreakthroughPlayer("W-AI");
            var p2 = new SmartBreakthroughPlayer("B-AI");
            _logic.MapPieces(new[] { (IPlayer)p1, p2 });
            _logic.InitializeBoard(_board);

            // Clear a W piece just short of home row
            _board.PieceAt(3, 0, BlankPiece.Blank);
            _board.PieceAt(3, 1, new Piece("W"));
            p1.SideSymbol.ShouldBe("W");
            p2.SideSymbol.ShouldBe("B");

            var move = p1.GetMove(_board) as PieceMove;
            move.ShouldNotBeNull();
            _logic.IsValidMove(_board, p1, move).ShouldBeTrue();
            move.FromX.ShouldBe(3);
            move.FromY.ShouldBe(1);
            move.ToY.ShouldBe(0);
        }
    }
}
