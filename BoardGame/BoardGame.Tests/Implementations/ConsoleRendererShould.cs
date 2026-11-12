namespace BoardGame.Tests.Implementations
{
    using Library.Implementations;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.IO;

    [TestFixture]
    public class ConsoleRendererShould
    {
        [Test]
        public void InheritFromIBoardRenderer()
        {
            var sut = new ConsoleRenderer();
            sut.ShouldBeAssignableTo<IBoardRenderer>();
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenConstructedWithNullWriter()
        {
            Should.Throw<ArgumentNullException>(() => new ConsoleRenderer(null));
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenBoardIsNull()
        {
            var sut = new ConsoleRenderer();
            Should.Throw<ArgumentNullException>(() => sut.RenderToString(null));
        }

        [Test]
        public void RenderEmptyBoardProperly()
        {
            var board = new Board(2, 2);
            var sut = new ConsoleRenderer();

            var rendered = sut.RenderToString(board);

            rendered.ShouldContain(" 0   1");
            rendered.ShouldContain(" 0    |   ");
            rendered.ShouldContain("---+---");
            rendered.ShouldContain(" 1    |   ");
        }

        [Test]
        public void RenderBoardWithPieces()
        {
            var board = new Board(3, 3);
            board.PieceAt(0, 0, XPiece.X);
            board.PieceAt(1, 1, OPiece.O);

            var sut = new ConsoleRenderer();
            var rendered = sut.RenderToString(board);

            rendered.ShouldContain(" 0  X |   |   ");
            rendered.ShouldContain(" 1    | O |   ");
        }

        [Test]
        public void RenderToTextWriter()
        {
            var board = new Board(1, 1);
            board.PieceAt(0, 0, XPiece.X);

            var sw = new StringWriter();
            var sut = new ConsoleRenderer(sw);

            sut.Render(board);

            var output = sw.ToString();
            output.ShouldContain("X");
        }
    }
}
