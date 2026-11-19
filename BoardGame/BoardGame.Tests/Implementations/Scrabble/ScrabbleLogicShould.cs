namespace BoardGame.Tests.Implementations.Scrabble
{
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Scrabble;
    using BoardGame.Library.Implementations.TicTacToe;
    using BoardGame.Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class ScrabbleLogicShould
    {
        private ScrabbleLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void SetUp()
        {
            _logic = new ScrabbleLogic();
            _board = new Board(15, 15);
            _p1 = new SimplePlayer("Alice");
            _p2 = new SimplePlayer("Bob");
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        [Test]
        public void DealSevenTileRacksAndReduceTileBag()
        {
            _logic.GetRack(_p1).Count.ShouldBe(7);
            _logic.GetRack(_p2).Count.ShouldBe(7);
            _logic.TileBagCount.ShouldBe(84);
        }

        [Test]
        public void RequireFirstWordToCoverCenterAndBeAtLeastTwoLetters()
        {
            _logic.SetRack(_p1, new[] { 'C', 'A', 'T', 'S', 'E', 'R', 'D' });

            // Does not cover center (7, 7) -> invalid
            var offCenter = WordMove.FromWord(_board, 0, 0, true, "CAT");
            _logic.IsValidMove(_board, _p1, offCenter).ShouldBeFalse();

            // Covers center (7, 7) with valid word -> valid
            var onCenter = WordMove.FromWord(_board, 7, 7, true, "CAT");
            _logic.IsValidMove(_board, _p1, onCenter).ShouldBeTrue();
        }

        [Test]
        public void ValidateWordsAgainstDictionary()
        {
            _logic.SetRack(_p1, new[] { 'C', 'A', 'T', 'X', 'Y', 'Z', 'Q' });

            var validWord = WordMove.FromWord(_board, 7, 7, true, "CAT");
            _logic.IsValidMove(_board, _p1, validWord).ShouldBeTrue();

            var gibberish = WordMove.FromWord(_board, 7, 7, true, "XYZQ");
            _logic.IsValidMove(_board, _p1, gibberish).ShouldBeFalse();
        }

        [Test]
        public void ApplyBonusSquareMultipliersCorrectly()
        {
            _logic.SetRack(_p1, new[] { 'C', 'A', 'T', 'E', 'R', 'S', 'D' });

            // CAT at (7,7)-(9,7): C(3) + A(1) + T(1) = 5, center (7,7) is DW -> 10 pts
            var catMove = WordMove.FromWord(_board, 7, 7, true, "CAT");
            _logic.CalculateMoveScore(_board, catMove).ShouldBe(10);
            _logic.ApplyMove(_board, _p1, catMove).ShouldBeTrue();
            _logic.GetScore(_p1).ShouldBe(10);

            // Next move: cross through A at (8,7) with E@(8,6)[DL] and T@(8,8)[DL] forming EAT vertically
            // E(1)*2 + A(1) + T(1)*2 = 5
            _logic.SetRack(_p2, new[] { 'E', 'T', 'R', 'S', 'D', 'O', 'N' });
            var eatMove = WordMove.FromWord(_board, 8, 6, false, "EAT");
            _logic.IsValidMove(_board, _p2, eatMove).ShouldBeTrue();
            _logic.CalculateMoveScore(_board, eatMove).ShouldBe(5);
            _logic.ApplyMove(_board, _p2, eatMove).ShouldBeTrue();
            _logic.GetScore(_p2).ShouldBe(5);
        }

        [Test]
        public void ApplyTripleWordAndTripleLetterBonuses()
        {
            _board.PieceAt(1, 0, new Piece("A"));
            _logic.SetRack(_p1, new[] { 'C', 'T', 'B', 'E', 'R', 'S', 'D' });

            // CAT at (0,0)-(2,0): (0,0) is TW -> (3 + 1 + 1) * 3 = 15
            var twMove = WordMove.FromWord(_board, 0, 0, true, "CAT");
            _logic.CalculateMoveScore(_board, twMove).ShouldBe(15);

            // BAT at (5,5)-(7,5): (5,5) is TL -> B(3)*3 + A(1) + T(1) = 11
            var tlMove = WordMove.FromWord(_board, 5, 5, true, "BAT");
            _logic.CalculateMoveScore(_board, tlMove).ShouldBe(11);
        }

        [Test]
        public void RequireSubsequentWordsToConnectAndReplenishRack()
        {
            _logic.SetRack(_p1, new[] { 'C', 'A', 'T', 'E', 'R', 'S', 'D' });
            _logic.ApplyMove(_board, _p1, WordMove.FromWord(_board, 7, 7, true, "CAT")).ShouldBeTrue();

            // Rack should be replenished back to 7 tiles
            _logic.GetRack(_p1).Count.ShouldBe(7);

            // Disconnected word must be rejected
            _logic.SetRack(_p2, new[] { 'D', 'O', 'G', 'A', 'E', 'I', 'N' });
            var disconnected = WordMove.FromWord(_board, 0, 0, true, "DOG");
            _logic.IsValidMove(_board, _p2, disconnected).ShouldBeFalse();

            // Connected word extending CAT -> CATS is accepted
            _logic.SetRack(_p2, new[] { 'S', 'O', 'G', 'A', 'E', 'I', 'N' });
            var connected = WordMove.FromWord(_board, 7, 7, true, "CATS");
            _logic.IsValidMove(_board, _p2, connected).ShouldBeTrue();
            _logic.ApplyMove(_board, _p2, connected).ShouldBeTrue();
            _board.PieceAt(10, 7).Value.ShouldBe("S");
        }

        [Test]
        public void TrackCumulativeScoresAndDeclareWinnerOnConsecutivePasses()
        {
            _logic.SetRack(_p1, new[] { 'C', 'A', 'T', 'E', 'R', 'S', 'D' });
            _logic.ApplyMove(_board, _p1, WordMove.FromWord(_board, 7, 7, true, "CAT")).ShouldBeTrue();
            _logic.GetScore(_p1).ShouldBe(10);

            _logic.ApplyMove(_board, _p2, WordMove.Pass).ShouldBeTrue();
            _logic.GetWinners(_board, new[] { _p1, _p2 }).ShouldBeEmpty();

            _logic.ApplyMove(_board, _p1, WordMove.Pass).ShouldBeTrue();
            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBe(_p1);
        }

        [Test]
        public void SmartPlayerReturnsValidOpeningMove()
        {
            var board = new Board(15, 15);
            var smart = new SmartScrabblePlayer("Smart");
            var random = new RandomAiPlayer("Random");
            var logic = new ScrabbleLogic();
            logic.MapPieces(new IPlayer[] { smart, random });
            logic.InitializeBoard(board);
            logic.SetRack(smart, new[] { 'C', 'A', 'T', 'S', 'E', 'R', 'D' });

            var move = smart.GetMove(board);
            move.ShouldBeAssignableTo<WordMove>();
            logic.IsValidMove(board, smart, move).ShouldBeTrue();
        }

        [Test]
        public void CompleteFullAiVsAiPlaythroughViaGameRunner()
        {
            var board = new Board(15, 15);
            var smart = new SmartScrabblePlayer("SmartBot");
            var random = new RandomAiPlayer("RandomBot");
            var players = new IPlayer[] { smart, random };
            var logic = new ScrabbleLogic();

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game);

            var winners = runner.RunGame().ToList();
            winners.ShouldNotBeEmpty();
            (logic.GetScore(smart) + logic.GetScore(random)).ShouldBeGreaterThan(0);
        }
    }
}
