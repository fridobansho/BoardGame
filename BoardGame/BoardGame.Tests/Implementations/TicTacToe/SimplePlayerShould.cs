namespace BoardGame.Tests.Implementations.TicTacToe
{
    using Shouldly;
    using Library.Implementations;
    using Library.Interfaces;
    using NUnit.Framework;
    using Library.Implementations.TicTacToe;

    [TestFixture]
    public class SimplePlayerShould
    {
        [Test]
        public void ConstructWithDefault()
        {
            var sut = new SimplePlayer();

            sut.Name.ShouldBe(typeof(SimplePlayer).Name);
        }
        [Test]
        public void ConstructWithValueGiven()
        {
            var name = "Player1";

            var sut = new SimplePlayer(name);

            sut.Name.ShouldBe(name);
        }

        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new SimplePlayer();

            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void MoveInFirstAvailableSpace()
        {
            var location = new Location();
            var sut = new SimplePlayer();
            var board = new Board();

            var location1 = sut.GetMove(board);
            board.PieceAt(location1.X, location1.Y, XPiece.X);
            var location2 = sut.GetMove(board);
            board.PieceAt(location2.X, location2.Y, XPiece.X);
            var location3 = sut.GetMove(board);

            location1.X.ShouldBe(location.X);
            location1.Y.ShouldBe(location.Y);
            location2.X.ShouldBe(location.X);
            location2.Y.ShouldBe(location.Y + 1);
            location3.X.ShouldBe(location.X);
            location3.Y.ShouldBe(location.Y + 2);
        }
    }
}
