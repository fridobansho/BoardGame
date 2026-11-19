namespace BoardGame.Tests.Implementations.Hex
{
    using BoardGame.Library.Implementations;
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

        [Test]
        public void DetectZigZagConnectedPathWin()
        {
            // Top-to-bottom zig-zag using hex neighbors (x, y) -> (x+1, y-1) / (x-1, y+1)
            var path = new[]
            {
                new Location(2, 0),
                new Location(2, 1),
                new Location(1, 2),
                new Location(1, 3),
                new Location(1, 4)
            };

            foreach (var loc in path)
            {
                Assert.That(_logic.ApplyMove(_board, _p1, loc), Is.True);
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p1));
        }

        [Test]
        public void CompleteFullAiVsAiPlaythrough()
        {
            var board = new Board(5, 5);
            var smart = new SmartHexPlayer("Smart");
            var random = new RandomAiPlayer("Random");
            var players = new IPlayer[] { smart, random };
            _logic.MapPieces(players);
            _logic.InitializeBoard(board);

            int turn = 0;
            while (!_logic.GetWinners(board, players).Any() && turn < 25)
            {
                var current = players[turn % 2];
                var move = current.GetMove(board);
                Assert.That(move, Is.Not.Null);
                Assert.That(_logic.ApplyMove(board, current, move), Is.True);
                turn++;
            }

            var winners = _logic.GetWinners(board, players).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
        }
    }
}
