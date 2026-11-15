namespace BoardGame.Tests.Implementations.Nim
{
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Nim;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class NimLogicShould
    {
        private NimLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new NimLogic();
            _board = new Board(7, 4);
            _p1 = new SimplePlayer("Player 1");
            _p2 = new SimplePlayer("Player 2");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void InitializeRowsWithOneThreeFiveSevenStones()
        {
            int[] expected = { 1, 3, 5, 7 };
            for (int y = 0; y < 4; y++)
            {
                int count = 0;
                for (int x = 0; x < 7; x++)
                {
                    if (_board.PieceAt(x, y)?.Value == StonePiece.Stone.Value)
                    {
                        count++;
                    }
                }
                Assert.That(count, Is.EqualTo(expected[y]));
            }
        }

        [Test]
        public void RemoveClickedStoneAndStonesToTheRight()
        {
            Assert.That(_logic.ApplyMove(_board, _p1, new Location(2, 2)), Is.True);
            Assert.That(_board.PieceAt(0, 2).Value, Is.EqualTo(StonePiece.Stone.Value));
            Assert.That(_board.PieceAt(1, 2).Value, Is.EqualTo(StonePiece.Stone.Value));
            Assert.That(_board.PieceAt(2, 2).Value, Is.EqualTo(BlankPiece.BlankValue));
            Assert.That(_board.PieceAt(4, 2).Value, Is.EqualTo(BlankPiece.BlankValue));
        }

        [Test]
        public void DeclareLastPlayerToTakeStoneAsWinner()
        {
            _logic.ApplyMove(_board, _p1, new Location(0, 0));
            _logic.ApplyMove(_board, _p2, new Location(0, 1));
            _logic.ApplyMove(_board, _p1, new Location(0, 2));
            _logic.ApplyMove(_board, _p2, new Location(0, 3));

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            Assert.That(winners, Has.Count.EqualTo(1));
            Assert.That(winners[0], Is.EqualTo(_p2));
        }

        [Test]
        public void SmartPlayerFindsWinningNimSumMove()
        {
            // Clear all except row 3 (7 stones) -> nim-sum is 7, smart player should take all 7 stones at (0, 3)
            _logic.ApplyMove(_board, _p1, new Location(0, 0));
            _logic.ApplyMove(_board, _p1, new Location(0, 1));
            _logic.ApplyMove(_board, _p1, new Location(0, 2));

            var smart = new SmartNimPlayer();
            var move = smart.GetMove(_board);
            Assert.That(move, Is.Not.Null);
            Assert.That(move.X, Is.EqualTo(0));
            Assert.That(move.Y, Is.EqualTo(3));
        }

        [Test]
        public void RejectMoveOnEmptyCell()
        {
            Assert.That(_logic.IsValidMove(_board, new Location(6, 0)), Is.False);
            Assert.That(_logic.ApplyMove(_board, _p1, new Location(6, 0)), Is.False);
        }

        [Test]
        public void CompleteFullAiVsAiPlaythrough()
        {
            var board = new Board(7, 4);
            var smart = new SmartNimPlayer("Smart");
            var random = new RandomNimPlayer("Random");
            var players = new IPlayer[] { smart, random };
            _logic.MapPieces(players);
            _logic.InitializeBoard(board);

            int turn = 0;
            while (!_logic.GetWinners(board, players).Any() && turn < 20)
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
