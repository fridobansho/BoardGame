namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Implementations.ConnectFour;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using System;
    using System.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            bool useInPlace = args.Contains("--in-place") || (!args.Contains("--stream") && !Console.IsOutputRedirected);
            IBoardRenderer renderer = useInPlace
                ? new InPlaceBoardRenderer(new ConsoleRenderer())
                : new ConsoleRenderer();
            int turnDelayMs = useInPlace ? 250 : 0;

            Console.WriteLine("========================================");
            Console.WriteLine("   BoardGame Multi-Game Engine (Linux)   ");
            Console.WriteLine($"   Render Mode: {(useInPlace ? "In-Place (TUI / No Scrolling)" : "Streaming (Log Output)")}");
            Console.WriteLine("========================================");

            Console.WriteLine("\n=== 1. Tic-Tac-Toe (Minimax AI vs Simple AI) ===");
            PlayGame(new Board(3, 3),
                     new IPlayer[] { new MinimaxPlayer("Minimax (X)"), new SimplePlayer("Simple (O)") },
                     new TicTacToeLogic(),
                     renderer,
                     turnDelayMs);

            Console.WriteLine("\n=== 2. Chomp 2D Nim (Smart AI vs Basic Player) ===");
            PlayGame(new Board(4, 3),
                     new IPlayer[] { new SmartChompPlayer("SmartChomp (P1)"), new ChompPlayer("BasicChomp (P2)") },
                     new ChompLogic(),
                     renderer,
                     turnDelayMs);

            Console.WriteLine("\n=== 3. Connect Four (Red AI vs Yellow AI) ===");
            PlayGame(new Board(ConnectFourLogic.DefaultWidth, ConnectFourLogic.DefaultHeight),
                     new IPlayer[] { new ConnectFourPlayer("Red Bot"), new ConnectFourPlayer("Yellow Bot") },
                     new ConnectFourLogic(),
                     renderer,
                     turnDelayMs);

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
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
