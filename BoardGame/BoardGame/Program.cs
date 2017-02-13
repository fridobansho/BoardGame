namespace BoardGame
{
    using Library.Implementations;
    using Library.Implementations.Silly;
    using Library.Implementations.TicTacToe;
    using Library.Interfaces;
    using System;

    class Program
    {
        static void Main(string[] args)
        {
            var player1 = new SimplePlayer("Player 1");
            var player2 = new SillyPlayer("Player 2");

            var players = new IPlayer[] { player1, player2 };

            var gameLogic = new TicTacToeLogic();

            var board = new Board(3, 3);

            var game = new Game(board, players, gameLogic);

            var gameRunner = new GameRunner(game);

            var winners = gameRunner.RunGame();

            foreach(var winner in winners)
            {
                Console.WriteLine("Winner: " + winner.Name);
            }
            Console.ReadKey();
        }
    }
}
