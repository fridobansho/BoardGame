namespace BoardGame.Library.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class PieceMove : Location, IMove, IEquatable<PieceMove>
    {
        public int FromX { get; }
        public int FromY { get; }
        public int ToX { get; }
        public int ToY { get; }
        public ILocation From => new Location(FromX, FromY);
        public ILocation To => new Location(ToX, ToY);
        public IReadOnlyList<ILocation> Path { get; }

        public PieceMove(int fromX, int fromY, int toX, int toY, IEnumerable<ILocation> path = null)
            : base(toX, toY)
        {
            FromX = fromX;
            FromY = fromY;
            ToX = toX;
            ToY = toY;
            if (path != null)
            {
                var list = path.Where(p => p != null).Select(p => (ILocation)new Location(p.X, p.Y)).ToList();
                Path = list.Count > 0 ? list : new ILocation[] { new Location(fromX, fromY), new Location(toX, toY) };
            }
            else
            {
                Path = new ILocation[] { new Location(fromX, fromY), new Location(toX, toY) };
            }
        }

        public PieceMove(ILocation from, ILocation to, IEnumerable<ILocation> path = null)
            : this(
                from?.X ?? throw new ArgumentNullException(nameof(from)),
                from.Y,
                to?.X ?? throw new ArgumentNullException(nameof(to)),
                to.Y,
                path)
        {
        }

        public bool Equals(PieceMove other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FromX == other.FromX
                && FromY == other.FromY
                && ToX == other.ToX
                && ToY == other.ToY
                && Path.Count == other.Path.Count
                && Path.Zip(other.Path, (a, b) => a.X == b.X && a.Y == b.Y).All(eq => eq);
        }

        public override bool Equals(ILocation other)
        {
            return other is PieceMove pm && Equals(pm);
        }

        public override bool Equals(object obj)
        {
            return obj is PieceMove pm && Equals(pm);
        }

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(FromX);
            hash.Add(FromY);
            hash.Add(ToX);
            hash.Add(ToY);
            foreach (var p in Path)
            {
                hash.Add(p.X);
                hash.Add(p.Y);
            }
            return hash.ToHashCode();
        }

        public override string ToString()
        {
            return $"({FromX}, {FromY}) -> ({ToX}, {ToY})";
        }
    }
}
