namespace BoardGame.Tests.Implementations
{
    using Library.Implementations;
    using Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.IO;

    [TestFixture]
    public class InPlaceBoardRendererShould
    {
        [Test]
        public void InheritFromIBoardRenderer()
        {
            var sut = new InPlaceBoardRenderer();
            sut.ShouldBeAssignableTo<IBoardRenderer>();
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenConstructedWithNullParams()
        {
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(null));
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(new ConsoleRenderer(), null, () => false));
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(new ConsoleRenderer(), new StringWriter(), null));
        }

        [Test]
        public void ThrowArgumentNullExceptionWhenBoardIsNull()
        {
            var sut = new InPlaceBoardRenderer();
            Should.Throw<ArgumentNullException>(() => sut.Render(null));
        }

        [Test]
        public void FallbackToInnerRendererWhenOutputIsRedirected()
        {
            var inner = new Mock<IBoardRenderer>();
            var sw = new StringWriter();
            var sut = new InPlaceBoardRenderer(
                inner.Object,
                sw,
                isOutputRedirected: () => true);

            var board = new Board(3, 3);
            sut.Render(board);

            inner.Verify(m => m.Render(board), Times.Once);
            sw.ToString().ShouldBeEmpty();
        }

        [Test]
        public void OutputRelativeCursorMoveOnSubsequentRenders()
        {
            var inner = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            // Simulate 3 lines of output
            inner.Setup(m => m.RenderToString(board)).Returns("Line1\nLine2\nLine3");

            var sw = new StringWriter();
            var sut = new InPlaceBoardRenderer(
                inner.Object,
                sw,
                isOutputRedirected: () => false);

            // First render: writes lines with clear-line codes, no move-up code yet
            sut.Render(board);
            var firstOutput = sw.ToString();
            firstOutput.ShouldContain("\x1b[?25l"); // cursor hidden
            firstOutput.ShouldNotContain("\x1b[3F");

            // Second render: must output \x1b[3F to move cursor UP 3 lines
            sut.Render(board);
            var secondOutput = sw.ToString();
            secondOutput.ShouldContain("\x1b[3F");

            // Third render: must output \x1b[3F again
            sut.Render(board);
            var thirdOutput = sw.ToString();
            thirdOutput.Split(new[] { "\x1b[3F" }, StringSplitOptions.None).Length.ShouldBe(3);
        }

        [Test]
        public void ResetPreviousLineCountWhenResetCalled()
        {
            var inner = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            inner.Setup(m => m.RenderToString(board)).Returns("Row0\nRow1");

            var sw = new StringWriter();
            var sut = new InPlaceBoardRenderer(
                inner.Object,
                sw,
                isOutputRedirected: () => false);

            sut.Render(board); // initial render: 2 lines
            sw.ToString().ShouldNotContain("\x1b[2F");

            sut.Render(board); // second render: moves up 2 lines
            sw.ToString().ShouldContain("\x1b[2F");

            // Resetting for a new game:
            sut.Reset();
            sw.GetStringBuilder().Clear();

            sut.Render(board); // first render of new game: no move-up code
            sw.ToString().ShouldNotContain("\x1b[2F");

            sut.Render(board); // second render of new game: moves up 2 lines
            sw.ToString().ShouldContain("\x1b[2F");
        }

        [Test]
        public void DelegateRenderToStringToInnerRenderer()
        {
            var inner = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            inner.Setup(m => m.RenderToString(board)).Returns("MockedBoard");

            var sut = new InPlaceBoardRenderer(inner.Object);

            sut.RenderToString(board).ShouldBe("MockedBoard");
        }

        [Test]
        public void RestoreCursorOnDispose()
        {
            var inner = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            inner.Setup(m => m.RenderToString(board)).Returns("Line");

            var sw = new StringWriter();
            using (var sut = new InPlaceBoardRenderer(inner.Object, sw, () => false))
            {
                sut.Render(board);
                sw.ToString().ShouldContain("\x1b[?25l"); // hide
            }

            sw.ToString().ShouldContain("\x1b[?25h"); // show cursor on dispose
        }

        [Test]
        public void EnterAlternateScreenAndExitOnDisposeWhenEnabled()
        {
            var inner = new Mock<IBoardRenderer>();
            var board = new Board(3, 3);
            inner.Setup(m => m.RenderToString(board)).Returns("Line1\nLine2");

            var sw = new StringWriter();
            using (var sut = new InPlaceBoardRenderer(inner.Object, sw, () => false, useAlternateScreen: true))
            {
                sut.InAlternateScreen.ShouldBeFalse();
                sut.Render(board);
                sut.InAlternateScreen.ShouldBeTrue();
                sw.ToString().ShouldContain("\x1b[?1049h\x1b[H"); // alternate screen enter + home
            }

            sw.ToString().ShouldContain("\x1b[?1049l"); // alternate screen exit
        }

        [Test]
        public void SupportExplicitEnterAndExitAlternateScreen()
        {
            var inner = new Mock<IBoardRenderer>();
            var sw = new StringWriter();
            var sut = new InPlaceBoardRenderer(inner.Object, sw, () => false, useAlternateScreen: false);

            sut.EnterAlternateScreen();
            sut.InAlternateScreen.ShouldBeTrue();
            sw.ToString().ShouldContain("\x1b[?1049h\x1b[H");

            sut.ExitAlternateScreen();
            sut.InAlternateScreen.ShouldBeFalse();
            sw.ToString().ShouldContain("\x1b[?1049l");
        }

        [Test]
        public void NotOutputAlternateScreenWhenOutputRedirected()
        {
            var inner = new Mock<IBoardRenderer>();
            var sw = new StringWriter();
            var sut = new InPlaceBoardRenderer(inner.Object, sw, () => true, useAlternateScreen: true);

            sut.EnterAlternateScreen();
            sut.InAlternateScreen.ShouldBeFalse();
            sw.ToString().ShouldBeEmpty();
        }
    }
}
