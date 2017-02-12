namespace BoardGame.Tests
{
    using Moq;
    using Shouldly;
    using NUnit.Framework;
    using Library.Interfaces;
    using Library.Implementations.TicTacToe;
    using System;
    using System.Linq;

    [TestFixture]
    public class TicTacToeLogicShould
    {
        [Test]
        public void ConstructWithValuesGiven()
        {
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 2);

            var sut = new TicTacToeLogic(players);

            sut.Players.ShouldBe(players);
            sut.PlayerPieces.ShouldBe(new[] { XPiece.X, OPiece.O });
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowExceptionWhenCalledWithEmpty()
        {
            var player = new Mock<IPlayer>();
            var players = new[] { player.Object };

            var sut = new TicTacToeLogic(Enumerable.Empty<IPlayer>());
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowExceptionWhenCalledWithSinglePlayer()
        {
            var player = new Mock<IPlayer>();
            var players = new[] { player.Object };

            var sut = new TicTacToeLogic(players);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "players.Count()")]
        public void ThrowExceptionWhenCalledWithThreePlayers()
        {
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 3);

            var sut = new TicTacToeLogic(players);
        }

        [Test]
        public void ReturnEmptyForAnEmptyBoard()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 2);
            var sut = new TicTacToeLogic(players);

            var result = sut.DoTurn(board.Object, players);

            result.ShouldBeEmpty();
            player.Verify(mock => mock.GetMove(board.Object), Times.Exactly(2));
        }

        [Test]
        public void AskPlayersForTheirMoves()
        {
            var board = new Mock<IBoard>();
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 2);
            var sut = new TicTacToeLogic(players);
            player.Setup(mock => mock.GetMove(board.Object));

            var result = sut.DoTurn(board.Object, players);

            result.ShouldBeEmpty();
            player.Verify(mock => mock.GetMove(board.Object), Times.Exactly(2));
        }
    }
}
