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
            var renderer = new ConsoleRenderer();

            Console.WriteLine("========================================");
            Console.WriteLine("   BoardGame Multi-Game Engine (Linux)   ");
            Console.WriteLine("========================================");

            Console.WriteLine("\n=== 1. Tic-Tac-Toe (Minimax AI vs Simple AI) ===");
            PlayGame(new Board(3, 3),
                     new IPlayer[] { new MinimaxPlayer("Minimax (X)"), new SimplePlayer("Simple (O)") },
                     new TicTacToeLogic(),
                     renderer);

            Console.WriteLine("\n=== 2. Chomp 2D Nim (Smart AI vs Basic Player) ===");
            PlayGame(new Board(4, 3),
                     new IPlayer[] { new SmartChompPlayer("SmartChomp (P1)"), new ChompPlayer("BasicChomp (P2)") },
                     new ChompLogic(),
                     renderer);

            Console.WriteLine("\n=== 3. Connect Four (Red AI vs Yellow AI) ===");
            PlayGame(new Board(ConnectFourLogic.DefaultWidth, ConnectFourLogic.DefaultHeight),
                     new IPlayer[] { new ConnectFourPlayer("Red Bot"), new ConnectFourPlayer("Yellow Bot") },
                     new ConnectFourLogic(),
                     renderer);

            if (!Console.IsInputRedirected)
            {
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }

        private static void PlayGame(IBoard board, IPlayer[] players, IGameLogic logic, IBoardRenderer renderer)
        {
            var game = new Game(board, players, logic);
            var runner = new GameRunner(game, renderer);
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
