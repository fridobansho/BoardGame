namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Enumerations;

    [TestFixture]
    public class GameRunnerShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new GameRunner();
            
        }

        [Test]
        public void ConstructWithValueGiven()
        {
            var game = new Game();

            var sut = new GameRunner(game);

            sut.Game.ShouldBe(game);
        }
    }
}
