namespace BoardGame.Tests.Implementations.Chomp
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Interfaces;

    [TestFixture]
    public class ChompPlayerShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new ChompPlayer();

            sut.Name.ShouldBe(nameof(ChompPlayer));
            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void ConstructWithValueGiven()
        {
            var sut = new ChompPlayer("Alice");

            sut.Name.ShouldBe("Alice");
        }

        [Test]
        public void AvoidPoisonBlockIfChocolateAvailable()
        {
            var sut = new ChompPlayer();
            var board = new Board(3, 3);
            var logic = new ChompLogic();
            logic.InitializeBoard(board);

            var move = sut.GetMove(board);

            // Should pick from the edge (e.g. 2, 2), not (0, 0)
            move.ShouldNotBe(new Location(0, 0));
        }

        [Test]
        public void PickPoisonBlockWhenOnlyPoisonRemains()
        {
            var sut = new ChompPlayer();
            var board = new Board(2, 2);
            // Leave only (0, 0) as poison
            board.PieceAt(0, 0, PoisonPiece.Poison);

            var move = sut.GetMove(board);

            move.ShouldBe(new Location(0, 0));
        }
    }
}
