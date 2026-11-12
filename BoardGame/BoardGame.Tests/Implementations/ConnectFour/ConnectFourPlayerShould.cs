namespace BoardGame.Tests.Implementations.ConnectFour
{
    using Library.Implementations;
    using Library.Implementations.ConnectFour;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System.Linq;

    [TestFixture]
    public class ConnectFourPlayerShould
    {
        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new ConnectFourPlayer();
            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new ConnectFourPlayer();
            sut.Name.ShouldBe(nameof(ConnectFourPlayer));
        }

        [Test]
        public void ConstructWithName()
        {
            var sut = new ConnectFourPlayer("ConnectBot");
            sut.Name.ShouldBe("ConnectBot");
        }

        [Test]
        public void TakeWinningMoveWhenThreeInAColumn()
        {
            // Bottom of column 3 has 3 Red pieces (y = 5, 4, 3)
            var board = new Board(7, 6);
            board.PieceAt(3, 5, RedPiece.Red);
            board.PieceAt(3, 4, RedPiece.Red);
            board.PieceAt(3, 3, RedPiece.Red);

            // Equal number of Red and Yellow pieces on board
            board.PieceAt(0, 5, YellowPiece.Yellow);
            board.PieceAt(1, 5, YellowPiece.Yellow);
            board.PieceAt(2, 5, YellowPiece.Yellow);

            var sut = new ConnectFourPlayer("Red");
            var move = sut.GetMove(board);

            // Red must choose column 3 to win
            move.X.ShouldBe(3);
        }

        [Test]
        public void BlockOpponentWinningMove()
        {
            // Bottom of column 2 has 3 Yellow pieces (y = 5, 4, 3)
            var board = new Board(7, 6);
            board.PieceAt(2, 5, YellowPiece.Yellow);
            board.PieceAt(2, 4, YellowPiece.Yellow);
            board.PieceAt(2, 3, YellowPiece.Yellow);

            // Red has 2 pieces
            board.PieceAt(0, 5, RedPiece.Red);
            board.PieceAt(1, 5, RedPiece.Red);

            var sut = new ConnectFourPlayer("Red");
            var move = sut.GetMove(board);

            // Red must block column 2
            move.X.ShouldBe(2);
        }

        [Test]
        public void RunFullConnectFourGameBetweenTwoAIs()
        {
            var p1 = new ConnectFourPlayer("RedAI");
            var p2 = new ConnectFourPlayer("YellowAI");
            var board = new Board(7, 6);
            var logic = new ConnectFourLogic();

            var game = new Game(board, new IPlayer[] { p1, p2 }, logic);
            var runner = new GameRunner(game);
            var winners = runner.RunGame().ToList();

            // Game must complete with either a single winner or a draw (both players)
            winners.ShouldNotBeEmpty();
        }
    }
}
