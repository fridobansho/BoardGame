namespace BoardGame.Library.Implementations
{
    using Interfaces;

    public class Player : IPlayer
    {
        public string Name { get; private set; }

        public Player(string name)
        {
            Name = name;
        }
    }
}
