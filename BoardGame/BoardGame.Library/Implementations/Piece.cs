namespace BoardGame.Library.Implementations
{
    using Interfaces;
    public class Piece : IPiece
    {
        public const string Blank = " ";

        public string Value { get; }

        public Piece()
        {
            Value = Blank;
        }

        public Piece(string value)
        {
            Value = value;
        }
    }
}
