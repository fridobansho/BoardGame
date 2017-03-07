namespace BoardGame.Tests.Implementations
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;

    [TestFixture]
    public class PieceShould
    {

        [Test]
        [TestCase("X")]
        [TestCase("O")]
        [TestCase(" ")]
        public void ConstructWithValueGiven(string value)
        {
            var sut = new Piece(value);

            sut.Value.ShouldBe(value);
        }

        [Test]
        public void InheritFromIPiece()
        {
            var sut = new Piece();

            sut.ShouldBeAssignableTo<IPiece>();
        }
    }
}
