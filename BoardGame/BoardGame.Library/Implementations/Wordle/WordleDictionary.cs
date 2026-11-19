namespace BoardGame.Library.Implementations.Wordle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Provides curated five-letter answer words and the broader allowed guess list for Wordle.
    /// </summary>
    public static class WordleDictionary
    {
        public const int WordLength = 5;

        private static readonly List<string> _answerWords;
        private static readonly HashSet<string> _answerSet;
        private static readonly HashSet<string> _allowedGuesses;

        static WordleDictionary()
        {
            var answers = LoadWordResource(".wordle-answers.txt");
            if (answers.Count == 0)
            {
                answers.Add("CRANE");
            }

            _answerWords = answers
                .Distinct(StringComparer.Ordinal)
                .OrderBy(w => w, StringComparer.Ordinal)
                .ToList();
            _answerSet = new HashSet<string>(_answerWords, StringComparer.Ordinal);

            var guesses = LoadWordResource(".wordle-guesses.txt");
            _allowedGuesses = new HashSet<string>(guesses, StringComparer.Ordinal);
            foreach (var answer in _answerWords)
            {
                _allowedGuesses.Add(answer);
            }
        }

        public static IReadOnlyList<string> AnswerWords => _answerWords;

        public static IReadOnlyCollection<string> AllowedGuesses => _allowedGuesses;

        public static bool IsValidGuess(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            string normalized = word.Trim().ToUpperInvariant();
            if (normalized.Length != WordLength)
            {
                return false;
            }

            return _allowedGuesses.Contains(normalized);
        }

        public static bool IsAnswerWord(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            string normalized = word.Trim().ToUpperInvariant();
            if (normalized.Length != WordLength)
            {
                return false;
            }

            return _answerSet.Contains(normalized);
        }

        private static List<string> LoadWordResource(string resourceSuffix)
        {
            var assembly = typeof(WordleDictionary).Assembly;
            string resourceName = assembly
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));

            var words = new List<string>();
            if (resourceName == null)
            {
                return words;
            }

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return words;
            }

            using var reader = new StreamReader(stream);
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string candidate = line.Trim().ToUpperInvariant();
                if (IsAsciiFiveLetterWord(candidate))
                {
                    words.Add(candidate);
                }
            }

            return words;
        }

        private static bool IsAsciiFiveLetterWord(string word)
        {
            if (word.Length != WordLength)
            {
                return false;
            }

            for (int i = 0; i < word.Length; i++)
            {
                char c = word[i];
                if (c < 'A' || c > 'Z')
                {
                    return false;
                }
            }

            return true;
        }
    }
}
