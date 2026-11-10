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

        [Test]
        public void BeEqualWhenCoordinatesMatch()
        {
            var location1 = new Location(2, 3);
            var location2 = new Location(2, 3);

            location1.ShouldBe(location2);
            location1.GetHashCode().ShouldBe(location2.GetHashCode());
        }

        [Test]
        public void NotBeEqualWhenCoordinatesDiffer()
        {
            var location1 = new Location(2, 3);
            var location2 = new Location(3, 2);

            location1.ShouldNotBe(location2);
            location1.Equals(null).ShouldBeFalse();
        }

        [Test]
        public void FormatCoordinatesInToString()
        {
            var sut = new Location(4, 2);

            sut.ToString().ShouldBe("(4, 2)");
        }
    }
}
