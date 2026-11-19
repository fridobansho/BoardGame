namespace BoardGame.Tests.Implementations.Wordle
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BoardGame.Library.Implementations;
    using BoardGame.Library.Implementations.Wordle;
    using BoardGame.Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    public class WordleLogicShould
    {
        private WordleLogic _logic;
        private IBoard _board;
        private IPlayer _p1;
        private IPlayer _p2;

        [SetUp]
        public void Setup()
        {
            _logic = new WordleLogic(seed: 123);
            _board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
            _p1 = new Mock<IPlayer>().Object;
            _p2 = new Mock<IPlayer>().Object;
            _logic.MapPieces(new[] { _p1, _p2 });
            _logic.InitializeBoard(_board);
        }

        private static WordMove BuildWordMove(string word, int row)
        {
            var placements = word
                .Select((c, i) => new TilePlacement(i, row, c))
                .ToList();
            return new WordMove(placements);
        }

        [Test]
        public void ValidateWordShapeAndDictionarySetting()
        {
            _logic.IsValidMove(_board, _p1, WordMove.Pass).ShouldBeFalse();
            _logic.IsValidMove(_board, _p1, BuildWordMove("CAT", 0)).ShouldBeFalse();
            _logic.IsValidMove(_board, _p1, new WordMove(new[]
            {
                new TilePlacement(0, 0, 'C'),
                new TilePlacement(0, 0, 'R'),
                new TilePlacement(2, 0, 'A'),
                new TilePlacement(3, 0, 'N'),
                new TilePlacement(4, 0, 'E')
            })).ShouldBeFalse();

            // Non-dictionary guess rejected when AllowAnyLetters == false
            _logic.AllowAnyLetters.ShouldBeFalse();
            _logic.IsValidMove(_board, _p1, BuildWordMove("ZZZZZ", 0)).ShouldBeFalse();

            // Non-dictionary 5-letter guess allowed when AllowAnyLetters == true
            _logic.AllowAnyLetters = true;
            _logic.IsValidMove(_board, _p1, BuildWordMove("ZZZZZ", 0)).ShouldBeTrue();
        }

        [Test]
        public void DeclareWinnerAndAwardPointsOnExactMatch()
        {
            string secret = _logic.SecretWord;
            IMove move = BuildWordMove(secret, 0);

            _logic.ApplyMove(_board, _p1, move).ShouldBeTrue();
            _logic.IsSolved.ShouldBeTrue();
            _logic.HasValidMoves(_board).ShouldBeFalse();
            _logic.GetScore(_p1).ShouldBe(1000);

            for (int x = 0; x < WordleLogic.WordLength; x++)
            {
                _board.PieceAt(x, 0).Value.ShouldBe($"G{secret[x]}");
            }

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(1);
            winners[0].ShouldBeSameAs(_p1);
        }

        [Test]
        public void ReturnBothPlayersWhenSixGuessesExhaustedWithoutSolving()
        {
            _logic.AllowAnyLetters = true;
            string wrongGuess = _logic.SecretWord == "ZZZZZ" ? "QQQQQ" : "ZZZZZ";

            for (int row = 0; row < WordleLogic.MaxGuesses; row++)
            {
                _logic.HasValidMoves(_board).ShouldBeTrue();
                _logic.ApplyMove(_board, _p1, (IMove)BuildWordMove(wrongGuess, row)).ShouldBeTrue();
            }

            _logic.IsSolved.ShouldBeFalse();
            _logic.CurrentRow.ShouldBe(WordleLogic.MaxGuesses);
            _logic.HasValidMoves(_board).ShouldBeFalse();

            var winners = _logic.GetWinners(_board, new[] { _p1, _p2 }).ToList();
            winners.Count.ShouldBe(2);
        }

        [Test]
        public void ProduceDeterministicDailyWordForSameDate()
        {
            var date = new DateTime(2026, 5, 14, 0, 0, 0, DateTimeKind.Utc);
            var w1 = WordleLogic.CreateDaily(date);
            var w2 = WordleLogic.CreateDaily(date);
            var b1 = new Board(5, 6);
            var b2 = new Board(5, 6);

            w1.MapPieces(new[] { _p1, _p2 });
            w2.MapPieces(new[] { _p1, _p2 });
            w1.InitializeBoard(b1);
            w2.InitializeBoard(b2);

            w1.SecretWord.ShouldBe(w2.SecretWord);
        }

        [Test]
        public void ExportAndImportCustomStatePreservingSolvedAndWinner()
        {
            var players = new[] { _p1, _p2 };
            _logic.ApplyMove(_board, _p2, (IMove)BuildWordMove(_logic.SecretWord, 0)).ShouldBeTrue();

            var exported = _logic.ExportCustomState(players);
            exported["isSolved"].ShouldBe("1");
            exported["winnerIdx"].ShouldBe("1");

            var restored = new WordleLogic();
            restored.MapPieces(players);
            restored.InitializeBoard(new Board(5, 6));
            restored.ImportCustomState(players, exported);

            restored.SecretWord.ShouldBe(_logic.SecretWord);
            restored.IsSolved.ShouldBeTrue();
            restored.CurrentRow.ShouldBe(1);
            restored.GetWinners(_board, players).Single().ShouldBeSameAs(_p2);
        }
    }
}
