namespace BoardGame.Library.Implementations
{
    using System;
    using Interfaces;

    public class Location : ILocation, IEquatable<ILocation>
    {
        public int X { get; }
        public int Y { get; }

        public Location() : this(0, 0)
        {
        }

        public Location(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(ILocation other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ILocation);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
