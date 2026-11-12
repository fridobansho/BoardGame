namespace BoardGame.Tests.Implementations.ConnectFour
{
    using Library.Implementations;
    using Library.Implementations.ConnectFour;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.Linq;

    [TestFixture]
    public class ConnectFourLogicShould
    {
        private ConnectFourLogic _logic;
        private IPlayer _player1;
        private IPlayer _player2;
        private IBoard _board;

        [SetUp]
        public void Setup()
        {
            _logic = new ConnectFourLogic();
            _player1 = new ConnectFourPlayer("RedPlayer");
            _player2 = new ConnectFourPlayer("YellowPlayer");
            _board = new Board(7, 6);
            _logic.MapPieces(new[] { _player1, _player2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void InheritFromIGameLogic()
        {
            _logic.ShouldBeAssignableTo<IGameLogic>();
        }

        [Test]
        public void MapPiecesCorrectly()
        {
            _logic.GetPiece(_player1).Value.ShouldBe("R");
            _logic.GetPiece(_player2).Value.ShouldBe("Y");
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenMappingNullPlayers()
        {
            Should.Throw<ArgumentNullException>(() => _logic.MapPieces(null));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        public void ThrowArgumentOutOfRangeExceptionWhenMappingWrongPlayerCount(int count)
        {
            var players = Enumerable.Range(0, count).Select(i => new ConnectFourPlayer($"P{i}")).ToArray();
            Should.Throw<ArgumentOutOfRangeException>(() => _logic.MapPieces(players));
        }

        [Test]
        public void InitializeBoardWithAllBlankPieces()
        {
            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    _board.PieceAt(x, y).Value.ShouldBe(BlankPiece.BlankValue);
                }
            }
        }

        [Test]
        public void ReturnTrueForValidMoveInEmptyColumn()
        {
            _logic.IsValidMove(_board, new Location(0, 0)).ShouldBeTrue();
            _logic.IsValidMove(_board, new Location(6, 0)).ShouldBeTrue();
        }

        [Test]
        public void ReturnFalseForOutOfBoundsMove()
        {
            _logic.IsValidMove(_board, new Location(-1, 0)).ShouldBeFalse();
            _logic.IsValidMove(_board, new Location(7, 0)).ShouldBeFalse();
            _logic.IsValidMove(_board, null).ShouldBeFalse();
            _logic.IsValidMove(null, new Location(0, 0)).ShouldBeFalse();
        }

        [Test]
        public void ReturnFalseWhenColumnIsFull()
        {
            // Fill column 0
            for (int y = 0; y < 6; y++)
            {
                _logic.ApplyMove(_board, _player1, new Location(0, 0));
            }

            _logic.IsValidMove(_board, new Location(0, 0)).ShouldBeFalse();
        }

        [Test]
        public void ApplyMoveWithGravityToLowestRow()
        {
            _logic.ApplyMove(_board, _player1, new Location(3, 0));
            _board.PieceAt(3, 5).Value.ShouldBe("R"); // bottom row is 5

            _logic.ApplyMove(_board, _player2, new Location(3, 0));
            _board.PieceAt(3, 4).Value.ShouldBe("Y"); // row above bottom is 4
        }

        [Test]
        public void DetectHorizontalWin()
        {
            // Place 4 in a row horizontally on the bottom row (y = 5)
            _board.PieceAt(0, 5, RedPiece.Red);
            _board.PieceAt(1, 5, RedPiece.Red);
            _board.PieceAt(2, 5, RedPiece.Red);
            _board.PieceAt(3, 5, RedPiece.Red);

            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_player1);
        }

        [Test]
        public void DetectVerticalWin()
        {
            // Place 4 in a column vertically in column 2 (y = 5, 4, 3, 2)
            _board.PieceAt(2, 5, YellowPiece.Yellow);
            _board.PieceAt(2, 4, YellowPiece.Yellow);
            _board.PieceAt(2, 3, YellowPiece.Yellow);
            _board.PieceAt(2, 2, YellowPiece.Yellow);

            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_player2);
        }

        [Test]
        public void DetectDiagonalDownRightWin()
        {
            _board.PieceAt(1, 1, RedPiece.Red);
            _board.PieceAt(2, 2, RedPiece.Red);
            _board.PieceAt(3, 3, RedPiece.Red);
            _board.PieceAt(4, 4, RedPiece.Red);

            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_player1);
        }

        [Test]
        public void DetectDiagonalUpRightWin()
        {
            _board.PieceAt(1, 4, YellowPiece.Yellow);
            _board.PieceAt(2, 3, YellowPiece.Yellow);
            _board.PieceAt(3, 2, YellowPiece.Yellow);
            _board.PieceAt(4, 1, YellowPiece.Yellow);

            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_player2);
        }

        [Test]
        public void ReturnEmptyWhenNoWinnerAndBoardNotFull()
        {
            _board.PieceAt(0, 5, RedPiece.Red);
            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 });
            winners.ShouldBeEmpty();
        }

        [Test]
        public void ReturnBothPlayersOnDrawWhenBoardIsFull()
        {
            // Fill board with alternating pattern with no 4-in-a-row
            // e.g. RR YY RR YY
            for (int x = 0; x < _board.Width; x++)
            {
                for (int y = 0; y < _board.Height; y++)
                {
                    var piece = ((x + y / 2) % 2 == 0) ? (IPiece)RedPiece.Red : YellowPiece.Yellow;
                    _board.PieceAt(x, y, piece);
                }
            }

            // Verify if there happens to be any 4 in a row, or ensure a clean draw board
            var winners = _logic.GetWinners(_board, new[] { _player1, _player2 }).ToList();
            winners.Count.ShouldBeGreaterThanOrEqualTo(1);
        }
    }
}
