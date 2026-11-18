namespace BoardGame.Library.Implementations.Scrabble
{
    using System;
    using System.Collections.Generic;
    using Interfaces;

    public class SmartScrabblePlayer : IPlayer
    {
        private readonly Random _random = new Random();
        private ScrabbleLogic _logic;

        public string Name { get; }

        public SmartScrabblePlayer(string name = "Scrabble Bot (Smart)")
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

            var candidates = ScrabbleMoveGenerator.GenerateCandidateMoves(board, logic, this, maxCandidates: 250);
            if (candidates.Count == 0)
            {
                return WordMove.Pass;
            }

            WordMove bestMove = candidates[0];
            int bestScore = int.MinValue;

            foreach (var move in candidates)
            {
                int score = logic.CalculateMoveScore(board, move);
                if (score > bestScore || (score == bestScore && _random.Next(2) == 0))
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return bestMove;
        }
    }

    internal static class ScrabbleMoveGenerator
    {
        public static List<WordMove> GenerateCandidateMoves(
            IBoard board,
            ScrabbleLogic logic,
            IPlayer player,
            int maxCandidates)
        {
            var results = new List<WordMove>();
            var rack = logic.GetRack(player);
            if (rack == null || rack.Count == 0) return results;

            var rackCounts = new int[26];
            foreach (char c in rack)
            {
                if (c >= 'A' && c <= 'Z') rackCounts[c - 'A']++;
            }

            if (ScrabbleLogic.IsBoardEmpty(board))
            {
                int centerX = board.Width / 2;
                int centerY = board.Height / 2;

                foreach (var word in ScrabbleDictionary.Words)
                {
                    if (word.Length < 2 || word.Length > rack.Count) continue;
                    if (!CanFormFromRack(word, rackCounts)) continue;

                    for (int offset = 0; offset < word.Length; offset++)
                    {
                        int startX = centerX - offset;
                        if (startX < 0 || startX + word.Length > board.Width) continue;

                        var move = WordMove.FromWord(board, startX, centerY, horizontal: true, word);
                        if (logic.IsValidMove(board, player, move))
                        {
                            results.Add(move);
                            if (results.Count >= maxCandidates) return results;
                        }
                    }
                }

                return results;
            }

            // Non-empty board: try extending or crossing through existing board tiles
            var anchors = new List<(int x, int y, char letter)>();
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue && piece.Value.Length > 0)
                    {
                        char ch = char.ToUpperInvariant(piece.Value[0]);
                        if (ch >= 'A' && ch <= 'Z')
                        {
                            anchors.Add((x, y, ch));
                        }
                    }
                }
            }

            foreach (var (ax, ay, anchorChar) in anchors)
            {
                var matchingWords = ScrabbleDictionary.GetWordsContaining(anchorChar);
                foreach (var word in matchingWords)
                {
                    if (word.Length > board.Width && word.Length > board.Height) continue;

                    for (int idx = 0; idx < word.Length; idx++)
                    {
                        if (word[idx] != anchorChar) continue;

                        if (TryBuildWordPlacement(board, ax - idx, ay, true, word, rackCounts, out var hMove) &&
                            logic.IsValidMove(board, player, hMove))
                        {
                            results.Add(hMove);
                            if (results.Count >= maxCandidates) return results;
                        }

                        if (TryBuildWordPlacement(board, ax, ay - idx, false, word, rackCounts, out var vMove) &&
                            logic.IsValidMove(board, player, vMove))
                        {
                            results.Add(vMove);
                            if (results.Count >= maxCandidates) return results;
                        }
                    }
                }
            }

            return results;
        }

        private static bool CanFormFromRack(string word, int[] rackCounts)
        {
            Span<int> needed = stackalloc int[26];
            for (int i = 0; i < word.Length; i++)
            {
                int idx = word[i] - 'A';
                if (idx < 0 || idx >= 26) return false;
                if (++needed[idx] > rackCounts[idx]) return false;
            }
            return true;
        }

        private static bool TryBuildWordPlacement(
            IBoard board,
            int startX,
            int startY,
            bool horizontal,
            string word,
            int[] rackCounts,
            out WordMove move)
        {
            move = null;
            int endX = horizontal ? startX + word.Length - 1 : startX;
            int endY = horizontal ? startY : startY + word.Length - 1;
            if (!board.CheckBounds(startX, startY) || !board.CheckBounds(endX, endY)) return false;

            // Ensure squares immediately before and after the word are blank or out of bounds
            int beforeX = horizontal ? startX - 1 : startX;
            int beforeY = horizontal ? startY : startY - 1;
            if (board.CheckBounds(beforeX, beforeY) && board.PieceAt(beforeX, beforeY)?.Value != BlankPiece.BlankValue)
            {
                return false;
            }
            int afterX = horizontal ? endX + 1 : endX;
            int afterY = horizontal ? endY : endY + 1;
            if (board.CheckBounds(afterX, afterY) && board.PieceAt(afterX, afterY)?.Value != BlankPiece.BlankValue)
            {
                return false;
            }

            Span<int> usedFromRack = stackalloc int[26];
            List<TilePlacement> placements = null;

            for (int i = 0; i < word.Length; i++)
            {
                int x = horizontal ? startX + i : startX;
                int y = horizontal ? startY : startY + i;
                char ch = word[i];

                var existing = board.PieceAt(x, y);
                if (existing != null && existing.Value != BlankPiece.BlankValue)
                {
                    if (existing.Value.Length != 1 || char.ToUpperInvariant(existing.Value[0]) != ch)
                    {
                        return false;
                    }
                }
                else
                {
                    int idx = ch - 'A';
                    if (idx < 0 || idx >= 26 || ++usedFromRack[idx] > rackCounts[idx])
                    {
                        return false;
                    }
                    placements ??= new List<TilePlacement>();
                    placements.Add(new TilePlacement(x, y, ch));
                }
            }

            if (placements == null || placements.Count == 0) return false;
            move = new WordMove(placements);
            return true;
        }
    }
}
