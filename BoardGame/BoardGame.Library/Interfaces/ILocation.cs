namespace BoardGame.Library.Interfaces
{
    public interface ILocation : IMove
    {
        int X { get; }
        int Y { get; }
    }
}
