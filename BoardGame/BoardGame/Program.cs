namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Implementations.ConnectFour;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using System;
    using System.Linq;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            bool forceInPlace = args.Contains("--in-place");
            bool forceStream = args.Contains("--stream");
            bool useInPlace = forceInPlace || (!forceStream && !Console.IsOutputRedirected);

            bool forceAlt = args.Contains("--alt");
            bool forceNoAlt = args.Contains("--no-alt");
            bool useAlt = useInPlace && (forceAlt || (!forceNoAlt && !Console.IsOutputRedirected && !Console.IsInputRedirected));

            int turnDelayMs = useInPlace ? 300 : 0;
            string requestedGame = "all";

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i].ToLowerInvariant();
                if ((arg == "--game" || arg == "-g") && i + 1 < args.Length)
                {
                    requestedGame = args[++i].ToLowerInvariant();
                }
                else if ((arg == "--delay" || arg == "-d") && i + 1 < args.Length && int.TryParse(args[i + 1], out int delay))
                {
                    turnDelayMs = delay;
                    i++;
                }
                else if (arg == "connect4" || arg == "c4" || arg == "connectfour" ||
                         arg == "tictactoe" || arg == "ttt" ||
                         arg == "chomp")
                {
                    requestedGame = arg;
                }
            }

            InPlaceBoardRenderer inPlaceRenderer = null;
            IBoardRenderer renderer;
            if (useInPlace)
            {
                inPlaceRenderer = new InPlaceBoardRenderer(new ConsoleRenderer(), useAlternateScreen: useAlt);
                if (useAlt)
                {
                    inPlaceRenderer.EnterAlternateScreen();
                }
                renderer = inPlaceRenderer;
            }
            else
            {
                renderer = new ConsoleRenderer();
            }

            Console.CancelKeyPress += (s, e) =>
            {
                inPlaceRenderer?.Dispose();
            };

            try
            {
                Console.WriteLine("========================================");
                Console.WriteLine("   BoardGame Multi-Game Engine (Linux)   ");
                Console.WriteLine($"   Render Mode: {(useInPlace ? (useAlt ? "In-Place (TUI / Alternate Screen)" : "In-Place (TUI / No Scrolling)") : "Streaming (Log Output)")}");
                if (turnDelayMs > 0)
                {
                    Console.WriteLine($"   Turn Pacing: {turnDelayMs}ms per move");
                }
                Console.WriteLine("========================================");

                if (requestedGame == "all" || requestedGame == "tictactoe" || requestedGame == "ttt")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Tic-Tac-Toe (Minimax AI vs Simple AI) ===");
                    PlayGame(new Board(3, 3),
                             new IPlayer[] { new MinimaxPlayer("Minimax (X)"), new SimplePlayer("Simple (O)") },
                             new TicTacToeLogic(),
                             renderer,
                             turnDelayMs);

                    if (requestedGame == "all" && turnDelayMs > 0)
                    {
                        Thread.Sleep(1200);
                    }
                }

                if (requestedGame == "all" || requestedGame == "chomp")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Chomp 2D Nim (Smart AI vs Basic Player) ===");
                    PlayGame(new Board(4, 3),
                             new IPlayer[] { new SmartChompPlayer("SmartChomp (P1)"), new ChompPlayer("BasicChomp (P2)") },
                             new ChompLogic(),
                             renderer,
                             turnDelayMs);

                    if (requestedGame == "all" && turnDelayMs > 0)
                    {
                        Thread.Sleep(1200);
                    }
                }

                if (requestedGame == "all" || requestedGame == "connect4" || requestedGame == "c4" || requestedGame == "connectfour")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Connect Four (Red AI vs Yellow AI) ===");
                    PlayGame(new Board(ConnectFourLogic.DefaultWidth, ConnectFourLogic.DefaultHeight),
                             new IPlayer[] { new ConnectFourPlayer("Red Bot"), new ConnectFourPlayer("Yellow Bot") },
                             new ConnectFourLogic(),
                             renderer,
                             turnDelayMs);
                }

                if (!Console.IsInputRedirected)
                {
                    Console.WriteLine("\nPress any key to exit...");
                    Console.ReadKey(true);
                }
            }
            finally
            {
                inPlaceRenderer?.Dispose();
            }
        }

        private static void PlayGame(IBoard board, IPlayer[] players, IGameLogic logic, IBoardRenderer renderer, int turnDelayMs)
        {
            if (renderer is InPlaceBoardRenderer inPlace)
            {
                inPlace.Reset();
            }

            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, renderer, turnDelayMs);
            var winners = runner.RunGame().ToList();

            if (winners.Count == 0)
            {
                Console.WriteLine("Game ended with no winner.\n");
            }
            else if (winners.Count == players.Length)
            {
                Console.WriteLine("Game ended in a DRAW between: " + string.Join(" and ", winners.Select(w => w.Name)) + "\n");
            }
            else
            {
                foreach (var winner in winners)
                {
                    Console.WriteLine("Winner: " + winner.Name);
                }
                Console.WriteLine();
            }
        }
    }
}
