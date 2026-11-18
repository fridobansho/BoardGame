namespace BoardGame.Library.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Interfaces;

    public class TilePlacement : IEquatable<TilePlacement>
    {
        public int X { get; }
        public int Y { get; }
        public char Letter { get; }

        public TilePlacement(int x, int y, char letter)
        {
            X = x;
            Y = y;
            Letter = char.ToUpperInvariant(letter);
        }

        public bool Equals(TilePlacement other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y && Letter == other.Letter;
        }

        public override bool Equals(object obj)
        {
            return obj is TilePlacement other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Letter);
        }

        public override string ToString()
        {
            return $"{Letter}@({X},{Y})";
        }
    }

    public class WordMove : Location, IMove
    {
        public IReadOnlyList<TilePlacement> Placements { get; }
        public bool IsPass { get; }

        public static WordMove Pass { get; } = new WordMove(Array.Empty<TilePlacement>(), isPass: true);

        public WordMove(IEnumerable<TilePlacement> placements, bool isPass = false)
            : base(GetAnchorX(placements), GetAnchorY(placements))
        {
            IsPass = isPass;
            Placements = placements != null
                ? placements.Where(p => p != null).ToList()
                : new List<TilePlacement>();
        }

        public WordMove(params TilePlacement[] placements)
            : this((IEnumerable<TilePlacement>)placements, false)
        {
        }

        private static int GetAnchorX(IEnumerable<TilePlacement> placements)
        {
            if (placements == null) return 0;
            var first = placements.FirstOrDefault(p => p != null);
            return first?.X ?? 0;
        }

        private static int GetAnchorY(IEnumerable<TilePlacement> placements)
        {
            if (placements == null) return 0;
            var first = placements.FirstOrDefault(p => p != null);
            return first?.Y ?? 0;
        }

        public static WordMove FromWord(IBoard board, int startX, int startY, bool horizontal, string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return new WordMove(Array.Empty<TilePlacement>());
            }

            var placements = new List<TilePlacement>();
            string normalized = word.Trim().ToUpperInvariant();

            for (int i = 0; i < normalized.Length; i++)
            {
                int x = horizontal ? startX + i : startX;
                int y = horizontal ? startY : startY + i;
                char ch = normalized[i];

                if (board != null && board.CheckBounds(x, y))
                {
                    var existing = board.PieceAt(x, y);
                    if (existing != null && existing.Value != BlankPiece.BlankValue)
                    {
                        if (existing.Value.Length == 1 && char.ToUpperInvariant(existing.Value[0]) == ch)
                        {
                            continue;
                        }
                    }
                }

                placements.Add(new TilePlacement(x, y, ch));
            }

            return new WordMove(placements);
        }

        public override string ToString()
        {
            if (IsPass) return "PASS";
            return string.Join(", ", Placements.Select(p => p.ToString()));
        }
    }
}
