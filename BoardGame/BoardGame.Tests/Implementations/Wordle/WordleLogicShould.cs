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

        [TestCase("APPLE", "PAPAL", "YYGBY")]
        [TestCase("CRANE", "EERIE", "BBYBG")]
        [TestCase("ROBOT", "FLOOR", "BBYGY")]
        [TestCase("SPEED", "CREEP", "BBGGY")]
        [TestCase("ABBEY", "KEBAB", "BYGYY")]
        [TestCase("MUMMY", "MOMMY", "GBGGG")]
        public void EvaluateDuplicateLettersWithStandardWordleRules(string secret, string guess, string expectedColors)
        {
            var logic = new WordleLogic();
            var board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
            logic.MapPieces(new[] { _p1, _p2 });
            logic.InitializeBoard(board);
            logic.ImportCustomState(new[] { _p1, _p2 }, new Dictionary<string, string>
            {
                ["secretWord"] = secret
            });

            logic.ApplyMove(board, _p1, (IMove)BuildWordMove(guess, 0)).ShouldBeTrue();
            string actualColors = new string(Enumerable.Range(0, WordleLogic.WordLength)
                .Select(x => board.PieceAt(x, 0).Value[0])
                .ToArray());
            actualColors.ShouldBe(expectedColors);
        }

        [Test]
        public void ExposeCuratedAnswersAndBroadValidGuessesInWordleDictionary()
        {
            WordleDictionary.AnswerWords.Count.ShouldBeGreaterThanOrEqualTo(2000);
            WordleDictionary.AllowedGuesses.Count.ShouldBeGreaterThanOrEqualTo(10000);

            foreach (string answer in WordleDictionary.AnswerWords)
            {
                answer.Length.ShouldBe(WordleLogic.WordLength);
                answer.All(c => c >= 'A' && c <= 'Z').ShouldBeTrue();
                WordleDictionary.IsValidGuess(answer).ShouldBeTrue();
            }

            foreach (string validGuess in new[]
            {
                "ABOUT", "AFTER", "AUDIO", "ADIEU", "AROSE", "ALIVE", "AGENT",
                "COLOR", "MOULD", "LITRE", "LITER", "MAKES", "BOOKS", "FRIED", "BEING"
            })
            {
                WordleDictionary.IsValidGuess(validGuess).ShouldBeTrue($"Expected {validGuess} to be a valid guess.");
            }

            foreach (string invalidGuess in new[] { "ZZZZZ", "QXZJK", "ASDFG", "CAT", "" })
            {
                WordleDictionary.IsValidGuess(invalidGuess).ShouldBeFalse($"Expected {invalidGuess} to be rejected.");
            }

            foreach (string nonAnswer in new[] { "PLUTO", "XEROX", "SATAN", "HDQRS" })
            {
                WordleDictionary.IsAnswerWord(nonAnswer).ShouldBeFalse($"Expected {nonAnswer} to be excluded from AnswerWords.");
            }
        }

        [Test]
        public void ProduceDistinctNonAlphabeticalDailyWordsAcrossConsecutiveDates()
        {
            var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var words = new List<string>();
            for (int day = 0; day < 60; day++)
            {
                var daily = WordleLogic.CreateDaily(startDate.AddDays(day));
                var board = new Board(WordleLogic.WordLength, WordleLogic.MaxGuesses);
                daily.MapPieces(new[] { _p1, _p2 });
                daily.InitializeBoard(board);
                words.Add(daily.SecretWord);
            }

            words.Distinct(StringComparer.Ordinal).Count().ShouldBe(60);

            bool strictlySortedAsc = words.Zip(words.Skip(1), (a, b) => string.CompareOrdinal(a, b) <= 0).All(x => x);
            bool strictlySortedDesc = words.Zip(words.Skip(1), (a, b) => string.CompareOrdinal(a, b) >= 0).All(x => x);
            strictlySortedAsc.ShouldBeFalse();
            strictlySortedDesc.ShouldBeFalse();
        }

        [Test]
        public void RejectMismatchedRowCoordinatesAndIgnoreMalformedImportedSecretWord()
        {
            // CurrentRow is 0 -> placing tiles on row 1 must be rejected
            _logic.IsValidMove(_board, _p1, BuildWordMove("CRANE", 1)).ShouldBeFalse();

            string originalSecret = _logic.SecretWord;
            _logic.ImportCustomState(new[] { _p1, _p2 }, new Dictionary<string, string>
            {
                ["secretWord"] = "12345"
            });
            _logic.SecretWord.ShouldBe(originalSecret);

            _logic.ImportCustomState(new[] { _p1, _p2 }, new Dictionary<string, string>
            {
                ["secretWord"] = "AB$DE"
            });
            _logic.SecretWord.ShouldBe(originalSecret);

            _logic.ImportCustomState(new[] { _p1, _p2 }, new Dictionary<string, string>
            {
                ["secretWord"] = "CAT"
            });
            _logic.SecretWord.ShouldBe(originalSecret);
        }
    }
}
