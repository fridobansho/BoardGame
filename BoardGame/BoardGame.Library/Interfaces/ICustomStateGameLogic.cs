namespace BoardGame.Library.Interfaces
{
    using System.Collections.Generic;

    public interface ICustomStateGameLogic : IGameLogic
    {
        Dictionary<string, string> ExportCustomState(IReadOnlyList<IPlayer> players);

        void ImportCustomState(IReadOnlyList<IPlayer> players, IReadOnlyDictionary<string, string> state);
    }
}
