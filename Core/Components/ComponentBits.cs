#if !COMPONENT_BITS_32 && !COMPONENT_BITS_64 && !COMPONENT_BITS_128 && !COMPONENT_BITS_256
#define COMPONENT_BITS_32
#endif

using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace UnsafeEcs.Core.Components
{
    public struct ComponentBits : IEquatable<ComponentBits>
    {
#if COMPONENT_BITS_32
        // 32 bits total
        public uint part0; // 0-31
        private const int PARTS_COUNT = 1;
        private const int MAX_COMPONENTS = 32;
        private const int BITS_PER_PART = 32;
        private const int SHIFT_BITS = 5; // 2^5 = 32
        private const int BIT_MASK = 0x1F; // 31 in binary: 11111
#elif COMPONENT_BITS_64
            // 64 bits total
            public ulong part0; // 0-63
            private const int PARTS_COUNT = 1;
            private const int MAX_COMPONENTS = 64;
            private const int BITS_PER_PART = 64;
            private const int SHIFT_BITS = 6; // 2^6 = 64
            private const int BIT_MASK = 0x3F; // 63 in binary: 111111
#elif COMPONENT_BITS_128
            // 128 bits total
            public ulong part0; // 0-63
            public ulong part1; // 64-127
            private const int PARTS_COUNT = 2;
            private const int MAX_COMPONENTS = 128;
            private const int BITS_PER_PART = 64;
            private const int SHIFT_BITS = 6; // 2^6 = 64
            private const int BIT_MASK = 0x3F; // 63 in binary: 111111
#elif COMPONENT_BITS_256
            // 256 bits total
            public ulong part0; // 0-63
            public ulong part1; // 64-127
            public ulong part2; // 128-191
            public ulong part3; // 192-255
            private const int PARTS_COUNT = 4;
            private const int MAX_COMPONENTS = 256;
            private const int BITS_PER_PART = 64;
            private const int SHIFT_BITS = 6; // 2^6 = 64
            private const int BIT_MASK = 0x3F; // 63 in binary: 111111
#endif

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if COMPONENT_BITS_32
                return part0 == 0;
#elif COMPONENT_BITS_64
                    return part0 == 0;
#elif COMPONENT_BITS_128
                    return part0 == 0 && part1 == 0;
#elif COMPONENT_BITS_256
                    return part0 == 0 && part1 == 0 && part2 == 0 && part3 == 0;
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasComponent(int index)
        {
#if DEBUG
            if ((uint)index >= MAX_COMPONENTS)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif

#if COMPONENT_BITS_32
            var bit = index & BIT_MASK;
            var mask = 1U << bit;
            return (part0 & mask) != 0;
#else
                var part = index >> SHIFT_BITS;
                var bit = index & BIT_MASK;
                var mask = 1UL << bit;

                #if COMPONENT_BITS_64
                    return (part0 & mask) != 0;
                #elif COMPONENT_BITS_128
                    return part == 0 ? (part0 & mask) != 0 : (part1 & mask) != 0;
                #elif COMPONENT_BITS_256
                    return part == 0 ? (part0 & mask) != 0 :
                           part == 1 ? (part1 & mask) != 0 :
                           part == 2 ? (part2 & mask) != 0 :
                           (part3 & mask) != 0;
                #endif
#endif
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
            if ((uint)index >= MAX_COMPONENTS)
                throw new ArgumentOutOfRangeException(nameof(index));
#endif

#if COMPONENT_BITS_32
            var bit = index & BIT_MASK;
            var mask = 1U << bit;

            if (remove)
                part0 &= ~mask;
            else
                part0 |= mask;
#else
                var part = index >> SHIFT_BITS;
                var bit = index & BIT_MASK;
                var mask = 1UL << bit;

                #if COMPONENT_BITS_64
                    if (remove)
                        part0 &= ~mask;
                    else
                        part0 |= mask;
                #elif COMPONENT_BITS_128
                    if (part == 0)
                    {
                        if (remove)
                            part0 &= ~mask;
                        else
                            part0 |= mask;
                    }
                    else
                    {
                        if (remove)
                            part1 &= ~mask;
                        else
                            part1 |= mask;
                    }
                #elif COMPONENT_BITS_256
                    ref var target = ref part0;
                    if (part == 1)
                        target = ref part1;
                    else if (part == 2)
                        target = ref part2;
                    else if (part == 3)
                        target = ref part3;

                    if (remove)
                        target &= ~mask;
                    else
                        target |= mask;
                #endif
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAll()
        {
#if COMPONENT_BITS_32
            part0 = 0;
#elif COMPONENT_BITS_64
                part0 = 0;
#elif COMPONENT_BITS_128
                part0 = 0;
                part1 = 0;
#elif COMPONENT_BITS_256
                part0 = 0;
                part1 = 0;
                part2 = 0;
                part3 = 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAny(in ComponentBits other)
        {
#if COMPONENT_BITS_32
            return (part0 & other.part0) != 0;
#elif COMPONENT_BITS_64
                return (part0 & other.part0) != 0;
#elif COMPONENT_BITS_128
                return (part0 & other.part0) != 0 || (part1 & other.part1) != 0;
#elif COMPONENT_BITS_256
                return (part0 & other.part0) != 0 ||
                       (part1 & other.part1) != 0 ||
                       (part2 & other.part2) != 0 ||
                       (part3 & other.part3) != 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool HasAll(in ComponentBits other)
        {
#if COMPONENT_BITS_32
            return (part0 & other.part0) == other.part0;
#elif COMPONENT_BITS_64
                return (part0 & other.part0) == other.part0;
#elif COMPONENT_BITS_128
                return (part0 & other.part0) == other.part0 && (part1 & other.part1) == other.part1;
#elif COMPONENT_BITS_256
                return (part0 & other.part0) == other.part0 &&
                       (part1 & other.part1) == other.part1 &&
                       (part2 & other.part2) == other.part2 &&
                       (part3 & other.part3) == other.part3;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentBits operator &(in ComponentBits left, in ComponentBits right)
        {
#if COMPONENT_BITS_32
            return new ComponentBits { part0 = left.part0 & right.part0 };
#elif COMPONENT_BITS_64
                return new ComponentBits { part0 = left.part0 & right.part0 };
#elif COMPONENT_BITS_128
                return new ComponentBits
                {
                    part0 = left.part0 & right.part0,
                    part1 = left.part1 & right.part1
                };
#elif COMPONENT_BITS_256
                return new ComponentBits
                {
                    part0 = left.part0 & right.part0,
                    part1 = left.part1 & right.part1,
                    part2 = left.part2 & right.part2,
                    part3 = left.part3 & right.part3
                };
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ComponentBits operator |(in ComponentBits left, in ComponentBits right)
        {
#if COMPONENT_BITS_32
            return new ComponentBits { part0 = left.part0 | right.part0 };
#elif COMPONENT_BITS_64
                return new ComponentBits { part0 = left.part0 | right.part0 };
#elif COMPONENT_BITS_128
                return new ComponentBits
                {
                    part0 = left.part0 | right.part0,
                    part1 = left.part1 | right.part1
                };
#elif COMPONENT_BITS_256
                return new ComponentBits
                {
                    part0 = left.part0 | right.part0,
                    part1 = left.part1 | right.part1,
                    part2 = left.part2 | right.part2,
                    part3 = left.part3 | right.part3
                };
#endif
        }

        public readonly override bool Equals(object obj)
        {
            if (obj is not ComponentBits bits)
                return false;

#if COMPONENT_BITS_32
            return part0 == bits.part0;
#elif COMPONENT_BITS_64
                return part0 == bits.part0;
#elif COMPONENT_BITS_128
                return part0 == bits.part0 && part1 == bits.part1;
#elif COMPONENT_BITS_256
                return part0 == bits.part0 &&
                       part1 == bits.part1 &&
                       part2 == bits.part2 &&
                       part3 == bits.part3;
#endif
        }

        public readonly override int GetHashCode()
        {
#if COMPONENT_BITS_32
            return part0.GetHashCode();
#elif COMPONENT_BITS_64
                return part0.GetHashCode();
#elif COMPONENT_BITS_128
                return HashCode.Combine(part0, part1);
#elif COMPONENT_BITS_256
                return HashCode.Combine(part0, part1, part2, part3);
#endif
        }

        public struct BitEnumerator
        {
            private readonly ComponentBits m_bits;
            private int m_currentPart;
#if COMPONENT_BITS_32
            private uint m_currentPartBits;
#else
                private ulong m_currentPartBits;
#endif

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
#if COMPONENT_BITS_32
                if (m_currentPart < 1)
                {
                    if (m_currentPartBits != 0)
                    {
                        var bitIndex = math.tzcnt(m_currentPartBits);
                        Current = bitIndex;
                        m_currentPartBits &= m_currentPartBits - 1; // Clear lowest set bit
                        return true;
                    }

                    m_currentPart++;
                }

                return false;
#elif COMPONENT_BITS_64
                    if (m_currentPart < 1)
                    {
                        if (m_currentPartBits != 0)
                        {
                            var bitIndex = math.tzcnt(m_currentPartBits);
                            Current = bitIndex;
                            m_currentPartBits &= m_currentPartBits - 1; // Clear lowest set bit
                            return true;
                        }
                        m_currentPart++;
                    }
                    return false;
#elif COMPONENT_BITS_128
                    while (m_currentPart < 2)
                    {
                        if (m_currentPartBits != 0)
                        {
                            var bitIndex = math.tzcnt(m_currentPartBits);
                            Current = bitIndex + m_currentPart * BITS_PER_PART;
                            m_currentPartBits &= m_currentPartBits - 1;
                            return true;
                        }

                        m_currentPart++;
                        if (m_currentPart == 1)
                            m_currentPartBits = m_bits.part1;
                    }
                    return false;
#elif COMPONENT_BITS_256
                    while (m_currentPart < 4)
                    {
                        if (m_currentPartBits != 0)
                        {
                            var bitIndex = math.tzcnt(m_currentPartBits);
                            Current = bitIndex + m_currentPart * BITS_PER_PART;
                            m_currentPartBits &= m_currentPartBits - 1;
                            return true;
                        }

                        m_currentPart++;
                        if (m_currentPart == 1)
                            m_currentPartBits = m_bits.part1;
                        else if (m_currentPart == 2)
                            m_currentPartBits = m_bits.part2;
                        else if (m_currentPart == 3)
                            m_currentPartBits = m_bits.part3;
                    }
                    return false;
#endif
            }
        }

        public BitEnumerator GetEnumerator()
        {
            return new BitEnumerator(this);
        }

        public bool TryGetNextBit(ref int current, out int nextBit)
        {
#if COMPONENT_BITS_32
            var bitInPart = current & BIT_MASK;
            var mask = ~((1U << bitInPart) - 1);

            var partBits = part0 & mask;
            if (partBits != 0)
            {
                nextBit = math.tzcnt(partBits);
                current = nextBit;
                return true;
            }

            nextBit = -1;
            return false;
#else
                var part = current >> SHIFT_BITS;
                var bitInPart = current & BIT_MASK;
                var mask = ~((1UL << bitInPart) - 1);

                #if COMPONENT_BITS_64
                    if (part < 1)
                    {
                        ulong partBits = part0 & mask;
                        if (partBits != 0)
                        {
                            nextBit = math.tzcnt(partBits);
                            current = nextBit;
                            return true;
                        }
                    }
                    nextBit = -1;
                    return false;
                #elif COMPONENT_BITS_128
                    while (part < 2)
                    {
                        ulong partBits;
                        if (part == 0)
                            partBits = part0 & mask;
                        else
                            partBits = part1 & mask;

                        if (partBits != 0)
                        {
                            nextBit = math.tzcnt(partBits) + part * BITS_PER_PART;
                            current = nextBit;
                            return true;
                        }

                        part++;
                        mask = ~0UL;
                    }
                    nextBit = -1;
                    return false;
                #elif COMPONENT_BITS_256
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
                            nextBit = math.tzcnt(partBits) + part * BITS_PER_PART;
                            current = nextBit;
                            return true;
                        }

                        part++;
                        mask = ~0UL;
                    }
                    nextBit = -1;
                    return false;
                #endif
#endif
        }

        public bool Equals(ComponentBits other)
        {
#if COMPONENT_BITS_32
            return part0 == other.part0;
#elif COMPONENT_BITS_64
                return part0 == other.part0;
#elif COMPONENT_BITS_128
                return part0 == other.part0 && part1 == other.part1;
#elif COMPONENT_BITS_256
                return part0 == other.part0 && 
                       part1 == other.part1 && 
                       part2 == other.part2 && 
                       part3 == other.part3;
#endif
        }

        public void Clear()
        {
#if COMPONENT_BITS_32
            part0 = 0;
#elif COMPONENT_BITS_64
                part0 = 0;
#elif COMPONENT_BITS_128
                part0 = 0;
                part1 = 0;
#elif COMPONENT_BITS_256
                part0 = 0;
                part1 = 0;
                part2 = 0;
                part3 = 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(in ComponentBits left, in ComponentBits right)
        {
#if COMPONENT_BITS_32
            return left.part0 == right.part0;
#elif COMPONENT_BITS_64
                return left.part0 == right.part0;
#elif COMPONENT_BITS_128
                return left.part0 == right.part0 && left.part1 == right.part1;
#elif COMPONENT_BITS_256
                return left.part0 == right.part0 &&
                       left.part1 == right.part1 &&
                       left.part2 == right.part2 &&
                       left.part3 == right.part3;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(in ComponentBits left, in ComponentBits right)
        {
#if COMPONENT_BITS_32
            return left.part0 != right.part0;
#elif COMPONENT_BITS_64
                return left.part0 != right.part0;
#elif COMPONENT_BITS_128
                return left.part0 != right.part0 || left.part1 != right.part1;
#elif COMPONENT_BITS_256
                return left.part0 != right.part0 ||
                       left.part1 != right.part1 ||
                       left.part2 != right.part2 ||
                       left.part3 != right.part3;
#endif
        }
    }
}