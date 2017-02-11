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

            sut.Status.ShouldBe(Status.InProgress);
            sut.Board.ShouldNotBeNull();
            sut.Players.ShouldNotBeNull();
        }
        [Test]
        public void ConstructWithValuesGiven()
        {
            var board = new Board();
            var players = new List<IPlayer>(new[] { new SillyPlayer() });

            var sut = new Game(board, players);

            sut.Status.ShouldBe(Status.InProgress);
            sut.Board.ShouldBe(board);
            sut.Players.ShouldBe(players);
        }

        [Test]
        public void InheritFromIGame()
        {
            var sut = new Game();

            sut.ShouldBeAssignableTo<IGame>();
        }

        [Test]
        public void AskPlayersForTheirMovesEachTurn()
        {
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var board = new Board();
            var sut = new Game(board, new IPlayer[] { player1.Object, player2.Object });
            player1.Setup(player => player.GetMove(board)).Returns(new Location(1, 2));
            player2.Setup(player => player.GetMove(board)).Returns(new Location(2, 1));

            sut.DoTurns().ShouldBeTrue();

            player1.VerifyAll();
            player2.VerifyAll();
        }

        [Test]
        public void GivePlayers3ChancesToMakeValidMoves()
        {
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var board = new Board();
            var sut = new Game(board, new IPlayer[] { player1.Object, player2.Object });
            player1.Setup(player => player.GetMove(board)).Returns(new Location(-1, -1));
            player2.Setup(player => player.GetMove(board)).Returns(new Location(-1, -1));

            sut.DoTurns().ShouldBeFalse();

            player1.Verify(player => player.GetMove(board), Times.Exactly(3));
            player2.Verify(player => player.GetMove(board), Times.Exactly(3));
        }
    }
}
