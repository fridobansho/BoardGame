namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;

    [TestFixture]
    public class PieceShould
    {
        [Test]
        public void Construct()
        {
            var sut = new Piece();

            sut.ShouldBeOfType<Piece>();
            sut.Value.ShouldBe(Piece.Blank);
        }

        [Test]
        public void InheritFromIPiece()
        {
            var sut = new Piece();

            sut.ShouldBeAssignableTo<IPiece>();
        }

        [Test]
        public void TakeValueGiven()
        {
            string value = "X";
            var sut = new Piece(value);

            sut.Value.ShouldBe(value);
        }
    }
}
