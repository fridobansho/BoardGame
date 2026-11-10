namespace BoardGame.Tests.Implementations
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;

    [TestFixture]
    public class PieceShould
    {
        [Test]
        public void ConstructWithDefault()
        {
            var sut = new Piece();

            sut.Value.ShouldBe(string.Empty);
        }

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

        [Test]
        public void BeEqualWhenValuesMatch()
        {
            var piece1 = new Piece("X");
            var piece2 = new XPiece();

            piece1.ShouldBe(piece2);
            piece1.ShouldBe(XPiece.X);
            piece1.GetHashCode().ShouldBe(piece2.GetHashCode());
            piece1.ToString().ShouldBe("X");
        }

        [Test]
        public void NotBeEqualWhenValuesDiffer()
        {
            IPiece pieceX = new XPiece();
            IPiece pieceO = new OPiece();

            pieceX.ShouldNotBe(pieceO);
            pieceX.ShouldNotBe(BlankPiece.Blank);
            pieceX.Equals(null).ShouldBeFalse();
        }
    }
}
