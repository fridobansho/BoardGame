namespace BoardGame.Tests.Implementations.Othello
{
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Othello;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using System.Linq;

    [TestFixture]
    public class OthelloLogicShould
    {
        private OthelloLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new OthelloLogic();
            _board = new Board(8, 8);
            _p1 = new SimplePlayer("Black");
            _p2 = new SimplePlayer("White");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void InitializeCenterFourDiscs()
        {
            Assert.That(_board.PieceAt(3, 3).Value, Is.EqualTo(WhitePiece.White.Value));
            Assert.That(_board.PieceAt(4, 4).Value, Is.EqualTo(WhitePiece.White.Value));
            Assert.That(_board.PieceAt(4, 3).Value, Is.EqualTo(BlackPiece.Black.Value));
            Assert.That(_board.PieceAt(3, 4).Value, Is.EqualTo(BlackPiece.Black.Value));
        }

        [Test]
        public void AllowValidOutflankingMoveAndFlipDiscs()
        {
            // Black at (2, 3) outflanks White at (3, 3) against Black at (4, 3)
            Assert.That(_logic.IsValidMove(_board, _p1, new Location(2, 3)), Is.True);
            Assert.That(_logic.ApplyMove(_board, _p1, new Location(2, 3)), Is.True);

            Assert.That(_board.PieceAt(2, 3).Value, Is.EqualTo(BlackPiece.Black.Value));
            Assert.That(_board.PieceAt(3, 3).Value, Is.EqualTo(BlackPiece.Black.Value));
        }

        [Test]
        public void RejectNonOutflankingMove()
        {
            Assert.That(_logic.IsValidMove(_board, _p1, new Location(0, 0)), Is.False);
            Assert.That(_logic.ApplyMove(_board, _p1, new Location(0, 0)), Is.False);
        }

        [Test]
        public void SmartPlayerReturnsLegalMove()
        {
            var smart = new SmartOthelloPlayer();
            var move = smart.GetMove(_board);
            Assert.That(move, Is.Not.Null);
            Assert.That(_logic.IsValidMove(_board, _p1, move), Is.True);
        }

        [Test]
        public void CompleteFullAiVsAiPlaythroughWithTurnSkipping()
        {
            var board = new Board(8, 8);
            var smart = new SmartOthelloPlayer("Smart", BlackPiece.Black.Value);
            var random = new RandomAiPlayer("Random");
            var players = new IPlayer[] { smart, random };
            _logic.MapPieces(players);
            _logic.InitializeBoard(board);

            int turn = 0;
            int safety = 0;
            while (!_logic.GetWinners(board, players).Any() && safety < 100)
            {
                var current = players[turn % 2];
                if (_logic.HasValidMoves(board, current))
                {
                    var move = current.GetMove(board);
                    Assert.That(move, Is.Not.Null);
                    Assert.That(_logic.ApplyMove(board, current, move), Is.True);
                }
                turn++;
                safety++;
            }

            var winners = _logic.GetWinners(board, players).ToList();
            Assert.That(winners, Is.Not.Empty);
        }
    }
}
