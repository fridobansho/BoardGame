namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Moq;
    using Library.Interfaces;
    using Library.Enumerations;
    using System.Linq;

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
    }
}
