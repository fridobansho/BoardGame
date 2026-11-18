namespace BoardGame.Library.Implementations.Scrabble
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class RandomScrabblePlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private ScrabbleLogic _logic;

        public string Name { get; }

        public RandomScrabblePlayer(string name = "Scrabble Bot (Easy)")
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        internal void Bind(ScrabbleLogic logic)
        {
            _logic = logic;
        }

        public ILocation GetMove(IBoard board)
        {
            var logic = _logic ?? ScrabbleLogic.LastInitializedLogic;
            if (board == null || logic == null)
            {
                return WordMove.Pass;
            }

            var candidates = ScrabbleMoveGenerator.GenerateCandidateMoves(board, logic, this, maxCandidates: 80);
            if (candidates.Count == 0)
            {
                return WordMove.Pass;
            }

            return candidates[_random.Next(candidates.Count)];
        }
    }
}
