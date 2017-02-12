namespace BoardGame.Library.Interfaces
{
    using System.Collections.Generic;

    public interface IGameRunner
    {
        IGame Game { get; }
        
        IEnumerable<IPlayer> RunGame();
    }
}
