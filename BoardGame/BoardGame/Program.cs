namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Implementations.ConnectFour;
    using Library.Implementations.Gomoku;
    using Library.Implementations.Hex;
    using Library.Implementations.Nim;
    using Library.Implementations.Othello;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using System;
    using System.Linq;
    using System.Threading;

    class Program
    {
        static void Main(string[] args)
        {
            if (args.Contains("--help") || args.Contains("-h"))
            {
                PrintHelp();
                return;
            }

            bool forceInPlace = args.Contains("--in-place");
            bool forceStream = args.Contains("--stream");
            bool useInPlace = forceInPlace || (!forceStream && !Console.IsOutputRedirected);

            bool forceAlt = args.Contains("--alt");
            bool forceNoAlt = args.Contains("--no-alt");
            bool useAlt = useInPlace && (forceAlt || (!forceNoAlt && !Console.IsOutputRedirected && !Console.IsInputRedirected));

            int turnDelayMs = useInPlace ? 300 : 0;
            string requestedGame = null;
            bool forceInteractive = args.Contains("--interactive") || args.Contains("-i");

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
                else if (arg == "all" || arg == "connect4" || arg == "c4" || arg == "connectfour" ||
                         arg == "tictactoe" || arg == "ttt" || arg == "chomp" ||
                         arg == "gomoku" || arg == "othello" || arg == "nim" || arg == "hex")
                {
                    requestedGame = arg;
                }
            }

            if (forceInteractive || (requestedGame == null && !Console.IsInputRedirected))
            {
                RunInteractiveMenu(useInPlace, useAlt);
                return;
            }

            requestedGame ??= "all";

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

            Console.CancelKeyPress += (s, e) => inPlaceRenderer?.Dispose();

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
                             new TicTacToeLogic(), renderer, turnDelayMs);
                    if (requestedGame == "all" && turnDelayMs > 0) Thread.Sleep(1500);
                }

                if (requestedGame == "all" || requestedGame == "connect4" || requestedGame == "c4" || requestedGame == "connectfour")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Connect Four (Smart AI vs Random AI) ===");
                    PlayGame(new Board(7, 6),
                             new IPlayer[] { new ConnectFourPlayer("Red (Smart)"), new SimplePlayer("Yellow (Random)") },
                             new ConnectFourLogic(), renderer, turnDelayMs);
                    if (requestedGame == "all" && turnDelayMs > 0) Thread.Sleep(1500);
                }

                if (requestedGame == "all" || requestedGame == "chomp")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Chomp (Smart AI vs Random AI) ===");
                    PlayGame(new Board(5, 4),
                             new IPlayer[] { new SmartChompPlayer("SmartBot"), new ChompPlayer("RandomBot") },
                             new ChompLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "gomoku")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Gomoku (Smart AI vs Random AI) ===");
                    PlayGame(new Board(9, 9),
                             new IPlayer[] { new SmartGomokuPlayer("Black (Smart)"), new RandomGomokuPlayer("White (Random)") },
                             new GomokuLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "othello")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Othello (Smart AI vs Random AI) ===");
                    PlayGame(new Board(8, 8),
                             new IPlayer[] { new SmartOthelloPlayer("Black (Smart)", BlackPiece.Black.Value), new RandomOthelloPlayer("White (Random)", WhitePiece.White.Value) },
                             new OthelloLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "nim")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Nim (Smart AI vs Random AI) ===");
                    PlayGame(new Board(7, 4),
                             new IPlayer[] { new SmartNimPlayer("SmartBot"), new RandomNimPlayer("RandomBot") },
                             new NimLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "hex")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Hex (Smart AI vs Random AI) ===");
                    PlayGame(new Board(7, 7),
                             new IPlayer[] { new SmartHexPlayer("Black (Smart)"), new RandomHexPlayer("White (Random)") },
                             new HexLogic(), renderer, turnDelayMs);
                }
            }
            finally
            {
                inPlaceRenderer?.Dispose();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("BoardGame Multi-Game TUI");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet run --project BoardGame/BoardGame/BoardGame.csproj -- [options] [game]");
            Console.WriteLine();
            Console.WriteLine("Games:");
            Console.WriteLine("  tictactoe (ttt), connect4 (c4), chomp, gomoku, othello, nim, hex, all");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -i, --interactive   Launch the interactive game & settings menu");
            Console.WriteLine("  -g, --game <name>   Run automated AI vs AI demo for the specified game");
            Console.WriteLine("  -d, --delay <ms>    Turn pacing delay in milliseconds (default: 300 in-place, 0 stream)");
            Console.WriteLine("  --in-place          Render board in-place without scrolling");
            Console.WriteLine("  --stream            Stream board output line by line");
            Console.WriteLine("  --alt / --no-alt    Enable or disable alternate screen buffer");
            Console.WriteLine("  -h, --help          Show this help message");
        }

        private static void RunInteractiveMenu(bool useInPlace, bool useAlt)
        {
            InPlaceBoardRenderer inPlaceRenderer = useInPlace
                ? new InPlaceBoardRenderer(new ConsoleRenderer(), useAlternateScreen: useAlt)
                : null;
            IBoardRenderer renderer = inPlaceRenderer ?? (IBoardRenderer)new ConsoleRenderer();

            while (true)
            {
                Console.WriteLine("\n========================================");
                Console.WriteLine("   BoardGame Multi-Game TUI");
                Console.WriteLine("========================================");
                Console.WriteLine("1. Tic-Tac-Toe");
                Console.WriteLine("2. Connect Four");
                Console.WriteLine("3. Chomp");
                Console.WriteLine("4. Gomoku (5-in-a-Row)");
                Console.WriteLine("5. Othello (Reversi)");
                Console.WriteLine("6. Nim");
                Console.WriteLine("7. Hex");
                Console.WriteLine("0. Exit");
                Console.Write("Select a game: ");

                var gameChoice = Console.ReadLine()?.Trim();
                if (gameChoice == "0" || string.Equals(gameChoice, "q", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                string gameKey = gameChoice switch
                {
                    "1" => "tictactoe",
                    "2" => "connect4",
                    "3" => "chomp",
                    "4" => "gomoku",
                    "5" => "othello",
                    "6" => "nim",
                    "7" => "hex",
                    _ => null
                };

                if (gameKey == null)
                {
                    Console.WriteLine("Invalid selection.");
                    continue;
                }

                Console.WriteLine("\nSelect Mode:");
                Console.WriteLine("1. Human vs AI");
                Console.WriteLine("2. Human vs Human");
                Console.WriteLine("3. AI vs AI (Demo)");
                Console.Write("Choice [1]: ");
                var modeChoice = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(modeChoice)) modeChoice = "1";

                bool smartAi = true;
                if (modeChoice == "1" || modeChoice == "3")
                {
                    Console.WriteLine("\nAI Difficulty:");
                    Console.WriteLine("1. Smart (Recommended)");
                    Console.WriteLine("2. Easy");
                    Console.Write("Choice [1]: ");
                    var diffChoice = Console.ReadLine()?.Trim();
                    smartAi = diffChoice != "2";
                }

                var (width, height) = PromptBoardSize(gameKey);
                var board = new Board(width, height);
                IGameLogic logic = gameKey switch
                {
                    "tictactoe" => new TicTacToeLogic(),
                    "connect4" => new ConnectFourLogic(),
                    "chomp" => new ChompLogic(),
                    "gomoku" => new GomokuLogic(),
                    "othello" => new OthelloLogic(),
                    "nim" => new NimLogic(),
                    "hex" => new HexLogic(),
                    _ => new TicTacToeLogic()
                };

                IPlayer p1 = modeChoice == "3"
                    ? CreateAi(gameKey, smartAi, "AI 1", isFirst: true)
                    : new ConsolePlayer("Player 1");

                IPlayer p2 = modeChoice == "2"
                    ? new ConsolePlayer("Player 2")
                    : CreateAi(gameKey, smartAi, "AI", isFirst: false);

                int delay = modeChoice == "3" ? 300 : 0;
                Console.WriteLine($"\nStarting {gameKey} ({width}x{height})...");
                if (useAlt) inPlaceRenderer?.EnterAlternateScreen();
                try
                {
                    PlayGame(board, new[] { p1, p2 }, logic, renderer, delay);
                }
                finally
                {
                    inPlaceRenderer?.Dispose();
                }
            }
        }

        private static (int width, int height) PromptBoardSize(string gameKey)
        {
            return gameKey switch
            {
                "tictactoe" => PromptChoice("Board Size: 1) 3x3  2) 4x4  3) 5x5", new[] { (3, 3), (4, 4), (5, 5) }),
                "connect4" => PromptChoice("Board Size: 1) 7x6  2) 6x5  3) 8x7", new[] { (7, 6), (6, 5), (8, 7) }),
                "chomp" => PromptChoice("Board Size: 1) 5x4  2) 4x3  3) 6x5", new[] { (5, 4), (4, 3), (6, 5) }),
                "gomoku" => PromptChoice("Board Size: 1) 15x15  2) 9x9  3) 19x19", new[] { (15, 15), (9, 9), (19, 19) }),
                "hex" => PromptChoice("Board Size: 1) 7x7  2) 5x5  3) 11x11", new[] { (7, 7), (5, 5), (11, 11) }),
                "othello" => (8, 8),
                "nim" => (7, 4),
                _ => (3, 3)
            };
        }

        private static (int, int) PromptChoice(string prompt, (int, int)[] options)
        {
            Console.WriteLine($"\n{prompt}");
            Console.Write("Choice [1]: ");
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out int idx) && idx >= 1 && idx <= options.Length)
            {
                return options[idx - 1];
            }
            return options[0];
        }

        private static IPlayer CreateAi(string gameKey, bool smart, string name, bool isFirst)
        {
            return gameKey switch
            {
                "tictactoe" => smart ? new MinimaxPlayer(name) : new SimplePlayer(name),
                "connect4" => smart ? new ConnectFourPlayer(name) : new SimplePlayer(name),
                "chomp" => smart ? new SmartChompPlayer(name) : new ChompPlayer(name),
                "gomoku" => smart ? new SmartGomokuPlayer(name) : new RandomGomokuPlayer(name),
                "othello" => smart
                    ? new SmartOthelloPlayer(name, isFirst ? BlackPiece.Black.Value : WhitePiece.White.Value)
                    : new RandomOthelloPlayer(name, isFirst ? BlackPiece.Black.Value : WhitePiece.White.Value),
                "nim" => smart ? new SmartNimPlayer(name) : new RandomNimPlayer(name),
                "hex" => smart ? new SmartHexPlayer(name) : new RandomHexPlayer(name),
                _ => new SimplePlayer(name)
            };
        }

        private static void PlayGame(IBoard board, IPlayer[] players, IGameLogic logic, IBoardRenderer renderer, int turnDelayMs)
        {
            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, renderer, turnDelayMs);
            var winners = runner.RunGame().ToList();
            if (winners.Count == 1)
            {
                Console.WriteLine($"\nWinner: {winners[0].Name}!");
            }
            else if (winners.Count > 1)
            {
                Console.WriteLine("\nResult: Draw!");
            }
        }
    }
}
