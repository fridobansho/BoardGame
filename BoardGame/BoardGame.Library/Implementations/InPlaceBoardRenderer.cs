namespace BoardGame.Library.Implementations
{
    using Interfaces;
    using System;

    public class InPlaceBoardRenderer : IBoardRenderer
    {
        private readonly IBoardRenderer _innerRenderer;
        private readonly Action<int, int> _setCursorPosition;
        private readonly Func<(int left, int top)> _getCursorPosition;
        private readonly Func<bool> _isOutputRedirected;

        private int _leftOffset;
        private int _topOffset;
        private bool _initialized;

        public InPlaceBoardRenderer() : this(new ConsoleRenderer())
        {
        }

        public InPlaceBoardRenderer(IBoardRenderer innerRenderer)
            : this(
                innerRenderer,
                safeSetCursorPosition: (left, top) =>
                {
                    try { Console.SetCursorPosition(left, top); }
                    catch { /* Fallback gracefully if not a TTY */ }
                },
                safeGetCursorPosition: () =>
                {
                    try { return (Console.CursorLeft, Console.CursorTop); }
                    catch { return (0, 0); }
                },
                isOutputRedirected: () => Console.IsOutputRedirected)
        {
        }

        public InPlaceBoardRenderer(
            IBoardRenderer innerRenderer,
            Action<int, int> safeSetCursorPosition,
            Func<(int left, int top)> safeGetCursorPosition,
            Func<bool> isOutputRedirected)
        {
            _innerRenderer = innerRenderer ?? throw new ArgumentNullException(nameof(innerRenderer));
            _setCursorPosition = safeSetCursorPosition ?? throw new ArgumentNullException(nameof(safeSetCursorPosition));
            _getCursorPosition = safeGetCursorPosition ?? throw new ArgumentNullException(nameof(safeGetCursorPosition));
            _isOutputRedirected = isOutputRedirected ?? throw new ArgumentNullException(nameof(isOutputRedirected));
        }

        public void Reset()
        {
            _initialized = false;
        }

        public void Render(IBoard board)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));

            if (_isOutputRedirected())
            {
                _innerRenderer.Render(board);
                return;
            }

            if (!_initialized)
            {
                var pos = _getCursorPosition();
                _leftOffset = pos.left;
                _topOffset = pos.top;
                _initialized = true;
            }
            else
            {
                _setCursorPosition(_leftOffset, _topOffset);
            }

            _innerRenderer.Render(board);
        }

        public string RenderToString(IBoard board)
        {
            return _innerRenderer.RenderToString(board);
        }
    }
}
