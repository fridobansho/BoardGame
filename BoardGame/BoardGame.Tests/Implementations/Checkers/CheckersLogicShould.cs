namespace BoardGame.Tests.Implementations.Checkers
{
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Checkers;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class CheckersLogicShould
    {
        private CheckersLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new CheckersLogic();
            _board = new Board(8, 8);
            _p1 = new SimplePlayer("Red");
            _p2 = new SimplePlayer("Black");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        private static void ClearBoard(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    board.PieceAt(x, y, BlankPiece.Blank);
                }
            }
        }

        [Test]
        public void InitializeTwelvePiecesPerPlayerOnDarkSquares()
        {
            int redCount = 0;
            int blackCount = 0;

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 8; y++)
                {
                    var val = _board.PieceAt(x, y).Value;
                    if (val == RedCheckerPiece.Red.Value)
                    {
                        ((x + y) % 2).ShouldBe(1);
                        y.ShouldBeGreaterThanOrEqualTo(5);
                        redCount++;
                    }
                    else if (val == BlackCheckerPiece.Black.Value)
                    {
                        ((x + y) % 2).ShouldBe(1);
                        y.ShouldBeLessThanOrEqualTo(2);
                        blackCount++;
                    }
                }
            }

            redCount.ShouldBe(12);
            blackCount.ShouldBe(12);
        }

        [Test]
        public void AllowForwardDiagonalMoveAndRejectBackwardMoveForRegularPiece()
        {
            ClearBoard(_board);
            _board.PieceAt(3, 4, RedCheckerPiece.Red);

            // Red moves upward (-Y)
            _logic.IsValidMove(_board, _p1, new PieceMove(3, 4, 2, 3)).ShouldBeTrue();
            _logic.IsValidMove(_board, _p1, new PieceMove(3, 4, 2, 5)).ShouldBeFalse();

            _logic.ApplyMove(_board, _p1, new PieceMove(3, 4, 2, 3)).ShouldBeTrue();
            _board.PieceAt(3, 4).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(2, 3).Value.ShouldBe(RedCheckerPiece.Red.Value);
        }

        [Test]
        public void ExecuteSingleJumpCaptureAndRemoveOpponentPiece()
        {
            ClearBoard(_board);
            _board.PieceAt(2, 5, RedCheckerPiece.Red);
            _board.PieceAt(3, 4, BlackCheckerPiece.Black);

            var jump = new PieceMove(2, 5, 4, 3);
            _logic.IsValidMove(_board, _p1, jump).ShouldBeTrue();
            _logic.ApplyMove(_board, _p1, jump).ShouldBeTrue();

            _board.PieceAt(2, 5).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(3, 4).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(4, 3).Value.ShouldBe(RedCheckerPiece.Red.Value);
        }

        [Test]
        public void EnforceMandatoryCaptureRule()
        {
            ClearBoard(_board);
            _board.PieceAt(2, 5, RedCheckerPiece.Red);
            _board.PieceAt(6, 5, RedCheckerPiece.Red);
            _board.PieceAt(3, 4, BlackCheckerPiece.Black);

            // Non-capture diagonal step must be rejected while a capture is available
            _logic.IsValidMove(_board, _p1, new PieceMove(6, 5, 7, 4)).ShouldBeFalse();
            _logic.ApplyMove(_board, _p1, new PieceMove(6, 5, 7, 4)).ShouldBeFalse();

            // Capture jump is valid
            _logic.IsValidMove(_board, _p1, new PieceMove(2, 5, 4, 3)).ShouldBeTrue();
        }

        [Test]
        public void ExecuteMultiJumpCaptureChain()
        {
            ClearBoard(_board);
            _board.PieceAt(1, 6, RedCheckerPiece.Red);
            _board.PieceAt(2, 5, BlackCheckerPiece.Black);
            _board.PieceAt(4, 3, BlackCheckerPiece.Black);

            var multiJump = new PieceMove(1, 6, 5, 2, new ILocation[]
            {
                new Location(1, 6),
                new Location(3, 4),
                new Location(5, 2)
            });

            _logic.IsValidMove(_board, _p1, multiJump).ShouldBeTrue();
            _logic.ApplyMove(_board, _p1, multiJump).ShouldBeTrue();

            _board.PieceAt(1, 6).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(2, 5).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(4, 3).Value.ShouldBe(BlankPiece.BlankValue);
            _board.PieceAt(5, 2).Value.ShouldBe(RedCheckerPiece.Red.Value);
        }

        [Test]
        public void PromoteToKingAndAllowBackwardMovement()
        {
            ClearBoard(_board);
            _board.PieceAt(1, 1, RedCheckerPiece.Red);
            _board.PieceAt(6, 6, BlackCheckerPiece.Black);

            // Red reaches row 0 -> promotes to RK
            _logic.ApplyMove(_board, _p1, new PieceMove(1, 1, 0, 0)).ShouldBeTrue();
            _board.PieceAt(0, 0).Value.ShouldBe(RedKingPiece.RedKing.Value);

            // Black reaches row 7 -> promotes to BK
            _logic.ApplyMove(_board, _p2, new PieceMove(6, 6, 7, 7)).ShouldBeTrue();
            _board.PieceAt(7, 7).Value.ShouldBe(BlackKingPiece.BlackKing.Value);

            // Promoted Red King can move backward (+Y)
            _logic.IsValidMove(_board, _p1, new PieceMove(0, 0, 1, 1)).ShouldBeTrue();
            _logic.ApplyMove(_board, _p1, new PieceMove(0, 0, 1, 1)).ShouldBeTrue();
            _board.PieceAt(1, 1).Value.ShouldBe(RedKingPiece.RedKing.Value);
        }

        [Test]
        public void DetectWinWhenOpponentHasNoPiecesOrMoves()
        {
            ClearBoard(_board);
            _board.PieceAt(3, 4, RedCheckerPiece.Red);

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_p1);
        }

        [Test]
        public void SmartPlayerReturnsLegalMove()
        {
            var smart = new SmartCheckersPlayer("Smart", RedCheckerPiece.Red.Value);
            var move = smart.GetMove(_board);

            move.ShouldBeAssignableTo<PieceMove>();
            _logic.IsValidMove(_board, _p1, move).ShouldBeTrue();
        }

        [Test]
        public void CompleteFullAiVsAiPlaythroughViaGameRunner()
        {
            var board = new Board(8, 8);
            var smart = new SmartCheckersPlayer("Red (Smart)");
            var random = new RandomCheckersPlayer("Black (Easy)");
            var players = new IPlayer[] { smart, random };
            var logic = new CheckersLogic();

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game);

            var winners = runner.RunGame().ToList();
            winners.ShouldNotBeEmpty();
        }
    }
}
