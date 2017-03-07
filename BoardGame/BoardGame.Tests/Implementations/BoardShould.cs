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
        public void ThrowExceptionWhenGettingOutOfBoundsX()
        {
            var sut = new Board();
            var location = new Location(-1, 0);

            Action action = () => sut.PieceAt(location.X, location.Y);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldBe("Specified argument was out of the range of valid values.\r\nParameter name: x");
        }

        [Test]
        public void ThrowExceptionWhenGettingOutOfBoundsY()
        {
            var sut = new Board();
            var location = new Location(0, -1);

            Action action = () => sut.PieceAt(location.X, location.Y);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldBe("Specified argument was out of the range of valid values.\r\nParameter name: y");
        }

        [Test]
        public void ThrowExceptionWhenSettingOutOfBoundsX()
        {
            var piece = new Mock<IPiece>();
            var sut = new Board();
            var location = new Location(-1, 0);

            Action action = () => sut.PieceAt(location.X, location.Y, piece.Object);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldBe("Specified argument was out of the range of valid values.\r\nParameter name: x");
        }

        [Test]
        public void ThrowExceptionWhenSettingOutOfBoundsY()
        {
            var piece = new Mock<IPiece>();
            var sut = new Board();
            var location = new Location(0, -1);

            Action action = () => sut.PieceAt(location.X, location.Y, piece.Object);
            var exception = action.ShouldThrow<ArgumentOutOfRangeException>();
            exception.Message.ShouldBe("Specified argument was out of the range of valid values.\r\nParameter name: y");
        }
    }
}
