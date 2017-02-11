namespace BoardGame.Tests
{
    using Shouldly;
    using Library.Implementations;
    using Library.Interfaces;
    using NUnit.Framework;

    [TestFixture]
    public class SillyPlayerShould
    {
        [Test]
        public void ConstructWithDefault()
        {
            var sut = new SillyPlayer();

            sut.Name.ShouldBe(typeof(SillyPlayer).Name);
        }
        [Test]
        public void ConstructWithValueGiven()
        {
            var name = "Player1";

            var sut = new SillyPlayer(name);

            sut.Name.ShouldBe(name);
        }

        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new SillyPlayer();

            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void AlwaysMoveInSamePlace()
        {
            var location = new Location();
            var sut = new SillyPlayer();
            var board = new Board();

            var location1 = sut.GetMove(board);
            var location2 = sut.GetMove(board);

            location1.X.ShouldBe(location.X);
            location1.Y.ShouldBe(location.Y);
            location2.X.ShouldBe(location.X);
            location2.Y.ShouldBe(location.Y);
        }
    }
}
