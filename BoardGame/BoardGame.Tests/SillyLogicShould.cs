namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;
    using Moq;
    using System.Linq;
    using Library.Implementations.Silly;

    [TestFixture]
    public class SillyLogicShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new SillyLogic();

            sut.Players.Count().ShouldBe(1);
            sut.Players.First().ShouldBeOfType(typeof(SillyPlayer));
            sut.PlayerPieces.ShouldBe(new[] { Piece.Blank });
        }

        [Test]
        public void ConstructWithValuesGiven()
        {
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 2);

            var sut = new SillyLogic(players);

            sut.Players.ShouldBe(players);
            sut.PlayerPieces.Count().ShouldBe(players.Count());
            sut.PlayerPieces.ShouldBe(Enumerable.Repeat(Piece.Blank, players.Count()));
        }

        [Test]
        public void InheritFromIGameLogic()
        {
            var sut = new SillyLogic();

            sut.ShouldBeAssignableTo<IGameLogic>();
        }

        [Test]
        public void ReturnFirstPlayerPassedIn()
        {
            var board = new Mock<IBoard>();
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new SillyLogic(players);

            var result = sut.DoTurn(board.Object, players);

            result.ShouldBe(new[] { player1.Object });
        }
    }
}
