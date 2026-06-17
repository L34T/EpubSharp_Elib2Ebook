#if !NETCOREAPP3_0_OR_GREATER && !NETSTANDARD2_1_OR_GREATER

namespace System
{
    internal readonly struct Index : IEquatable<Index>
    {
        private readonly int _value;

        public Index(int value, bool fromEnd = false)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "Value must be non-negative.");
            _value = fromEnd ? ~value : value;
        }

        public int Value => _value < 0 ? ~_value : _value;
        public bool IsFromEnd => _value < 0;

        public int GetOffset(int length)
        {
            int offset = _value;
            if (IsFromEnd) offset += length + 1;
            return offset;
        }

        public override bool Equals(object obj) => obj is Index index && Equals(index);
        public bool Equals(Index other) => _value == other._value;
        public override int GetHashCode() => _value;
        public static implicit operator Index(int value) => new Index(value);
    }

    internal readonly struct Range : IEquatable<Range>
    {
        public Index Start { get; }
        public Index End { get; }

        public Range(Index start, Index end)
        {
            Start = start;
            End = end;
        }

        public static Range StartAt(Index start) => new Range(start, new Index(0, true));
        public static Range EndAt(Index end) => new Range(new Index(0), end);
        public static Range All => new Range(new Index(0), new Index(0, true));

        public (int Offset, int Length) GetOffsetAndLength(int length)
        {
            int start = Start.GetOffset(length);
            int end = End.GetOffset(length);
            if (start < 0 || end > length || start > end) throw new ArgumentOutOfRangeException(nameof(length));
            return (start, end - start);
        }

        public override bool Equals(object obj) => obj is Range range && Equals(range);
        public bool Equals(Range other) => Start.Equals(other.Start) && End.Equals(other.End);
        public override int GetHashCode() => Start.GetHashCode() * 31 + End.GetHashCode();
    }
}

#endif
