namespace BoardGame.Tests.Implementations.Hex
{
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Gomoku;
    using BoardGame.Library.Implementations.Hex;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class HexLogicShould
    {
        private HexLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new HexLogic();
            _board = new Board(5, 5);
            _p1 = new SimplePlayer("Black");
            _p2 = new SimplePlayer("White");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void DetectTopToBottomWinForPlayer1()
        {
            for (int y = 0; y < 5; y++)
            {
                _logic.ApplyMove(_board, _p1, new Location(2, y));
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p1));
        }

        [Test]
        public void DetectLeftToRightWinForPlayer2()
        {
            for (int x = 0; x < 5; x++)
            {
                _logic.ApplyMove(_board, _p2, new Location(x, 2));
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p2));
        }

        [Test]
        public void SmartPlayerReturnsValidMove()
        {
            var smart = new SmartHexPlayer();
            var move = smart.GetMove(_board);
            Assert.That(move, Is.Not.Null);
            Assert.That(_logic.IsValidMove(_board, move), Is.True);
        }
    }
}
