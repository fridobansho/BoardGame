namespace BoardGame.Tests
{
    using Shouldly;
    using NUnit.Framework;
    using System.Collections.Generic;
    using Library.Implementations;
    using Library.Interfaces;
    using Library.Enumerations;

    [TestFixture]
    public class GameShould
    {
        [Test]
        public void Construct()
        {
            var board = new Board();
            var players = new List<IPlayer>(1);
            string name = "Player1";
            var player = new Player(name);
            players.Add(player);
            var sut = new Game(board, players);

            sut.Status.ShouldBe(Status.InProgress);
            sut.Board.ShouldBe(board);
            sut.Players.ShouldBe(players);
        }

        [Test]
        public void InheritFromIGame()
        {
            var board = new Board();
            var players = new List<IPlayer>(1);
            string name = "Player1";
            var player = new Player(name);
            players.Add(player);
            var sut = new Game(board, players);

            sut.ShouldBeAssignableTo<IGame>();
        }
    }
}
