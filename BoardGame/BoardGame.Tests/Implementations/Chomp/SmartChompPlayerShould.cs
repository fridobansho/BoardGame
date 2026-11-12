namespace BoardGame.Tests.Implementations.Chomp
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System.Linq;

    [TestFixture]
    public class SmartChompPlayerShould
    {
        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new SmartChompPlayer();
            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new SmartChompPlayer();
            sut.Name.ShouldBe(nameof(SmartChompPlayer));
        }

        [Test]
        public void ConstructWithName()
        {
            var sut = new SmartChompPlayer("SmartChomp");
            sut.Name.ShouldBe("SmartChomp");
        }

        [Test]
        public void ForceWinWhenOnlyOneChocolateRemains()
        {
            // Board with poison at (0,0) and single chocolate at (1,0)
            var board = new Board(2, 1);
            board.PieceAt(0, 0, PoisonPiece.Poison);
            board.PieceAt(1, 0, ChocolatePiece.Chocolate);

            var sut = new SmartChompPlayer();
            var move = sut.GetMove(board);

            // Eating the only chocolate leaves only poison for the opponent!
            move.X.ShouldBe(1);
            move.Y.ShouldBe(0);
        }

        [Test]
        public void EatPoisonWhenOnlyPoisonRemains()
        {
            var board = new Board(2, 2);
            board.PieceAt(0, 0, PoisonPiece.Poison);
            // all other pieces blank

            var sut = new SmartChompPlayer();
            var move = sut.GetMove(board);

            move.X.ShouldBe(0);
            move.Y.ShouldBe(0);
        }

        [Test]
        public void DefeatBasicChompPlayerOnSmallBoard()
        {
            var smart = new SmartChompPlayer("Smart");
            var basic = new ChompPlayer("Basic");
            var board = new Board(3, 2);
            var logic = new ChompLogic();

            var game = new Game(board, new IPlayer[] { smart, basic }, logic);
            var runner = new GameRunner(game);
            var winners = runner.RunGame().ToList();

            // Smart player moving first should win
            winners.Count.ShouldBe(1);
            winners[0].Name.ShouldBe("Smart");
        }
    }
}
