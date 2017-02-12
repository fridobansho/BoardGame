namespace BoardGame.Tests
{
    using Moq;
    using Shouldly;
    using NUnit.Framework;
    using System.Collections.Generic;
    using Library.Implementations;
    using Library.Interfaces;
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
            var players = new List<IPlayer>(new[] { player.Object });
            var gameLogic = new Mock<IGameLogic>();

            var sut = new Game(board.Object, players, gameLogic.Object);

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
        public void ApplyLogicOnEachTurn()
        {
            var player = new Mock<IPlayer>();
            var board = new Mock<IBoard>();
            var gameLogic = new Mock<IGameLogic>();
            var players = new IPlayer[] { player.Object };
            var sut = new Game(board.Object, players, gameLogic.Object);
            gameLogic.Setup(logic => logic.DoTurn(board.Object, players)).Returns(players);

            sut.DoTurns().ShouldBe(players);

            gameLogic.Verify(logic => logic.DoTurn(board.Object, players), Times.Once);
        }
    }
}
