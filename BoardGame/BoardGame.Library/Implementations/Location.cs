namespace BoardGame.Library.Implementations
{
    using Interfaces;

    public class Location : ILocation
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
    }
}
