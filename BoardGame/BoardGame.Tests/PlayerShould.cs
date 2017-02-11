namespace BoardGame.Tests
{
    using Shouldly;
    using Library.Implementations;
    using Library.Interfaces;
    using NUnit.Framework;

    [TestFixture]
    public class PlayerShould
    {
        [Test]
        public void Construct()
        {
            var name = "Player1";

            var sut = new Player(name);

            sut.Name.ShouldBe(name);
        }

        [Test]
        public void InheritFromIPlayer()
        {
            var name = "Player1";

            var sut = new Player(name);

            sut.ShouldBeAssignableTo<IPlayer>();
        }

    }
}
