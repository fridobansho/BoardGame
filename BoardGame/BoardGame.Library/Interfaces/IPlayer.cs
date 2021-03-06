﻿namespace BoardGame.Library.Interfaces
{
    public interface IPlayer
    {
        string Name { get; }

        ILocation GetMove(IBoard board);
    }
}
