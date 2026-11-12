namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System;
    using System.IO;

    public class InPlaceBoardRenderer : IBoardRenderer, IDisposable
    {
        private readonly IBoardRenderer _innerRenderer;
        private readonly TextWriter _writer;
        private readonly Func<bool> _isOutputRedirected;
        private readonly bool _useAlternateScreen;
        private int _previousLineCount;
        private bool _cursorHidden;
        private bool _inAlternateScreen;

        public InPlaceBoardRenderer()
            : this(new ConsoleRenderer(), Console.Out, () => Console.IsOutputRedirected, useAlternateScreen: false)
        {
        }

        public InPlaceBoardRenderer(IBoardRenderer innerRenderer)
            : this(innerRenderer, Console.Out, () => Console.IsOutputRedirected, useAlternateScreen: false)
        {
        }

        public InPlaceBoardRenderer(
            IBoardRenderer innerRenderer,
            bool useAlternateScreen)
            : this(innerRenderer, Console.Out, () => Console.IsOutputRedirected, useAlternateScreen)
        {
        }

        public InPlaceBoardRenderer(
            IBoardRenderer innerRenderer,
            TextWriter writer,
            Func<bool> isOutputRedirected,
            bool useAlternateScreen = false)
        {
            _innerRenderer = innerRenderer ?? throw new ArgumentNullException(nameof(innerRenderer));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _isOutputRedirected = isOutputRedirected ?? throw new ArgumentNullException(nameof(isOutputRedirected));
            _useAlternateScreen = useAlternateScreen;
            _previousLineCount = 0;
            _cursorHidden = false;
            _inAlternateScreen = false;
        }

        public bool InAlternateScreen => _inAlternateScreen;

        public void EnterAlternateScreen()
        {
            if (_isOutputRedirected()) return;

            if (!_inAlternateScreen)
            {
                // Switch to alternate screen buffer (\x1b[?1049h) and move cursor to home (\x1b[H)
                _writer.Write("\x1b[?1049h\x1b[H");
                _writer.Flush();
                _inAlternateScreen = true;
            }
        }

        public void ExitAlternateScreen()
        {
            if (_inAlternateScreen)
            {
                // Switch back to normal screen buffer (\x1b[?1049l)
                _writer.Write("\x1b[?1049l");
                _writer.Flush();
                _inAlternateScreen = false;
            }
        }

        public void Reset()
        {
            ShowCursor();
            _previousLineCount = 0;
        }

        public void Render(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (_isOutputRedirected())
            {
                _innerRenderer.Render(board);
                return;
            }

            if (_useAlternateScreen && !_inAlternateScreen)
            {
                EnterAlternateScreen();
            }

            HideCursor();

            var boardText = RenderToString(board);
            var lines = boardText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            if (_previousLineCount > 0)
            {
                // Move cursor UP by _previousLineCount lines to column 0 (CPL - Cursor Previous Line)
                _writer.Write($"\x1b[{_previousLineCount}F");
            }

            // Write each line, clearing any leftover characters from previous wider frames
            for (int i = 0; i < lines.Length; i++)
            {
                _writer.Write($"\x1b[2K{lines[i]}\n");
            }

            _previousLineCount = lines.Length;
            _writer.Flush();
        }

        public string RenderToString(IBoard board)
        {
            return _innerRenderer.RenderToString(board);
        }

        private void HideCursor()
        {
            if (!_cursorHidden)
            {
                _writer.Write("\x1b[?25l");
                _cursorHidden = true;
            }
        }

        private void ShowCursor()
        {
            if (_cursorHidden)
            {
                _writer.Write("\x1b[?25h");
                _cursorHidden = false;
            }
        }

        public void Dispose()
        {
            ShowCursor();
            ExitAlternateScreen();
        }
    }
}
