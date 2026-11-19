namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;

    public class SudokuMove : Location, IMove, IEquatable<SudokuMove>
    {
        public int Digit { get; }

        public SudokuMove(int x, int y, int digit) : base(x, y)
        {
            Digit = digit;
        }

        public bool Equals(SudokuMove other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y && Digit == other.Digit;
        }

        public override bool Equals(ILocation other)
        {
            return other is SudokuMove sm && Equals(sm);
        }

        public override bool Equals(object obj)
        {
            return obj is SudokuMove other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Digit);
        }

        public override string ToString()
        {
            return $"{Digit}@({X}, {Y})";
        }
    }
}
