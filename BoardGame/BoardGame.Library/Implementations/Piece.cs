namespace BoardGame.Library.Implementations
{
    using Interfaces;
    public class Piece : IPiece
    {
        public const string BlankValue = " ";

        public static IPiece Blank = new Piece(BlankValue);

        public string Value { get; }

        public Piece()
        {
            Value = BlankValue;
        }

        public Piece(string value)
        {
            Value = value;
        }
    }
}
