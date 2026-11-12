namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System;
    using System.IO;
    using System.Text;

    public class ConsoleRenderer : IBoardRenderer
    {
        private readonly TextWriter _writer;

        public ConsoleRenderer() : this(Console.Out)
        {
        }

        public ConsoleRenderer(TextWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        }

        public void Render(IBoard board)
        {
            _writer.WriteLine(RenderToString(board));
            _writer.WriteLine();
        }

        public string RenderToString(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            var sb = new StringBuilder();

            // Column numbers header
            sb.Append("   ");
            for (int x = 0; x < board.Width; x++)
            {
                sb.Append($" {x} ");
                if (x < board.Width - 1) sb.Append(" ");
            }
            sb.AppendLine();

            for (int y = 0; y < board.Height; y++)
            {
                sb.Append($"{y,2} ");
                for (int x = 0; x < board.Width; x++)
                {
                    var piece = board.PieceAt(x, y);
                    string val = piece?.Value;
                    if (string.IsNullOrEmpty(val)) val = " ";
                    sb.Append($" {val} ");
                    if (x < board.Width - 1) sb.Append("|");
                }
                sb.AppendLine();

                if (y < board.Height - 1)
                {
                    sb.Append("   ");
                    for (int x = 0; x < board.Width; x++)
                    {
                        sb.Append("---");
                        if (x < board.Width - 1) sb.Append("+");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString().TrimEnd('\r', '\n');
        }
    }
}
