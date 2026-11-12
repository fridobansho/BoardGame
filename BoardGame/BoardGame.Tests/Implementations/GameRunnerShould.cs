namespace BoardGame.Tests.Implementations
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Moq;
    using Library.Interfaces;
    using Library.Enumerations;

    [TestFixture]
    public class GameRunnerShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new GameRunner();

            sut.Game.ShouldNotBeNull();
        }

        [Test]
        public void ConstructWithValueGiven()
        {
            var game = new Mock<IGame>();

            var sut = new GameRunner(game.Object);

            sut.Game.ShouldBe(game.Object);
        }

        [Test]
        public void RunGameUntilFinished()
        {
            var game = new Mock<IGame>();
            var sut = new GameRunner(game.Object);
            int count = 0;
            game.Setup(mock => mock.Status).Returns(() => (count++ >= 1) ? Status.Finished : Status.InProgress);
            game.Setup(mock => mock.DoTurns()).Returns(false);

            var results = sut.RunGame();

            game.Verify(mock => mock.Status, Times.Exactly(2));
            results.ShouldBeEmpty();
        }

        [Test]
        public void StopAfterThreeTurnsWithNoValidMoves()
        {
            var game = new Mock<IGame>();
            var sut = new GameRunner(game.Object);
            game.Setup(mock => mock.Status).Returns(Status.InProgress);
            game.Setup(mock => mock.DoTurns()).Returns(false);

            var results = sut.RunGame();

            game.Verify(mock => mock.DoTurns(), Times.Exactly(3));
            results.ShouldBeEmpty();
        }

        [Test]
        public void RenderBoardWhenRendererProvided()
        {
            var game = new Mock<IGame>();
            var renderer = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            game.Setup(mock => mock.Board).Returns(board);
            game.Setup(mock => mock.Status).Returns(Status.Finished);

            var sut = new GameRunner(game.Object, renderer.Object);
            sut.RunGame();

            renderer.Verify(r => r.Render(board), Times.Once);
        }

        [Test]
        public void ConstructWithTurnDelayMs()
        {
            var game = new Mock<IGame>();
            var renderer = new Mock<IBoardRenderer>();

            var sut = new GameRunner(game.Object, renderer.Object, 250);

            sut.TurnDelayMs.ShouldBe(250);
        }

        [Test]
        public void ReturnWinnersFromGameWhenFinished()
        {
            var game = new Mock<IGame>();
            var winner = new Mock<IPlayer>();
            var expectedWinners = new[] { winner.Object };
            var sut = new GameRunner(game.Object);
            game.SetupSequence(mock => mock.Status)
                .Returns(Status.InProgress)
                .Returns(Status.Finished);
            game.Setup(mock => mock.DoTurns()).Returns(true);
            game.Setup(mock => mock.GetWinners()).Returns(expectedWinners);

            var results = sut.RunGame();

            results.ShouldBe(expectedWinners);
        }
    }
}
