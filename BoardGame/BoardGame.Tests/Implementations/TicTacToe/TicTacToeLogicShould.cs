namespace BoardGame.Tests.Implementations.TicTacToe
{
    using Moq;
    using Shouldly;
    using NUnit.Framework;
    using Library.Interfaces;
    using Library.Implementations.TicTacToe;
    using System;
    using System.Linq;
    using Library.Implementations;

    [TestFixture]
    public class TicTacToeLogicShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new TicTacToeLogic();

            sut.PlayerPieces.ShouldBeEmpty();
        }

        [Test]
        [TestCase(-1, -1)]
        [TestCase(3, 3)]
        public void ReturnFalseForOutOfBounds(int x, int y)
        {
            var board = new Mock<IBoard>();
            var location = new Location(x, y);
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.CheckBounds(location.X, location.Y)).Returns(false);

            var result = sut.IsValidMove(board.Object, location);

            board.Verify(mock => mock.CheckBounds(location.X, location.Y), Times.Once);
            result.ShouldBeFalse();
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        public void ReturnTrueForInBoundsBlank(int x, int y)
        {
            var board = new Mock<IBoard>();
            var location = new Location(x, y);
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.CheckBounds(location.X, location.Y)).Returns(true);
            board.Setup(mock => mock.PieceAt(location.X, location.Y)).Returns(BlankPiece.Blank);

            var result = sut.IsValidMove(board.Object, location);

            board.Verify(mock => mock.CheckBounds(location.X, location.Y), Times.Once);
            board.Verify(mock => mock.PieceAt(location.X, location.Y), Times.Once);
            result.ShouldBeTrue();
        }

        [Test]
        public void ReturnFalseForAlreadyTaken()
        {
            var board = new Mock<IBoard>();
            var location = new Location(0, 0);
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.CheckBounds(location.X, location.Y)).Returns(true);
            board.Setup(mock => mock.PieceAt(location.X, location.Y)).Returns(XPiece.X);

            var result = sut.IsValidMove(board.Object, location);

            board.Verify(mock => mock.CheckBounds(location.X, location.Y), Times.Once);
            board.Verify(mock => mock.PieceAt(location.X, location.Y), Times.Once);
            result.ShouldBeFalse();
        }

        [Test, ExpectedException(typeof(ArgumentNullException), MatchType = MessageMatch.Contains, ExpectedMessage = "players")]
        public void ThrowsExceptionIfPassedNull()
        {
            var sut = new TicTacToeLogic();

            sut.MapPieces(null);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowsExceptionIfPassedEmpty()
        {
            var players = Enumerable.Empty<IPlayer>();
            var sut = new TicTacToeLogic();

            sut.MapPieces(players);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowsExceptionIfPassedSingle()
        {
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var sut = new TicTacToeLogic();

            sut.MapPieces(players);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "player")]
        public void ThrowsExceptionIfNoMappedPlayer()
        {
            var player = new Mock<IPlayer>();
            var sut = new TicTacToeLogic();

            var result = sut.GetPiece(player.Object);
        }

        [Test]
        public void ReturnMappedPiece()
        {
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new TicTacToeLogic();
            sut.MapPieces(players);

            var result1 = sut.GetPiece(player1.Object);
            var result2 = sut.GetPiece(player2.Object);

            result1.ShouldBe(XPiece.X);
            result2.ShouldBe(OPiece.O);
        }

        [Test, ExpectedException(typeof(ArgumentNullException), MatchType = MessageMatch.Contains, ExpectedMessage = "players")]
        public void ThrowExceptionIfPassedNull()
        {
            var board = new Mock<IBoard>();
            var sut = new TicTacToeLogic();

            var result = sut.GetWinners(board.Object, null);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowExceptionIfPassedEmpty()
        {
            var board = new Mock<IBoard>();
            var players = Enumerable.Empty<IPlayer>();
            var sut = new TicTacToeLogic();

            var result = sut.GetWinners(board.Object, players);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowExceptionIfPassedSingle()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var sut = new TicTacToeLogic();

            var result = sut.GetWinners(board.Object, players);
        }

        [Test]
        public void ReturnFalseIfNoValidMoves()
        {
            var board = new Mock<IBoard>();
            var sut = new TicTacToeLogic();

            var result = sut.ValidMoves(board.Object);

            result.ShouldBeFalse();
        }

        [Test]
        public void ReturnTrueIfValidMoves()
        {
            var board = new Mock<IBoard>();
            var sut = new TicTacToeLogic();
            var location = new Location(2, 2);
            board.Setup(mock => mock.Width).Returns(3);
            board.Setup(mock => mock.Height).Returns(3);
            board.Setup(mock => mock.CheckBounds(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>())).Returns(BlankPiece.Blank);

            var result = sut.ValidMoves(board.Object);

            board.Verify(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>()), Times.Once);
            result.ShouldBeTrue();
        }

        [Test]
        public void ReturnEmptyIfNoWinAndValidMoves()
        {
            var board = new Mock<IBoard>();
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.Width).Returns(1);
            board.Setup(mock => mock.Height).Returns(1);
            board.Setup(mock => mock.CheckBounds(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>())).Returns(BlankPiece.Blank);

            var result = sut.GetWinners(board.Object, players);

            result.ShouldBeEmpty();
        }

        [Test]
        public void ReturnAllPlayersIfNoWinAndNoValidMoves()
        {
            var board = new Mock<IBoard>();
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.Width).Returns(3);
            board.Setup(mock => mock.Height).Returns(3);
            board.Setup(mock => mock.CheckBounds(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            bool flag = false;
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>())).Returns(() => !flag ? XPiece.X : OPiece.O).Callback(() => flag = !flag);

            var result = sut.GetWinners(board.Object, players);

            result.ShouldBe(players);
        }

        [Test]
        [TestCase(0, XPiece.XValue)]
        [TestCase(1, XPiece.XValue)]
        [TestCase(2, XPiece.XValue)]
        [TestCase(0, OPiece.OValue)]
        [TestCase(1, OPiece.OValue)]
        [TestCase(2, OPiece.OValue)]
        public void CheckVerticalsForWins(int x, string winningPiece)
        {
            var board = new Mock<IBoard>();
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new TicTacToeLogic();
            board.Setup(mock => mock.Width).Returns(3);
            board.Setup(mock => mock.Height).Returns(3);
            board.Setup(mock => mock.CheckBounds(It.IsAny<int>(), It.IsAny<int>())).Returns(true);
            board.Setup(mock => mock.PieceAt(x, It.IsAny<int>())).Returns(new Piece(winningPiece));
            board.Setup(mock => mock.PieceAt(It.IsNotIn(x), It.IsAny<int>())).Returns(BlankPiece.Blank);
            sut.MapPieces(players);
            var winner = sut.PlayerPieces.Where(pair => pair.Value.Value == winningPiece).Select(pair => pair.Key);

            var result = sut.GetWinners(board.Object, players);

            result.ShouldBe(winner);
            board.Verify(mock => mock.PieceAt(x, It.IsAny<int>()), Times.Exactly(3));
            board.Verify(mock => mock.PieceAt(It.IsNotIn(x), It.IsAny<int>()), Times.Exactly(3 * x));
        }
    }
}
