namespace BoardGame.Tests
{
    using Moq;
    using Shouldly;
    using NUnit.Framework;
    using System.Collections.Generic;
    using Library.Implementations;
    using Library.Interfaces;
    using System.Linq;
    using Library.Enumerations;

    [TestFixture]
    public class GameShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new Game();

            sut.Board.ShouldNotBeNull();
            sut.Players.ShouldNotBeNull();
        }

        [Test]
        public void ConstructWithValuesGiven()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();

            var sut = new Game(board.Object, players, gameLogic.Object);

            sut.Board.ShouldBe(board.Object);
            sut.Players.ShouldBe(players);
            sut.GameLogic.ShouldBe(gameLogic.Object);
        }

        [Test]
        public void MapPiecesToPlayers()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            gameLogic.Setup(mock => mock.MapPieces(players));

            var sut = new Game(board.Object, players, gameLogic.Object);

            gameLogic.Verify(mock => mock.MapPieces(players), Times.Once);
            sut.Board.ShouldBe(board.Object);
            sut.Players.ShouldBe(players);
            sut.GameLogic.ShouldBe(gameLogic.Object);
        }

        [Test]
        public void InheritFromIGame()
        {
            var sut = new Game();

            sut.ShouldBeAssignableTo<IGame>();
        }

        [Test]
        public void GetPlayerMoves()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            player.Setup(mock => mock.GetMove(board.Object));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            result.ShouldBeFalse();
        }

        [Test]
        public void CheckMoveValidity()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var location = new Location();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            player.Setup(mock => mock.GetMove(board.Object)).Returns(location);
            gameLogic.Setup(mock => mock.IsValidMove(board.Object, location)).Returns(true);

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            gameLogic.Verify(mock => mock.IsValidMove(board.Object, location), Times.Once);
            result.ShouldBeTrue();
        }

        [Test]
        public void GetPieceIfValidMove()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var location = new Location();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            player.Setup(mock => mock.GetMove(board.Object)).Returns(location);
            gameLogic.Setup(mock => mock.IsValidMove(board.Object, location)).Returns(true);
            gameLogic.Setup(mock => mock.GetPiece(player.Object));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            gameLogic.Verify(mock => mock.IsValidMove(board.Object, location), Times.Once);
            gameLogic.Verify(mock => mock.GetPiece(player.Object), Times.Once);
            result.ShouldBeTrue();
        }

        [Test]
        public void NotGetPieceIfInvalidMove()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var location = new Location();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            player.Setup(mock => mock.GetMove(board.Object)).Returns(location);
            gameLogic.Setup(mock => mock.IsValidMove(board.Object, location)).Returns(false);
            gameLogic.Setup(mock => mock.GetPiece(player.Object));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            gameLogic.Verify(mock => mock.IsValidMove(board.Object, location), Times.Once);
            gameLogic.Verify(mock => mock.GetPiece(player.Object), Times.Never);
            result.ShouldBeFalse();
            sut.Status.ShouldBe(Status.InProgress);
        }

        [Test]
        public void SetPieceIfValidMove()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var location = new Location();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            var piece = new Mock<IPiece>();
            board.Setup(mock => mock.PieceAt(location.X, location.Y, piece.Object));
            player.Setup(mock => mock.GetMove(board.Object)).Returns(location);
            gameLogic.Setup(mock => mock.IsValidMove(board.Object, location)).Returns(true);
            gameLogic.Setup(mock => mock.GetPiece(player.Object)).Returns(piece.Object);

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            gameLogic.Verify(mock => mock.IsValidMove(board.Object, location), Times.Once);
            gameLogic.Verify(mock => mock.GetPiece(player.Object), Times.Once);
            board.Verify(mock => mock.PieceAt(location.X, location.Y, piece.Object), Times.Once);
            result.ShouldBeTrue();
        }

        [Test]
        public void NotSetPieceIfInvalidMove()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var location = new Location();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            var piece = new Mock<IPiece>();
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IPiece>()));
            player.Setup(mock => mock.GetMove(board.Object)).Returns(location);
            gameLogic.Setup(mock => mock.IsValidMove(board.Object, location)).Returns(false);
            gameLogic.Setup(mock => mock.GetPiece(It.IsAny<IPlayer>()));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            player.Verify(mock => mock.GetMove(board.Object), Times.Once);
            gameLogic.Verify(mock => mock.IsValidMove(board.Object, location), Times.Once);
            gameLogic.Verify(mock => mock.GetPiece(It.IsAny<IPlayer>()), Times.Never);
            board.Verify(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<IPiece>()), Times.Never);
            result.ShouldBeFalse();
            sut.Status.ShouldBe(Status.InProgress);
        }

        [Test]
        public void CheckForWinnersAfterEachTurn()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 1);
            var gameLogic = new Mock<IGameLogic>();
            gameLogic.Setup(mock => mock.GetWinners(board.Object, players)).Returns(Enumerable.Empty<IPlayer>());
            gameLogic.Setup(mock => mock.GetPiece(It.IsAny<IPlayer>()));
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>()));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            board.Verify(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            gameLogic.Verify(mock => mock.GetPiece(It.IsAny<IPlayer>()), Times.Never);
            gameLogic.Verify(mock => mock.GetWinners(board.Object, players), Times.AtLeastOnce);
            result.ShouldBeFalse();
            sut.Status.ShouldBe(Status.InProgress);
        }

        [Test]
        public void CheckForWinnersAfterAllTurns()
        {
            var board = new Mock<IBoard>();
            var players = Enumerable.Empty<IPlayer>();
            var gameLogic = new Mock<IGameLogic>();
            gameLogic.Setup(mock => mock.GetWinners(board.Object, players)).Returns(players);
            gameLogic.Setup(mock => mock.GetPiece(It.IsAny<IPlayer>()));
            board.Setup(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>()));

            var sut = new Game(board.Object, players, gameLogic.Object);

            var result = sut.DoTurns();

            board.Verify(mock => mock.PieceAt(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
            gameLogic.Verify(mock => mock.GetPiece(It.IsAny<IPlayer>()), Times.Never);
            gameLogic.Verify(mock => mock.GetWinners(board.Object, players), Times.Once);
            result.ShouldBeFalse();
            sut.Status.ShouldBe(Status.InProgress);
        }
    }
}
