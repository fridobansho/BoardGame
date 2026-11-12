namespace BoardGame.Tests.Implementations
{
    using Library.Implementations;
    using Library.Interfaces;
    using Moq;
    using NUnit.Framework;
    using Shouldly;
    using System;

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
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(new ConsoleRenderer(), null, () => (0, 0), () => false));
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(new ConsoleRenderer(), (l, t) => { }, null, () => false));
            Should.Throw<ArgumentNullException>(() => new InPlaceBoardRenderer(new ConsoleRenderer(), (l, t) => { }, () => (0, 0), null));
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
            var setCursorCalled = false;
            var sut = new InPlaceBoardRenderer(
                inner.Object,
                (l, t) => { setCursorCalled = true; },
                () => (5, 10),
                isOutputRedirected: () => true);

            var board = new Board(3, 3);
            sut.Render(board);

            inner.Verify(m => m.Render(board), Times.Once);
            setCursorCalled.ShouldBeFalse();
        }

        [Test]
        public void RecordInitialPositionOnFirstRenderAndResetCursorOnSubsequentRenders()
        {
            var inner = new Mock<IBoardRenderer>();
            int setLeft = -1, setTop = -1;
            int setCursorCount = 0;

            var sut = new InPlaceBoardRenderer(
                inner.Object,
                (l, t) =>
                {
                    setLeft = l;
                    setTop = t;
                    setCursorCount++;
                },
                () => (2, 8),
                isOutputRedirected: () => false);

            var board = new Board(3, 3);

            // First render: records position (2, 8), does not call setCursorPosition yet
            sut.Render(board);
            setCursorCount.ShouldBe(0);

            // Second render: moves cursor back to (2, 8) and renders
            sut.Render(board);
            setCursorCount.ShouldBe(1);
            setLeft.ShouldBe(2);
            setTop.ShouldBe(8);

            // Third render: moves cursor back to (2, 8) again
            sut.Render(board);
            setCursorCount.ShouldBe(2);

            inner.Verify(m => m.Render(board), Times.Exactly(3));
        }

        [Test]
        public void ResetPositionTrackingWhenResetCalled()
        {
            var inner = new Mock<IBoardRenderer>();
            int setCursorCount = 0;
            int currentTop = 5;

            var sut = new InPlaceBoardRenderer(
                inner.Object,
                (l, t) => setCursorCount++,
                () => (0, currentTop),
                isOutputRedirected: () => false);

            var board = new Board(3, 3);

            sut.Render(board); // initial anchor at 5
            sut.Render(board); // overwrites at 5
            setCursorCount.ShouldBe(1);

            sut.Reset();
            currentTop = 15; // user printed text and new game started at row 15

            sut.Render(board); // re-initializes anchor at 15
            setCursorCount.ShouldBe(1);

            sut.Render(board); // overwrites at 15
            setCursorCount.ShouldBe(2);
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
    }
}
