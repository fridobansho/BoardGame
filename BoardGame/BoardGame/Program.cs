namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Checkers;
    using Library.Implementations.Chomp;
    using Library.Implementations.ConnectFour;
    using Library.Implementations.Hex;
    using Library.Implementations.Mancala;
    using Library.Implementations.Othello;
    using Library.Implementations.Scrabble;
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
                         arg == "gomoku" || arg == "othello" || arg == "nim" || arg == "hex" ||
                         arg == "checkers" || arg == "draughts" || arg == "breakthrough" ||
                         arg == "mancala" || arg == "scrabble")
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
                    Console.WriteLine("\n=== Tic-Tac-Toe (Minimax AI vs Easy AI) ===");
                    PlayGame(new Board(3, 3),
                             new IPlayer[] { new MinimaxPlayer("Minimax (X)"), new RandomAiPlayer("Random (O)") },
                             new TicTacToeLogic(), renderer, turnDelayMs);
                    if (requestedGame == "all" && turnDelayMs > 0) Thread.Sleep(1500);
                }

                if (requestedGame == "all" || requestedGame == "gomoku")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Gomoku 9x9 (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(9, 9),
                             new IPlayer[] { new MinimaxPlayer("Smart (X)"), new RandomAiPlayer("Random (O)") },
                             new TicTacToeLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "connect4" || requestedGame == "c4" || requestedGame == "connectfour")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Connect Four (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(7, 6),
                             new IPlayer[] { new ConnectFourPlayer("Red (Smart)"), new RandomAiPlayer("Yellow (Random)") },
                             new ConnectFourLogic(), renderer, turnDelayMs);
                    if (requestedGame == "all" && turnDelayMs > 0) Thread.Sleep(1500);
                }

                if (requestedGame == "all" || requestedGame == "chomp")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Chomp (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(5, 4),
                             new IPlayer[] { new SmartChompPlayer("SmartBot"), new RandomAiPlayer("RandomBot") },
                             new ChompLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "nim")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Nim (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(7, 4),
                             new IPlayer[] { new SmartChompPlayer("SmartBot"), new RandomAiPlayer("RandomBot") },
                             new ChompLogic { IsRowOnlyNim = true, IsMisere = false }, renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "othello")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Othello (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(8, 8),
                             new IPlayer[] { new SmartOthelloPlayer("Black (Smart)", BlackPiece.Black.Value), new RandomAiPlayer("White (Random)") },
                             new OthelloLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "hex")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Hex (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(7, 7),
                             new IPlayer[] { new SmartHexPlayer("Black (Smart)"), new RandomAiPlayer("White (Random)") },
                             new HexLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "checkers" || requestedGame == "draughts")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Checkers (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(8, 8),
                             new IPlayer[] { new SmartCheckersPlayer("Red (Smart)"), new RandomAiPlayer("Black (Random)") },
                             new CheckersLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "breakthrough")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Breakthrough (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(8, 8),
                             new IPlayer[] { new SmartCheckersPlayer("Red (Smart)"), new RandomAiPlayer("Black (Random)") },
                             new CheckersLogic { IsBreakthrough = true }, renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "mancala")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Mancala (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(6, 2),
                             new IPlayer[] { new SmartMancalaPlayer("SmartBot"), new RandomAiPlayer("RandomBot") },
                             new MancalaLogic(), renderer, turnDelayMs);
                }

                if (requestedGame == "all" || requestedGame == "scrabble")
                {
                    if (useAlt) Console.Clear();
                    Console.WriteLine("\n=== Scrabble (Smart AI vs Easy AI) ===");
                    PlayGame(new Board(15, 15),
                             new IPlayer[] { new SmartScrabblePlayer("SmartBot"), new RandomAiPlayer("RandomBot") },
                             new ScrabbleLogic(), renderer, turnDelayMs);
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
            Console.WriteLine("  tictactoe (ttt, gomoku), connect4 (c4), chomp (nim), othello, hex, checkers (breakthrough), mancala, scrabble, all");
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

            try
            {
                while (true)
                {
                    Console.WriteLine("\n========================================");
                    Console.WriteLine("   BoardGame Multi-Game TUI");
                    Console.WriteLine("========================================");
                    Console.WriteLine("1. Tic-Tac-Toe & Gomoku");
                    Console.WriteLine("2. Connect Four");
                    Console.WriteLine("3. Chomp & Nim");
                    Console.WriteLine("4. Othello (Reversi)");
                    Console.WriteLine("5. Hex");
                    Console.WriteLine("6. Checkers & Breakthrough");
                    Console.WriteLine("7. Mancala");
                    Console.WriteLine("8. Scrabble");
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
                        "4" => "othello",
                        "5" => "hex",
                        "6" => "checkers",
                        "7" => "mancala",
                        "8" => "scrabble",
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

                    var (width, height, isNimVariant, isBreakthroughVariant) = PromptBoardConfig(gameKey);
                    var board = new Board(width, height);
                    IGameLogic logic = gameKey switch
                    {
                        "tictactoe" => new TicTacToeLogic(),
                        "connect4" => new ConnectFourLogic(),
                        "chomp" => new ChompLogic { IsRowOnlyNim = isNimVariant, IsMisere = !isNimVariant },
                        "othello" => new OthelloLogic(),
                        "hex" => new HexLogic(),
                        "checkers" => new CheckersLogic { IsBreakthrough = isBreakthroughVariant },
                        "mancala" => new MancalaLogic(),
                        "scrabble" => new ScrabbleLogic(),
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
                        inPlaceRenderer?.Reset();
                    }
                }
            }
            finally
            {
                inPlaceRenderer?.Dispose();
            }
        }

        private static (int width, int height, bool isNim, bool isBreakthrough) PromptBoardConfig(string gameKey)
        {
            switch (gameKey)
            {
                case "tictactoe":
                    var tttSize = PromptChoice("Board Size: 1) 3x3  2) 4x4  3) 5x5  4) 9x9 (Gomoku)  5) 15x15 (Gomoku)", new[] { (3, 3), (4, 4), (5, 5), (9, 9), (15, 15) });
                    return (tttSize.Item1, tttSize.Item2, false, false);
                case "connect4":
                    var c4Size = PromptChoice("Board Size: 1) 7x6  2) 6x5  3) 8x7", new[] { (7, 6), (6, 5), (8, 7) });
                    return (c4Size.Item1, c4Size.Item2, false, false);
                case "chomp":
                    var chompChoice = PromptChoice("Variant / Size: 1) Chomp 5x4  2) Chomp 4x3  3) Nim (1-3-5-7)", new[] { (5, 4, false), (4, 3, false), (7, 4, true) });
                    return (chompChoice.Item1, chompChoice.Item2, chompChoice.Item3, false);
                case "hex":
                    var hexSize = PromptChoice("Board Size: 1) 7x7  2) 5x5  3) 11x11", new[] { (7, 7), (5, 5), (11, 11) });
                    return (hexSize.Item1, hexSize.Item2, false, false);
                case "checkers":
                    var chkChoice = PromptChoice("Ruleset: 1) Standard Draughts (8x8)  2) Breakthrough (8x8)", new[] { (8, 8, false), (8, 8, true) });
                    return (chkChoice.Item1, chkChoice.Item2, false, chkChoice.Item3);
                case "othello":
                    return (8, 8, false, false);
                case "mancala":
                    return (6, 2, false, false);
                case "scrabble":
                    return (15, 15, false, false);
                default:
                    return (3, 3, false, false);
            }
        }

        private static T PromptChoice<T>(string prompt, T[] options)
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
            if (!smart)
            {
                return new RandomAiPlayer(name);
            }

            return gameKey switch
            {
                "tictactoe" => new MinimaxPlayer(name),
                "connect4" => new ConnectFourPlayer(name),
                "chomp" => new SmartChompPlayer(name),
                "othello" => new SmartOthelloPlayer(name, isFirst ? BlackPiece.Black.Value : WhitePiece.White.Value),
                "hex" => new SmartHexPlayer(name),
                "checkers" => new SmartCheckersPlayer(name, isFirst ? RedCheckerPiece.Red.Value : BlackCheckerPiece.Black.Value),
                "mancala" => new SmartMancalaPlayer(name),
                "scrabble" => new SmartScrabblePlayer(name),
                _ => new RandomAiPlayer(name)
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
