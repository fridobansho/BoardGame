namespace BoardGame.Library.Implementations
{
    using Interfaces;
    public class Piece : IPiece
    {
        public string Value { get; }

        public Piece() : this(string.Empty)
        {
        }
        
        public Piece(string value)
        {
            Value = value;
        }
    }
}
