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

                _writer.Write("Invalid format. Please enter coordinates as 'x, y' (e.g. '0, 1'): ");
            }
        }
    }
}
