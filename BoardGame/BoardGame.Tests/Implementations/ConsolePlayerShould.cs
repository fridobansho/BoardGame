namespace BoardGame.Tests.Implementations
{
    using Library.Implementations;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.IO;

    [TestFixture]
    public class ConsolePlayerShould
    {
        [Test]
        public void InheritFromIPlayer()
        {
            var sut = new ConsolePlayer();
            sut.ShouldBeAssignableTo<IPlayer>();
        }

        [Test]
        public void ConstructWithDefaults()
        {
            var sut = new ConsolePlayer();
            sut.Name.ShouldBe("Human");
        }

        [Test]
        public void ConstructWithName()
        {
            var sut = new ConsolePlayer("Alice");
            sut.Name.ShouldBe("Alice");
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenConstructedWithNullParams()
        {
            Should.Throw<ArgumentNullException>(() => new ConsolePlayer(null, Console.In, Console.Out));
            Should.Throw<ArgumentNullException>(() => new ConsolePlayer("Human", null, Console.Out));
            Should.Throw<ArgumentNullException>(() => new ConsolePlayer("Human", Console.In, null));
        }

        [Test]
        public void ParseValidCommaSeparatedMove()
        {
            var input = new StringReader("1, 2\n");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Alice", input, output);
            var board = new Board(3, 3);

            var move = sut.GetMove(board);

            move.X.ShouldBe(1);
            move.Y.ShouldBe(2);
            output.ToString().ShouldContain("Alice, enter your move");
        }

        [Test]
        public void ParseValidSpaceSeparatedMove()
        {
            var input = new StringReader("0 1\n");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Bob", input, output);
            var board = new Board(3, 3);

            var move = sut.GetMove(board);

            move.X.ShouldBe(0);
            move.Y.ShouldBe(1);
        }

        [Test]
        public void RepromptWhenInputFormatIsInvalid()
        {
            var input = new StringReader("invalid\nfoo bar baz\n2, 1\n");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Alice", input, output);
            var board = new Board(3, 3);

            var move = sut.GetMove(board);

            move.X.ShouldBe(2);
            move.Y.ShouldBe(1);
            output.ToString().ShouldContain("Invalid format");
        }

        [Test]
        public void RepromptWhenInputIsOutOfBounds()
        {
            var input = new StringReader("5, 5\n-1, 0\n2, 2\n");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Alice", input, output);
            var board = new Board(3, 3);

            var move = sut.GetMove(board);

            move.X.ShouldBe(2);
            move.Y.ShouldBe(2);
            output.ToString().ShouldContain("out of bounds");
        }

        [Test]
        public void ReturnNegativeCoordinatesOnEndOfInput()
        {
            var input = new StringReader("");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Alice", input, output);
            var board = new Board(3, 3);

            var move = sut.GetMove(board);

            move.X.ShouldBe(-1);
            move.Y.ShouldBe(-1);
        }

        [Test]
        public void ReturnNegativeCoordinatesWhenBoardIsNull()
        {
            var input = new StringReader("1, 1\n");
            var output = new StringWriter();
            var sut = new ConsolePlayer("Alice", input, output);

            var move = sut.GetMove(null);

            move.X.ShouldBe(-1);
            move.Y.ShouldBe(-1);
        }
    }
}
