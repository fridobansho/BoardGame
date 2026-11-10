namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Chomp;
    using Library.Implementations.Silly;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Tic-Tac-Toe ===");
            PlayGame(new Board(3, 3),
                     new IPlayer[] { new SimplePlayer("TicTacToe Player 1"), new SillyPlayer("TicTacToe Player 2") },
                     new TicTacToeLogic());

            Console.WriteLine("\n=== Chomp (2D Nim) ===");
            PlayGame(new Board(4, 3),
                     new IPlayer[] { new ChompPlayer("Chomp Player 1"), new ChompPlayer("Chomp Player 2") },
                     new ChompLogic());

            if (!Console.IsInputRedirected)
            {
                Console.ReadKey();
            }
        }

        private static void PlayGame(IBoard board, IPlayer[] players, IGameLogic logic)
        {
            var game = new Game(board, players, logic);
            var runner = new GameRunner(game);
            var winners = runner.RunGame();

            foreach (var winner in winners)
            {
                Console.WriteLine("Winner: " + winner.Name);
            }
        }
    }
}
