namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;
    using System;
    using Moq;

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
        public void ConstructWithValuesGiven()
        {
            int width = 5;
            int height = 9;

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

        [Test]
        public void ReturnTrueWhenPassedInBounds()
        {
            var sut = new Board();
            var location = new Location(sut.Width - 1, sut.Height - 1);

            var result = sut.CheckBounds(location);

            result.ShouldBeTrue();
        }

        [Test]
        public void ReturnFalseWhenPassedOutOfBounds()
        {
            var sut = new Board();
            var location = new Location(sut.Width, sut.Height);

            var result = sut.CheckBounds(location);

            result.ShouldBeFalse();
        }

        [Test]
        public void ReturnFalseWhenPassedOutOfBoundsX()
        {
            var sut = new Board();
            var location = new Location(sut.Width, sut.Height - 1);

            var result = sut.CheckBounds(location);

            result.ShouldBeFalse();
        }

        [Test]
        public void ReturnFalseWhenPassedOutOfBoundsY()
        {
            var sut = new Board();
            var location = new Location(sut.Width - 1, sut.Height);

            var result = sut.CheckBounds(location);

            result.ShouldBeFalse();
        }

        [Test]
        public void ReturnBlankPiece()
        {
            var sut = new Board();
            var location = new Location(0, 0);

            var piece = sut.PieceAt(location);

            piece.Value.ShouldBe(Piece.BlankValue);
        }

        [Test]
        public void ContainTheRightNumberOfBlankPieces()
        {
            var sut = new Board();

            int count = 0;
            for (int x = 0; x < sut.Width; x++)
            {
                for (int y = 0; y < sut.Height; y++)
                {
                    var location = new Location(x, y);
                    sut.PieceAt(location).Value.ShouldBe(Piece.BlankValue);
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
            var location = new Location(0, 0);

            sut.PieceAt(location, piece);

            sut.PieceAt(location).Value.ShouldBe(value);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "location")]
        public void ThrowExceptionWhenGettingOutOfBounds()
        {
            var sut = new Board();
            var location = new Location(-1, -1);

            var piece = sut.PieceAt(location);
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "location")]
        public void ThrowExceptionWhenSettingOutOfBounds()
        {
            var piece = new Mock<IPiece>();
            var sut = new Board();
            var location = new Location(-1, -1);

            sut.PieceAt(location, piece.Object);
        }
    }
}
