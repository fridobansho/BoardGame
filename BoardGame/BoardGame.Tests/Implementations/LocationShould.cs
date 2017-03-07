namespace BoardGame.Tests.Implementations
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;

    [TestFixture]
    public class LocationShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            int x = 0;
            int y = 0;

            var sut = new Location();

            sut.X.ShouldBe(x);
            sut.Y.ShouldBe(y);
        }

        [Test]
        public void ConstructWithValuesGiven()
        {
            int x = 4;
            int y = 2;

            var sut = new Location(x, y);

            sut.X.ShouldBe(x);
            sut.Y.ShouldBe(y);
        }

        [Test]
        public void InheritFromILocation()
        {
            var sut = new Location();

            sut.ShouldBeAssignableTo<ILocation>();
        }
    }
}
