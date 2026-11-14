namespace BoardGame.Tests.Implementations.Gomoku
{
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Gomoku;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class GomokuLogicShould
    {
        private GomokuLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new GomokuLogic();
            _board = new Board(15, 15);
            _p1 = new SimplePlayer("Black");
            _p2 = new SimplePlayer("White");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void AllowValidMoveOnEmptyCell()
        {
            Assert.That(_logic.IsValidMove(_board, new Location(7, 7)), Is.True);
            Assert.That(_logic.ApplyMove(_board, _p1, new Location(7, 7)), Is.True);
            Assert.That(_board.PieceAt(7, 7).Value, Is.EqualTo(BlackPiece.Black.Value));
        }

        [Test]
        public void RejectMoveOnOccupiedCell()
        {
            _logic.ApplyMove(_board, _p1, new Location(7, 7));
            Assert.That(_logic.IsValidMove(_board, new Location(7, 7)), Is.False);
            Assert.That(_logic.ApplyMove(_board, _p2, new Location(7, 7)), Is.False);
        }

        [Test]
        public void DetectHorizontalFiveInARowWin()
        {
            for (int x = 3; x < 8; x++)
            {
                _logic.ApplyMove(_board, _p1, new Location(x, 5));
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p1));
        }

        [Test]
        public void DetectDiagonalFiveInARowWin()
        {
            for (int i = 0; i < 5; i++)
            {
                _logic.ApplyMove(_board, _p2, new Location(2 + i, 2 + i));
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p2));
        }

        [Test]
        public void NotDeclareWinnerForFourInARow()
        {
            for (int x = 0; x < 4; x++)
            {
                _logic.ApplyMove(_board, _p1, new Location(x, 0));
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Is.Empty);
        }

        [Test]
        public void SmartPlayerBlocksImmediateThreat()
        {
            var smart = new SmartGomokuPlayer();
            for (int x = 4; x < 8; x++)
            {
                _logic.ApplyMove(_board, _p1, new Location(x, 7));
            }

            var move = smart.GetMove(_board);
            Assert.That(move, Is.Not.Null);
            Assert.That(move.Y, Is.EqualTo(7));
            Assert.That(move.X == 3 || move.X == 8, Is.True);
        }
    }
}
