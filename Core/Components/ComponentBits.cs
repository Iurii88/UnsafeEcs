using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnsafeEcs.Core.Components
{
    public struct ComponentBits : IEquatable<ComponentBits>
    {
        public ulong part0; // 0-63
        public ulong part1; // 64-127
        public ulong part2; // 128-191
        public ulong part3; // 192-255

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => part0 == 0 && part1 == 0 && part2 == 0 && part3 == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasComponent(int index)
        {
#if DEBUG
            if ((uint)index >= 256)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif

            var part = index >> 6;
            var bit = index & 0x3F;
            var mask = 1UL << bit;

            return part == 0 ? (part0 & mask) != 0 :
                part == 1 ? (part1 & mask) != 0 :
                part == 2 ? (part2 & mask) != 0 :
                (part3 & mask) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetComponent(int index)
        {
            ChangeComponent(index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(int index)
        {
            ChangeComponent(index, true);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ChangeComponent(int index, bool remove = false)
        {
#if DEBUG
            if ((uint)index >= 256)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif

            var part = index >> 6;
            var bit = index & 0x3F;
            var mask = 1UL << bit;

            ref var target = ref part0;
            if (part == 1)
                target = ref part1;
            else if (part == 2)
                target = ref part2;
            else if (part == 3) target = ref part3;

            if (remove)
                target &= ~mask;
            else
                target |= mask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAll()
        {
            (part0, part1, part2, part3) = (0, 0, 0, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAny(in ComponentBits other)
        {
            return (part0 & other.part0) != 0 ||
                   (part1 & other.part1) != 0 ||
                   (part2 & other.part2) != 0 ||
                   (part3 & other.part3) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAll(in ComponentBits other)
        {
            return (part0 & other.part0) == other.part0 &&
                   (part1 & other.part1) == other.part1 &&
                   (part2 & other.part2) == other.part2 &&
                   (part3 & other.part3) == other.part3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentBits operator &(in ComponentBits left, in ComponentBits right)
        {
            return new ComponentBits
            {
                part0 = left.part0 & right.part0,
                part1 = left.part1 & right.part1,
                part2 = left.part2 & right.part2,
                part3 = left.part3 & right.part3
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentBits operator |(in ComponentBits left, in ComponentBits right)
        {
            return new ComponentBits
            {
                part0 = left.part0 | right.part0,
                part1 = left.part1 | right.part1,
                part2 = left.part2 | right.part2,
                part3 = left.part3 | right.part3
            };
        }

        public readonly override bool Equals(object obj)
        {
            return obj is ComponentBits bits &&
                   part0 == bits.part0 &&
                   part1 == bits.part1 &&
                   part2 == bits.part2 &&
                   part3 == bits.part3;
        }

        public readonly override int GetHashCode()
        {
            return HashCode.Combine(part0, part1, part2, part3);
        }

        public struct BitEnumerator
        {
            private readonly ComponentBits m_bits;
            private int m_currentPart;
            private ulong m_currentPartBits;

            public BitEnumerator(ComponentBits bits)
            {
                m_bits = bits;
                m_currentPart = 0;
                m_currentPartBits = bits.part0;
                Current = -1;
            }

            public int Current { get; private set; }

            public bool MoveNext()
            {
                while (m_currentPart < 4)
                {
                    if (m_currentPartBits != 0)
                    {
                        var bitIndex = math.tzcnt(m_currentPartBits);
                        Current = bitIndex + m_currentPart * 64;
                        m_currentPartBits &= m_currentPartBits - 1;
                        return true;
                    }

                    m_currentPart++;
                    if (m_currentPart == 1)
                        m_currentPartBits = m_bits.part1;
                    else if (m_currentPart == 2)
                        m_currentPartBits = m_bits.part2;
                    else if (m_currentPart == 3) m_currentPartBits = m_bits.part3;
                }

                return false;
            }
        }

        public BitEnumerator GetEnumerator()
        {
            return new BitEnumerator(this);
        }

        public bool TryGetNextBit(ref int current, out int nextBit)
        {
            var part = current >> 6;
            var bitInPart = current & 0x3F;
            var mask = ~((1UL << bitInPart) - 1);

            while (part < 4)
            {
                ulong partBits;
                if (part == 0)
                    partBits = part0 & mask;
                else if (part == 1)
                    partBits = part1 & mask;
                else if (part == 2)
                    partBits = part2 & mask;
                else if (part == 3)
                    partBits = part3 & mask;
                else
                    partBits = 0;

                if (partBits != 0)
                {
                    nextBit = math.tzcnt(partBits) + part * 64;
                    current = nextBit;
                    return true;
                }

                part++;
                mask = ~0UL;
            }

            nextBit = -1;
            return false;
        }

        public bool Equals(ComponentBits other)
        {
            return part0 == other.part0 && part1 == other.part1 && part2 == other.part2 && part3 == other.part3;
        }

        public void Clear()
        {
            part0 = 0;
            part1 = 0;
            part2 = 0;
            part3 = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ComponentBits left, in ComponentBits right)
        {
            return left.part0 == right.part0 &&
                   left.part1 == right.part1 &&
                   left.part2 == right.part2 &&
                   left.part3 == right.part3;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ComponentBits left, in ComponentBits right)
        {
            return left.part0 != right.part0 ||
                   left.part1 != right.part1 ||
                   left.part2 != right.part2 ||
                   left.part3 != right.part3;
        }
    }
}