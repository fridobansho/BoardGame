namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;

    public class Piece : IPiece, IEquatable<IPiece>
    {
        public string Value { get; }

        public Piece() : this(string.Empty)
        {
        }
        
        public Piece(string value)
        {
            Value = value;
        }

        public bool Equals(IPiece other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IPiece);
        }

        public override int GetHashCode()
        {
            return Value != null ? StringComparer.Ordinal.GetHashCode(Value) : 0;
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
