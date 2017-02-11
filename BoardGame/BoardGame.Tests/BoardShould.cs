namespace BoardGame.Tests
{
    using System;
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;

    [TestFixture]
    public class BoardShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new Board();

            sut.Height.ShouldBe(Board.DEFAULT_LENGTH);
            sut.Width.ShouldBe(Board.DEFAULT_LENGTH);
        }
        [Test]
        public void ConstructWithPassedInValues()
        {
            uint width = 5;
            uint height = 9;
            var sut = new Board(width, height);

            sut.Height.ShouldBe(height);
            sut.Width.ShouldBe(width);
        }

        [Test]
        public void InheritFromIBoard()
        {
            var sut = new Board();

            sut.ShouldBeAssignableTo<IBoard>();
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "Parameter name: x")]
        public void ThrowArgumentExceptionWhenPassedOutOfBoundsX()
        {
            var sut = new Board();

            sut.PieceAt(sut.Width + 1, sut.Height);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "Parameter name: y")]
        public void ThrowArgumentExceptionWhenPassedOutOfBoundsY()
        {
            var sut = new Board();

            var piece = sut.PieceAt(sut.Width, sut.Height + 1);
        }

        [Test]
        public void ReturnBlankPiece()
        {
            var sut = new Board();

            var piece = sut.PieceAt(0, 0);

            piece.Value.ShouldBe(Piece.Blank);
        }

        [Test]
        public void ContainTheRightNumberOfBlankPieces()
        {
            var sut = new Board();
            uint count = 0;
            for (uint x = 0; x < sut.Width; x++)
            {
                for (uint y = 0; y < sut.Height; y++)
                {
                    sut.PieceAt(x, y).Value.ShouldBe(Piece.Blank);
                    count++;
                }
            }
            count.ShouldBe(sut.Width * sut.Height);
        }

        [Test]
        public void ReturnPieceSet()
        {
            var sut = new Board();

            const string value = "X";
            var piece = new Piece(value);

            sut.PieceAt(0, 0, piece);

            sut.PieceAt(0, 0).Value.ShouldBe(value);
        }
    }
}
