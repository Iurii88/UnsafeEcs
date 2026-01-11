using System.Collections.Generic;
using NUnit.Framework;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.ComponentTests
{
    [TestFixture]
    public class ComponentBitsTests
    {
        #region HasComponent Tests

        [Test]
        public void HasComponent_ReturnsFalse_WhenBitNotSet()
        {
            var bits = new ComponentBits();

            Assert.IsFalse(bits.HasComponent(0));
            Assert.IsFalse(bits.HasComponent(63));
            Assert.IsFalse(bits.HasComponent(127));
            Assert.IsFalse(bits.HasComponent(255));
        }

        [Test]
        public void HasComponent_ReturnsTrue_WhenBitIsSet()
        {
            var bits = new ComponentBits();
            bits.SetComponent(42);

            Assert.IsTrue(bits.HasComponent(42));
        }

        [Test]
        public void HasComponent_WorksForAllParts()
        {
            var bits = new ComponentBits();

            // Part 0 (0-63)
            bits.SetComponent(0);
            Assert.IsTrue(bits.HasComponent(0));

            // Part 1 (64-127)
            bits.SetComponent(64);
            Assert.IsTrue(bits.HasComponent(64));

            // Part 2 (128-191)
            bits.SetComponent(128);
            Assert.IsTrue(bits.HasComponent(128));

            // Part 3 (192-255)
            bits.SetComponent(192);
            Assert.IsTrue(bits.HasComponent(192));
        }

        [Test]
        public void HasComponent_HandlesEdgeCases()
        {
            var bits = new ComponentBits();

            // Test boundary indices
            bits.SetComponent(63);  // Last of part 0
            bits.SetComponent(64);  // First of part 1
            bits.SetComponent(127); // Last of part 1
            bits.SetComponent(128); // First of part 2
            bits.SetComponent(191); // Last of part 2
            bits.SetComponent(192); // First of part 3
            bits.SetComponent(255); // Last of part 3

            Assert.IsTrue(bits.HasComponent(63));
            Assert.IsTrue(bits.HasComponent(64));
            Assert.IsTrue(bits.HasComponent(127));
            Assert.IsTrue(bits.HasComponent(128));
            Assert.IsTrue(bits.HasComponent(191));
            Assert.IsTrue(bits.HasComponent(192));
            Assert.IsTrue(bits.HasComponent(255));
        }

        #endregion

        #region SetComponent Tests

        [Test]
        public void SetComponent_SetsBit()
        {
            var bits = new ComponentBits();

            bits.SetComponent(10);

            Assert.IsTrue(bits.HasComponent(10));
        }

        [Test]
        public void SetComponent_CanSetMultipleBits()
        {
            var bits = new ComponentBits();

            bits.SetComponent(0);
            bits.SetComponent(50);
            bits.SetComponent(100);
            bits.SetComponent(200);

            Assert.IsTrue(bits.HasComponent(0));
            Assert.IsTrue(bits.HasComponent(50));
            Assert.IsTrue(bits.HasComponent(100));
            Assert.IsTrue(bits.HasComponent(200));
        }

        [Test]
        public void SetComponent_IsIdempotent()
        {
            var bits = new ComponentBits();

            bits.SetComponent(42);
            bits.SetComponent(42);
            bits.SetComponent(42);

            Assert.IsTrue(bits.HasComponent(42));
        }

        #endregion

        #region RemoveComponent Tests

        [Test]
        public void RemoveComponent_ClearsBit()
        {
            var bits = new ComponentBits();
            bits.SetComponent(42);

            bits.RemoveComponent(42);

            Assert.IsFalse(bits.HasComponent(42));
        }

        [Test]
        public void RemoveComponent_DoesNotAffectOtherBits()
        {
            var bits = new ComponentBits();
            bits.SetComponent(41);
            bits.SetComponent(42);
            bits.SetComponent(43);

            bits.RemoveComponent(42);

            Assert.IsTrue(bits.HasComponent(41));
            Assert.IsFalse(bits.HasComponent(42));
            Assert.IsTrue(bits.HasComponent(43));
        }

        [Test]
        public void RemoveComponent_WorksForAllParts()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(74);
            bits.SetComponent(138);
            bits.SetComponent(202);

            bits.RemoveComponent(10);
            bits.RemoveComponent(74);
            bits.RemoveComponent(138);
            bits.RemoveComponent(202);

            Assert.IsFalse(bits.HasComponent(10));
            Assert.IsFalse(bits.HasComponent(74));
            Assert.IsFalse(bits.HasComponent(138));
            Assert.IsFalse(bits.HasComponent(202));
        }

        #endregion

        #region ClearAll / Clear Tests

        [Test]
        public void ClearAll_ClearsAllBits()
        {
            var bits = new ComponentBits();
            bits.SetComponent(0);
            bits.SetComponent(63);
            bits.SetComponent(64);
            bits.SetComponent(127);
            bits.SetComponent(128);
            bits.SetComponent(191);
            bits.SetComponent(192);
            bits.SetComponent(255);

            bits.ClearAll();

            Assert.IsTrue(bits.IsEmpty);
            Assert.IsFalse(bits.HasComponent(0));
            Assert.IsFalse(bits.HasComponent(255));
        }

        [Test]
        public void Clear_ClearsAllBits()
        {
            var bits = new ComponentBits();
            bits.SetComponent(42);
            bits.SetComponent(100);

            bits.Clear();

            Assert.IsTrue(bits.IsEmpty);
        }

        #endregion

        #region IsEmpty Tests

        [Test]
        public void IsEmpty_ReturnsTrue_WhenNoBitsSet()
        {
            var bits = new ComponentBits();

            Assert.IsTrue(bits.IsEmpty);
        }

        [Test]
        public void IsEmpty_ReturnsFalse_WhenAnyBitSet()
        {
            var bits = new ComponentBits();
            bits.SetComponent(0);

            Assert.IsFalse(bits.IsEmpty);
        }

        [Test]
        public void IsEmpty_ReturnsFalse_ForEachPart()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(0);
            Assert.IsFalse(bits1.IsEmpty);

            var bits2 = new ComponentBits();
            bits2.SetComponent(64);
            Assert.IsFalse(bits2.IsEmpty);

            var bits3 = new ComponentBits();
            bits3.SetComponent(128);
            Assert.IsFalse(bits3.IsEmpty);

            var bits4 = new ComponentBits();
            bits4.SetComponent(192);
            Assert.IsFalse(bits4.IsEmpty);
        }

        #endregion

        #region HasAll Tests

        [Test]
        public void HasAll_ReturnsTrue_WhenAllBitsMatch()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(20);
            bits.SetComponent(30);

            var mask = new ComponentBits();
            mask.SetComponent(10);
            mask.SetComponent(20);

            Assert.IsTrue(bits.HasAll(mask));
        }

        [Test]
        public void HasAll_ReturnsFalse_WhenSomeBitsMissing()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(20);

            var mask = new ComponentBits();
            mask.SetComponent(10);
            mask.SetComponent(30);

            Assert.IsFalse(bits.HasAll(mask));
        }

        [Test]
        public void HasAll_ReturnsTrue_ForEmptyMask()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);

            var emptyMask = new ComponentBits();

            Assert.IsTrue(bits.HasAll(emptyMask));
        }

        [Test]
        public void HasAll_WorksAcrossAllParts()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(74);
            bits.SetComponent(138);
            bits.SetComponent(202);

            var mask = new ComponentBits();
            mask.SetComponent(10);
            mask.SetComponent(74);
            mask.SetComponent(138);
            mask.SetComponent(202);

            Assert.IsTrue(bits.HasAll(mask));
        }

        #endregion

        #region HasAny Tests

        [Test]
        public void HasAny_ReturnsTrue_WhenAnyBitMatches()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);

            var mask = new ComponentBits();
            mask.SetComponent(10);
            mask.SetComponent(20);

            Assert.IsTrue(bits.HasAny(mask));
        }

        [Test]
        public void HasAny_ReturnsFalse_WhenNoBitsMatch()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);

            var mask = new ComponentBits();
            mask.SetComponent(20);
            mask.SetComponent(30);

            Assert.IsFalse(bits.HasAny(mask));
        }

        [Test]
        public void HasAny_ReturnsFalse_ForEmptyMask()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);

            var emptyMask = new ComponentBits();

            Assert.IsFalse(bits.HasAny(emptyMask));
        }

        [Test]
        public void HasAny_WorksAcrossAllParts()
        {
            var bits = new ComponentBits();
            bits.SetComponent(202); // Part 3

            var mask = new ComponentBits();
            mask.SetComponent(10);   // Part 0
            mask.SetComponent(202);  // Part 3

            Assert.IsTrue(bits.HasAny(mask));
        }

        #endregion

        #region Operator Tests

        [Test]
        public void OperatorAnd_ReturnsIntersection()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(10);
            bits1.SetComponent(20);
            bits1.SetComponent(30);

            var bits2 = new ComponentBits();
            bits2.SetComponent(20);
            bits2.SetComponent(30);
            bits2.SetComponent(40);

            var result = bits1 & bits2;

            Assert.IsFalse(result.HasComponent(10));
            Assert.IsTrue(result.HasComponent(20));
            Assert.IsTrue(result.HasComponent(30));
            Assert.IsFalse(result.HasComponent(40));
        }

        [Test]
        public void OperatorOr_ReturnsUnion()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(10);
            bits1.SetComponent(20);

            var bits2 = new ComponentBits();
            bits2.SetComponent(30);
            bits2.SetComponent(40);

            var result = bits1 | bits2;

            Assert.IsTrue(result.HasComponent(10));
            Assert.IsTrue(result.HasComponent(20));
            Assert.IsTrue(result.HasComponent(30));
            Assert.IsTrue(result.HasComponent(40));
        }

        [Test]
        public void OperatorEquals_ReturnsTrue_ForEqualBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(10);
            bits1.SetComponent(100);

            var bits2 = new ComponentBits();
            bits2.SetComponent(10);
            bits2.SetComponent(100);

            Assert.IsTrue(bits1 == bits2);
        }

        [Test]
        public void OperatorEquals_ReturnsFalse_ForDifferentBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(10);

            var bits2 = new ComponentBits();
            bits2.SetComponent(20);

            Assert.IsFalse(bits1 == bits2);
        }

        [Test]
        public void OperatorNotEquals_ReturnsTrue_ForDifferentBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(10);

            var bits2 = new ComponentBits();
            bits2.SetComponent(20);

            Assert.IsTrue(bits1 != bits2);
        }

        #endregion

        #region Equals and GetHashCode Tests

        [Test]
        public void Equals_ReturnsTrue_ForEqualBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(42);

            var bits2 = new ComponentBits();
            bits2.SetComponent(42);

            Assert.IsTrue(bits1.Equals(bits2));
            Assert.IsTrue(bits1.Equals((object)bits2));
        }

        [Test]
        public void Equals_ReturnsFalse_ForDifferentBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(42);

            var bits2 = new ComponentBits();
            bits2.SetComponent(43);

            Assert.IsFalse(bits1.Equals(bits2));
        }

        [Test]
        public void Equals_ReturnsFalse_ForNull()
        {
            var bits = new ComponentBits();
            bits.SetComponent(42);

            Assert.IsFalse(bits.Equals(null));
        }

        [Test]
        public void GetHashCode_ReturnsSameValue_ForEqualBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(42);
            bits1.SetComponent(100);

            var bits2 = new ComponentBits();
            bits2.SetComponent(42);
            bits2.SetComponent(100);

            Assert.AreEqual(bits1.GetHashCode(), bits2.GetHashCode());
        }

        [Test]
        public void GetHashCode_ReturnsDifferentValue_ForDifferentBits()
        {
            var bits1 = new ComponentBits();
            bits1.SetComponent(42);

            var bits2 = new ComponentBits();
            bits2.SetComponent(43);

            // Not guaranteed, but highly likely for different bits
            Assert.AreNotEqual(bits1.GetHashCode(), bits2.GetHashCode());
        }

        #endregion

        #region BitEnumerator Tests

        [Test]
        public void GetEnumerator_EnumeratesAllSetBits()
        {
            var bits = new ComponentBits();
            bits.SetComponent(5);
            bits.SetComponent(10);
            bits.SetComponent(100);
            bits.SetComponent(200);

            var enumerated = new List<int>();
            foreach (var bit in bits)
            {
                enumerated.Add(bit);
            }

            Assert.AreEqual(4, enumerated.Count);
            Assert.Contains(5, enumerated);
            Assert.Contains(10, enumerated);
            Assert.Contains(100, enumerated);
            Assert.Contains(200, enumerated);
        }

        [Test]
        public void GetEnumerator_EnumeratesInOrder()
        {
            var bits = new ComponentBits();
            bits.SetComponent(200);
            bits.SetComponent(5);
            bits.SetComponent(100);
            bits.SetComponent(10);

            var enumerated = new List<int>();
            foreach (var bit in bits)
            {
                enumerated.Add(bit);
            }

            // Should be in ascending order
            Assert.AreEqual(5, enumerated[0]);
            Assert.AreEqual(10, enumerated[1]);
            Assert.AreEqual(100, enumerated[2]);
            Assert.AreEqual(200, enumerated[3]);
        }

        [Test]
        public void GetEnumerator_ReturnsEmpty_WhenNoBitsSet()
        {
            var bits = new ComponentBits();

            var count = 0;
            foreach (var _ in bits)
            {
                count++;
            }

            Assert.AreEqual(0, count);
        }

        #endregion

        #region TryGetNextBit Tests

        [Test]
        public void TryGetNextBit_FindsNextSetBit()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(50);
            bits.SetComponent(100);

            var current = 0;
            Assert.IsTrue(bits.TryGetNextBit(ref current, out var nextBit));
            Assert.AreEqual(10, nextBit);
        }

        [Test]
        public void TryGetNextBit_IteratesAllBits()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);
            bits.SetComponent(50);
            bits.SetComponent(100);

            var found = new List<int>();
            var current = 0;

            while (bits.TryGetNextBit(ref current, out var nextBit))
            {
                found.Add(nextBit);
                current = nextBit + 1;
            }

            Assert.AreEqual(3, found.Count);
            Assert.Contains(10, found);
            Assert.Contains(50, found);
            Assert.Contains(100, found);
        }

        [Test]
        public void TryGetNextBit_ReturnsFalse_WhenNoBitsRemaining()
        {
            var bits = new ComponentBits();
            bits.SetComponent(10);

            var current = 11;
            Assert.IsFalse(bits.TryGetNextBit(ref current, out var nextBit));
            Assert.AreEqual(-1, nextBit);
        }

        [Test]
        public void TryGetNextBit_ReturnsFalse_ForEmptyBits()
        {
            var bits = new ComponentBits();

            var current = 0;
            Assert.IsFalse(bits.TryGetNextBit(ref current, out var nextBit));
            Assert.AreEqual(-1, nextBit);
        }

        #endregion
    }
}
