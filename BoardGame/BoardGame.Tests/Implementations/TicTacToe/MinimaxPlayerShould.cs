namespace BoardGame.Tests.Implementations.TicTacToe
{
    using Library.Implementations;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System.Linq;

    [TestFixture]
    public class MinimaxPlayerShould
    {
        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new MinimaxPlayer();
            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new MinimaxPlayer();
            sut.Name.ShouldBe(nameof(MinimaxPlayer));
        }

        [Test]
        public void ConstructWithName()
        {
            var sut = new MinimaxPlayer("DeepAI");
            sut.Name.ShouldBe("DeepAI");
        }

        [Test]
        public void TakeWinningMoveWhenAvailable()
        {
            // Board state:
            // X | X | _
            // O | O | _
            // _ | _ | _
            // It is X's turn (2 Xs, 2 Os). X should choose (2, 0) to win!
            var board = new Board(3, 3);
            board.PieceAt(0, 0, XPiece.X);
            board.PieceAt(1, 0, XPiece.X);
            board.PieceAt(0, 1, OPiece.O);
            board.PieceAt(1, 1, OPiece.O);

            var sut = new MinimaxPlayer();
            var move = sut.GetMove(board);

            move.X.ShouldBe(2);
            move.Y.ShouldBe(0);
        }

        [Test]
        public void BlockOpponentWinningMove()
        {
            // Board state:
            // O | O | _
            // X | _ | _
            // _ | _ | _
            // Xs: 1, Os: 2 -> It is X's turn. O threatens (2, 0). X must block at (2, 0)!
            var board = new Board(3, 3);
            board.PieceAt(0, 0, OPiece.O);
            board.PieceAt(1, 0, OPiece.O);
            board.PieceAt(0, 1, XPiece.X);

            var sut = new MinimaxPlayer();
            var move = sut.GetMove(board);

            move.X.ShouldBe(2);
            move.Y.ShouldBe(0);
        }

        [Test]
        public void AlwaysDrawAgainstAnotherMinimaxPlayer()
        {
            var p1 = new MinimaxPlayer("AI-1");
            var p2 = new MinimaxPlayer("AI-2");
            var board = new Board(3, 3);
            var logic = new TicTacToeLogic();

            var game = new Game(board, new IPlayer[] { p1, p2 }, logic);
            var runner = new GameRunner(game);
            var winners = runner.RunGame();

            // A draw in TicTacToeLogic returns both players
            winners.Count().ShouldBe(2);
        }

        [Test]
        public void AlwaysWinAgainstSimplePlayer()
        {
            var ai = new MinimaxPlayer("Minimax");
            var simple = new SimplePlayer("Simple");
            var board = new Board(3, 3);
            var logic = new TicTacToeLogic();

            var game = new Game(board, new IPlayer[] { ai, simple }, logic);
            var runner = new GameRunner(game);
            var winners = runner.RunGame().ToList();

            // Minimax playing first against SimplePlayer must win
            winners.Count.ShouldBe(1);
            winners[0].Name.ShouldBe("Minimax");
        }
    }
}
