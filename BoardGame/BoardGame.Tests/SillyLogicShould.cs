namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using Library.Implementations;
    using Library.Interfaces;
    using Moq;
    using System.Linq;
    using Library.Implementations.Silly;
    using System.Collections.Generic;
    using System;

    [TestFixture]
    public class SillyLogicShould
    {
        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new SillyLogic();

            sut.PlayerPieces.ShouldBeEmpty();
        }

        [Test]
        public void InheritFromIGameLogic()
        {
            var sut = new SillyLogic();

            sut.ShouldBeAssignableTo<IGameLogic>();
        }

        [Test, ExpectedException(typeof(ArgumentNullException), MatchType = MessageMatch.Contains, ExpectedMessage = "players")]
        public void ThrowArgumentNullExceptionWhenPassedNull()
        {
            var sut = new SillyLogic();

            sut.MapPieces(null);
        }

        [Test]
        public void CreateEmptyMapWhenPassedEmpty()
        {
            var players = Enumerable.Empty<IPlayer>();
            var sut = new SillyLogic();

            sut.MapPieces(players);

            sut.PlayerPieces.ShouldBeEmpty();
        }

        [Test]
        public void MapAsBlankForAllPlayers()
        {
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var player3 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object, player3.Object };
            var blanks = Enumerable.Repeat(Piece.Blank, players.Count());
            var pairs = players.Zip(blanks, (p, b) => new KeyValuePair<IPlayer, IPiece>(p, b));
            var dictionary = new Dictionary<IPlayer, IPiece>();
            var sut = new SillyLogic();
            foreach(var pair in pairs)
            {
                dictionary.Add(pair.Key, pair.Value);
            }

            sut.MapPieces(players);

            sut.PlayerPieces.ShouldBe(dictionary);
        }

        [Test]
        [TestCase(-1, -1)]
        [TestCase(0, -1)]
        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        public void AlwaysReturnTrue(int x, int y)
        {
            var board = new Mock<IBoard>();
            var location = new Location(x, y);
            var sut = new SillyLogic();

            var result = sut.IsValidMove(board.Object, location);

            result.ShouldBeTrue();
        }

        [Test, ExpectedException(typeof(ArgumentOutOfRangeException), MatchType = MessageMatch.Contains, ExpectedMessage = "player")]
        public void ThrowsExceptionIfNoMappedPlayer()
        {
            var player = new Mock<IPlayer>();
            var sut = new SillyLogic();

            var result = sut.GetPiece(player.Object);
        }

        [Test]
        public void ReturnMappedPiece()
        {
            var player1 = new Mock<IPlayer>();
            var player2 = new Mock<IPlayer>();
            var players = new[] { player1.Object, player2.Object };
            var sut = new SillyLogic();
            sut.MapPieces(players);

            var result1 = sut.GetPiece(player1.Object);
            var result2 = sut.GetPiece(player2.Object);

            result1.ShouldBe(Piece.Blank);
            result2.ShouldBe(Piece.Blank);
        }

        [Test]
        public void ReturnNullWhenPassedNull()
        {
            var board = new Mock<IBoard>();
            var sut = new SillyLogic();

            var result = sut.GetWinners(board.Object, null);

            result.ShouldBeNull();
        }

        [Test]
        public void ReturnEmptyWhenPassedEmpty()
        {
            var players = Enumerable.Empty<IPlayer>();
            var board = new Mock<IBoard>();
            var sut = new SillyLogic();

            var result = sut.GetWinners(board.Object, players);

            result.ShouldBeEmpty();
            result.ShouldBe(players);
        }

        [Test]
        public void ReturnPlayersWhenPassedPlayers()
        {
            var player = new Mock<IPlayer>();
            var players = Enumerable.Repeat(player.Object, 2);
            var board = new Mock<IBoard>();
            var sut = new SillyLogic();

            var result = sut.GetWinners(board.Object, players);

            result.ShouldBe(players);
        }
    }
}
