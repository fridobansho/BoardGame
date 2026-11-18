namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System;
    using System.IO;

    public class ConsolePlayer : IPlayer
    {
        private readonly TextReader _reader;
        private readonly TextWriter _writer;

        public string Name { get; }

        public ConsolePlayer() : this("Human", Console.In, Console.Out)
        {
        }

        public ConsolePlayer(string name) : this(name, Console.In, Console.Out)
        {
        }

        public ConsolePlayer(string name, TextReader reader, TextWriter writer)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public ILocation GetMove(IBoard board)
        {
            if (board == null) return new Location(-1, -1);

            _writer.Write($"{Name}, enter your move as 'x, y' (columns 0..{board.Width - 1}, rows 0..{board.Height - 1}): ");

            while (true)
            {
                var input = _reader.ReadLine();
                if (input == null)
                {
                    return new Location(-1, -1);
                }

                input = input.Trim();
                if (string.Equals(input, "PASS", StringComparison.OrdinalIgnoreCase))
                {
                    return WordMove.Pass;
                }

                var parts = input.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out int x) &&
                    int.TryParse(parts[1], out int y))
                {
                    if (board.CheckBounds(x, y))
                    {
                        return new Location(x, y);
                    }

                    _writer.Write($"Coordinates ({x}, {y}) are out of bounds. Please enter coordinates between (0, 0) and ({board.Width - 1}, {board.Height - 1}): ");
                    continue;
                }

                if (parts.Length == 3 &&
                    int.TryParse(parts[0], out int sx) &&
                    int.TryParse(parts[1], out int sy) &&
                    int.TryParse(parts[2], out int digit))
                {
                    if (board.CheckBounds(sx, sy) && digit >= 1 && digit <= 9)
                    {
                        return new SudokuMove(sx, sy, digit);
                    }

                    _writer.Write($"Coordinates or digit out of bounds (x, y in range, digit 1..9). Try again: ");
                    continue;
                }

                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out int fromX) &&
                    int.TryParse(parts[1], out int fromY) &&
                    int.TryParse(parts[2], out int toX) &&
                    int.TryParse(parts[3], out int toY))
                {
                    if (board.CheckBounds(fromX, fromY) && board.CheckBounds(toX, toY))
                    {
                        return new PieceMove(fromX, fromY, toX, toY);
                    }

                    _writer.Write($"Coordinates are out of bounds. Please enter coordinates between (0, 0) and ({board.Width - 1}, {board.Height - 1}): ");
                    continue;
                }

                if (parts.Length == 4 &&
                    int.TryParse(parts[0], out int wordX) &&
                    int.TryParse(parts[1], out int wordY) &&
                    (string.Equals(parts[2], "H", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(parts[2], "V", StringComparison.OrdinalIgnoreCase)))
                {
                    bool horizontal = string.Equals(parts[2], "H", StringComparison.OrdinalIgnoreCase);
                    string word = parts[3].Trim().ToUpperInvariant();
                    if (word.Length > 0 && IsAsciiLetters(word))
                    {
                        int endX = horizontal ? wordX + word.Length - 1 : wordX;
                        int endY = horizontal ? wordY : wordY + word.Length - 1;
                        if (board.CheckBounds(wordX, wordY) && board.CheckBounds(endX, endY))
                        {
                            return WordMove.FromWord(board, wordX, wordY, horizontal, word);
                        }

                        _writer.Write($"Word placement is out of bounds for board ({board.Width}x{board.Height}). Try again: ");
                        continue;
                    }
                }

                _writer.Write("Invalid format. Please enter coordinates as 'x, y' (e.g. '0, 1'): ");
            }
        }

        private static bool IsAsciiLetters(string word)
        {
            foreach (char c in word)
            {
                if (c < 'A' || c > 'Z') return false;
            }
            return true;
        }
    }
}
