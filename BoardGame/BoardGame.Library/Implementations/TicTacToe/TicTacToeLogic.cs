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

            // Check verticals
            for (int x = 0; x < board.Width; x++)
            {
                var pieces = new List<IPiece>();
                for (int y = 0; y < board.Height; y++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        pieces.Add(piece);
                    }
                }
                if (TryGetLineWinner(pieces, board.Height, out var winner))
                {
                    return Enumerable.Repeat(winner, 1);
                }
            }

            // Check horizontals
            for (int y = 0; y < board.Height; y++)
            {
                var pieces = new List<IPiece>();
                for (int x = 0; x < board.Width; x++)
                {
                    var piece = board.PieceAt(x, y);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        pieces.Add(piece);
                    }
                }
                if (TryGetLineWinner(pieces, board.Width, out var winner))
                {
                    return Enumerable.Repeat(winner, 1);
                }
            }

            // Check diagonals when board is square
            if (board.Width > 0 && board.Width == board.Height)
            {
                var mainDiagonal = new List<IPiece>();
                for (int i = 0; i < board.Width; i++)
                {
                    var piece = board.PieceAt(i, i);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        mainDiagonal.Add(piece);
                    }
                }
                if (TryGetLineWinner(mainDiagonal, board.Width, out var mainWinner))
                {
                    return Enumerable.Repeat(mainWinner, 1);
                }

                var antiDiagonal = new List<IPiece>();
                for (int i = 0; i < board.Width; i++)
                {
                    var piece = board.PieceAt(i, board.Height - 1 - i);
                    if (piece != null && piece.Value != BlankPiece.BlankValue)
                    {
                        antiDiagonal.Add(piece);
                    }
                }
                if (TryGetLineWinner(antiDiagonal, board.Width, out var antiWinner))
                {
                    return Enumerable.Repeat(antiWinner, 1);
                }
            }

            if (!ValidMoves(board)) return players;

            return Enumerable.Empty<IPlayer>();
        }

        private bool TryGetLineWinner(IReadOnlyCollection<IPiece> pieces, int expectedLength, out IPlayer winner)
        {
            winner = null;
            if (expectedLength <= 0 || pieces.Count != expectedLength)
            {
                return false;
            }

            var distinct = pieces.Distinct().ToList();
            if (distinct.Count == 1)
            {
                var winningValue = distinct[0].Value;
                var match = PlayerPieces.FirstOrDefault(pair => pair.Value.Value == winningValue);
                if (match.Key != null)
                {
                    winner = match.Key;
                    return true;
                }
            }

            return false;
        }
    }
}
