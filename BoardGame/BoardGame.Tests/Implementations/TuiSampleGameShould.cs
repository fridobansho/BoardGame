namespace BoardGame.Tests.Implementations
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Implementations.ConnectFour;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using NUnit.Framework;
    using Shouldly;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    [TestFixture]
    public class TuiSampleGameShould
    {
        [Test]
        public void PlaySampleTicTacToeGameInPlaceWithoutScrolling()
        {
            var sw = new StringWriter();
            var innerRenderer = new ConsoleRenderer(sw);

            var inPlaceRenderer = new InPlaceBoardRenderer(
                innerRenderer,
                sw,
                isOutputRedirected: () => false);

            var board = new Board(3, 3);
            var players = new IPlayer[]
            {
                new MinimaxPlayer("Minimax-X"),
                new SimplePlayer("Simple-O")
            };
            var logic = new TicTacToeLogic();

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, inPlaceRenderer, turnDelayMs: 0);

            var winners = runner.RunGame().ToList();

            // 1. Minimax should win against SimplePlayer
            winners.Count.ShouldBe(1);
            winners[0].Name.ShouldBe("Minimax-X");

            // 2. Relative ANSI cursor movement must have occurred on every move after the initial frame
            var fullOutput = sw.ToString();

            // Cursor was hidden for flicker-free rendering
            fullOutput.ShouldContain("\x1b[?25l");

            // Line clears were issued before writing lines
            fullOutput.ShouldContain("\x1b[2K");

            // Cursor moved UP by board line count on every subsequent frame
            // For a 3x3 board, ConsoleRenderer outputs 6 lines (1 header + 3 rows + 2 divider rows), so code is \x1b[6F
            var cursorMoveMatches = Regex.Matches(fullOutput, @"\x1b\[6F");
            cursorMoveMatches.Count.ShouldBeGreaterThan(2);

            // 3. The final rendered board contains the players' tokens
            fullOutput.ShouldContain("X");
            fullOutput.ShouldContain("O");
        }

        [Test]
        public void PlaySampleChompGameInPlaceWithoutScrolling()
        {
            var sw = new StringWriter();
            var innerRenderer = new ConsoleRenderer(sw);

            var inPlaceRenderer = new InPlaceBoardRenderer(
                innerRenderer,
                sw,
                isOutputRedirected: () => false);

            var board = new Board(4, 3);
            var players = new IPlayer[]
            {
                new SmartChompPlayer("SmartChomp"),
                new ChompPlayer("BasicChomp")
            };
            var logic = new ChompLogic();

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, inPlaceRenderer, turnDelayMs: 0);

            var winners = runner.RunGame().ToList();

            // SmartChomp should win against basic ChompPlayer
            winners.Count.ShouldBe(1);
            winners[0].Name.ShouldBe("SmartChomp");

            // Relative cursor movement must have occurred for each move
            var fullOutput = sw.ToString();
            fullOutput.ShouldContain("\x1b[?25l");
            fullOutput.ShouldContain("\x1b[2K");

            // 4x3 board has height 3 -> 6 lines
            var cursorMoveMatches = Regex.Matches(fullOutput, @"\x1b\[6F");
            cursorMoveMatches.Count.ShouldBeGreaterThan(1);
        }

        [Test]
        public void PlaySampleConnectFourGameInPlaceWithoutScrolling()
        {
            var sw = new StringWriter();
            var innerRenderer = new ConsoleRenderer(sw);

            var inPlaceRenderer = new InPlaceBoardRenderer(
                innerRenderer,
                sw,
                isOutputRedirected: () => false);

            var board = new Board(ConnectFourLogic.DefaultWidth, ConnectFourLogic.DefaultHeight);
            var players = new IPlayer[]
            {
                new ConnectFourPlayer("RedAI"),
                new ConnectFourPlayer("YellowAI")
            };
            var logic = new ConnectFourLogic();

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, inPlaceRenderer, turnDelayMs: 0);

            var winners = runner.RunGame().ToList();

            // Game completes with either a winner or draw
            winners.ShouldNotBeEmpty();

            // Connect 4 board height is 6 lines -> 1 header + 6 rows + 5 dividers = 12 lines
            var fullOutput = sw.ToString();
            fullOutput.ShouldContain("\x1b[?25l");
            fullOutput.ShouldContain("\x1b[2K");

            var cursorMoveMatches = Regex.Matches(fullOutput, @"\x1b\[12F");
            cursorMoveMatches.Count.ShouldBeGreaterThan(3);
        }

        [Test]
        public void PlayMultipleGamesSequentiallyWithInPlaceRendererReset()
        {
            var sw = new StringWriter();
            var innerRenderer = new ConsoleRenderer(sw);

            using var inPlaceRenderer = new InPlaceBoardRenderer(
                innerRenderer,
                sw,
                isOutputRedirected: () => false);

            // Game 1: TicTacToe
            var tttGame = new Game(
                new Board(3, 3),
                new IPlayer[] { new MinimaxPlayer("X"), new SimplePlayer("O") },
                new TicTacToeLogic());
            new GameRunner(tttGame, inPlaceRenderer, turnDelayMs: 0).RunGame();

            int outputLengthAfterGame1 = sw.ToString().Length;
            outputLengthAfterGame1.ShouldBeGreaterThan(0);

            // Reset renderer for Game 2
            inPlaceRenderer.Reset();

            // Game 2: Chomp
            var chompGame = new Game(
                new Board(4, 3),
                new IPlayer[] { new SmartChompPlayer("Smart"), new ChompPlayer("Basic") },
                new ChompLogic());
            new GameRunner(chompGame, inPlaceRenderer, turnDelayMs: 0).RunGame();

            var game2Output = sw.ToString().Substring(outputLengthAfterGame1);
            // First render of game 2 should not move cursor up into Game 1's space
            game2Output.IndexOf("\x1b[6F", StringComparison.Ordinal).ShouldBeGreaterThan(0);
        }
    }
}
