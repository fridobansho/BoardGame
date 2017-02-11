namespace BoardGame.Library.Interfaces
{
    public interface IGameRunner
    {
        IGame Game { get; }

        IPlayer RunGame();
    }
}
