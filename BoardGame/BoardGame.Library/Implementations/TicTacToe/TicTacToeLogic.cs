namespace BoardGame.Library.Implementations.TicTacToe
{
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class TicTacToeLogic : IGameLogic
    {
        public IDictionary<IPlayer, IPiece> PlayerPieces { get; private set; }

        public TicTacToeLogic()
        {
            PlayerPieces = new Dictionary<IPlayer, IPiece>();
        }

        public void MapPieces(IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);
            PlayerPieces.Add(players.ElementAt(0), XPiece.X);
            PlayerPieces.Add(players.ElementAt(1), OPiece.O);
        }

        private static void ValidatePlayers(IEnumerable<IPlayer> players)
        {
            if (players == null) throw new ArgumentNullException("players");
            if (players.Count() != 2) throw new ArgumentOutOfRangeException("players.Count()");
        }

        public bool IsValidMove(IBoard board, ILocation location)
        {
            if (board.CheckBounds(location.X, location.Y))
            {
                var piece = board.PieceAt(location.X, location.Y);
                if (piece.Value == BlankPiece.BlankValue)
                {
                    return true;
                }
            }
            return false;
        }

        public IPiece GetPiece(IPlayer player)
        {
            IPiece piece;
            if (PlayerPieces.TryGetValue(player, out piece))
            {
                return piece;
            }
            throw new ArgumentOutOfRangeException("player");
        }

        public bool ValidMoves(IBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var location = new Location(x, y);
                    if (IsValidMove(board, location))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<IPlayer> GetWinners(IBoard board, IEnumerable<IPlayer> players)
        {
            ValidatePlayers(players);

            for(int x = 0;x < board.Width;x++)
            {
                var pieces = new List<IPiece>();
                for(int y = 0;y < board.Height;y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece.Value != BlankPiece.BlankValue)
                    {
                        pieces.Add(piece);
                    }
                }
                if(pieces.Count() == board.Height)
                {
                    var distinct = pieces.Distinct();
                    if(distinct.Count() == 1)
                    {
                        var winner = PlayerPieces.First(pair => pair.Value.Value == distinct.First().Value).Key;
                        return Enumerable.Repeat(winner, 1);
                    }
                }
            }

            if(!ValidMoves(board)) return players;

            return Enumerable.Empty<IPlayer>();
        }
    }
}
