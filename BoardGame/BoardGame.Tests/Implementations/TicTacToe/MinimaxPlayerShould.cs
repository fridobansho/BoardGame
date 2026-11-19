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

        [Test]
        public void BlockImmediateFourInARowThreatOnGomokuBoard()
        {
            var logic = new TicTacToeLogic { TargetLength = 5 };
            var board = new Board(15, 15);
            var smart = new MinimaxPlayer("Smart");
            var p1 = new SimplePlayer("Human");
            logic.MapPieces(new IPlayer[] { p1, smart });
            logic.InitializeBoard(board);

            for (int x = 4; x < 8; x++)
            {
                logic.ApplyMove(board, p1, new Location(x, 7));
            }

            var move = smart.GetMove(board);
            move.ShouldNotBeNull();
            move.Y.ShouldBe(7);
            (move.X == 3 || move.X == 8).ShouldBeTrue();
        }

        [Test]
        public void CompleteFullGomokuAiVsRandomAiPlaythrough()
        {
            var logic = new TicTacToeLogic();
            var board = new Board(9, 9);
            var smart = new MinimaxPlayer("Smart");
            var random = new RandomAiPlayer("Random");
            var players = new IPlayer[] { smart, random };
            logic.MapPieces(players);
            logic.InitializeBoard(board);

            int turn = 0;
            while (!logic.GetWinners(board, players).Any() && turn < 81)
            {
                var current = players[turn % 2];
                var move = current.GetMove(board);
                move.ShouldNotBeNull();
                logic.ApplyMove(board, current, move).ShouldBeTrue();
                turn++;
            }

            logic.GetWinners(board, players).ShouldNotBeEmpty();
        }

        [Test]
        public void AvoidSelfThreeInARowInMisereMode()
        {
            // X at (0,0) and (1,0), O at (0,1) and (1,1). Under Misere, X must NOT play (2,0) which loses!
            var board = new Board(3, 3);
            board.PieceAt(0, 0, XPiece.X);
            board.PieceAt(1, 0, XPiece.X);
            board.PieceAt(0, 1, OPiece.O);
            board.PieceAt(1, 1, OPiece.O);

            var sut = new MinimaxPlayer("MisereAI")
            {
                AssignedPiece = XPiece.X,
                IsMisere = true
            };
            var move = sut.GetMove(board);
            (move.X == 2 && move.Y == 0).ShouldBeFalse();
        }
    }
}
