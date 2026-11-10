namespace BoardGame.Tests.Implementations
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

            var result = sut.CheckBounds(location.X, location.Y);

            result.ShouldBeTrue();
        }

        [Test]
        public void ReturnBlankPiece()
        {
            var sut = new Board();
            var location = new Location(0, 0);

            var piece = sut.PieceAt(location.X, location.Y);

            piece.Value.ShouldBe(BlankPiece.BlankValue);
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
                    sut.PieceAt(location.X, location.Y).Value.ShouldBe(BlankPiece.BlankValue);
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

            sut.PieceAt(location.X, location.Y, piece);

            sut.PieceAt(location.X, location.Y).Value.ShouldBe(value);
        }

        [Test]
        [TestCase(-1, 0)]
        [TestCase(0, -1)]
        [TestCase(3, 0)]
        [TestCase(0, 3)]
        public void ReturnFalseWhenPassedOutOfBounds(int x, int y)
        {
            var sut = new Board();

            var result = sut.CheckBounds(x, y);

            result.ShouldBeFalse();
        }

        [Test]
        public void ReturnPieceSetUsingLocationOverload()
        {
            var sut = new Board();
            const string value = "X";
            var piece = new Piece(value);
            var location = new Location(1, 2);

            sut.PieceAt(location, piece);

            sut.PieceAt(location).ShouldBe(piece);
            sut.PieceAt(location).Value.ShouldBe(value);
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenGettingWithNullLocation()
        {
            var sut = new Board();

            Action action = () => sut.PieceAt(null);

            var exception = action.ShouldThrow<ArgumentNullException>();
            exception.ParamName.ShouldBe("location");
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenSettingWithNullLocation()
        {
            var sut = new Board();
            var piece = new Piece("X");

            Action action = () => sut.PieceAt(null, piece);

            var exception = action.ShouldThrow<ArgumentNullException>();
            exception.ParamName.ShouldBe("location");
        }

        [Test]
        [TestCase(-1)]
        [TestCase(3)]
        public void ThrowExceptionWhenGettingOutOfBoundsX(int outOfBoundsX)
        {
            var sut = new Board();
            var location = new Location(outOfBoundsX, 0);

            Action action = () => sut.PieceAt(location.X, location.Y);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldStartWith("Specified argument was out of the range of valid values.");
            exception.ParamName.ShouldBe("x");
            exception.ActualValue.ShouldBe(outOfBoundsX);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(3)]
        public void ThrowExceptionWhenGettingOutOfBoundsY(int outOfBoundsY)
        {
            var sut = new Board();
            var location = new Location(0, outOfBoundsY);

            Action action = () => sut.PieceAt(location.X, location.Y);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldStartWith("Specified argument was out of the range of valid values.");
            exception.ParamName.ShouldBe("y");
            exception.ActualValue.ShouldBe(outOfBoundsY);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(3)]
        public void ThrowExceptionWhenSettingOutOfBoundsX(int outOfBoundsX)
        {
            var piece = new Mock<IPiece>();
            var sut = new Board();
            var location = new Location(outOfBoundsX, 0);

            Action action = () => sut.PieceAt(location.X, location.Y, piece.Object);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldStartWith("Specified argument was out of the range of valid values.");
            exception.ParamName.ShouldBe("x");
            exception.ActualValue.ShouldBe(outOfBoundsX);
        }

        [Test]
        [TestCase(-1)]
        [TestCase(3)]
        public void ThrowExceptionWhenSettingOutOfBoundsY(int outOfBoundsY)
        {
            var piece = new Mock<IPiece>();
            var sut = new Board();
            var location = new Location(0, outOfBoundsY);

            Action action = () => sut.PieceAt(location.X, location.Y, piece.Object);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldStartWith("Specified argument was out of the range of valid values.");
            exception.ParamName.ShouldBe("y");
            exception.ActualValue.ShouldBe(outOfBoundsY);
        }
    }
}
