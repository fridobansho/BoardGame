namespace BoardGame.Library.Implementations.Silly
{
    using Interfaces;

    public class SillyPlayer : IPlayer
    {
        public string Name { get; }

        public SillyPlayer() : this(typeof(SillyPlayer).Name)
        {
        }

        public SillyPlayer(string name)
        {
            Name = name;
        }

        public ILocation GetMove(IBoard board)
        {
            return new Location();
        }
    }
}
