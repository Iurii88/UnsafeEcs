using NUnit.Framework;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Tests.Editor.ComponentTests
{
    public struct TypeTestComponentA : IComponent
    {
        public int value;
    }

    public struct TypeTestComponentB : IComponent
    {
        public float x;
        public float y;
    }

    public struct TypeTestBufferElement : IBufferElement
    {
        public int data;
    }

    [TestFixture]
    public class TypeManagerTests : UnsafeEcsBaseTest
    {
        #region GetComponentTypeIndex Tests

        [Test]
        public void GetComponentTypeIndex_ReturnsSameIndex_ForSameType()
        {
            var index1 = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var index2 = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();

            Assert.AreEqual(index1, index2);
        }

        [Test]
        public void GetComponentTypeIndex_ReturnsDifferentIndex_ForDifferentTypes()
        {
            var indexA = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var indexB = TypeManager.GetComponentTypeIndex<TypeTestComponentB>();

            Assert.AreNotEqual(indexA, indexB);
        }

        [Test]
        public void GetComponentTypeIndex_ReturnsNonNegativeIndex()
        {
            var index = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();

            Assert.GreaterOrEqual(index, 0);
        }

        #endregion

        #region GetBufferTypeIndex Tests

        [Test]
        public void GetBufferTypeIndex_ReturnsSameIndex_ForSameType()
        {
            var index1 = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();
            var index2 = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            Assert.AreEqual(index1, index2);
        }

        [Test]
        public void GetBufferTypeIndex_MarksAsBufferType()
        {
            var index = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            Assert.IsTrue(TypeManager.IsBufferType(index));
        }

        [Test]
        public void GetComponentTypeIndex_DoesNotMarkAsBufferType()
        {
            var index = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();

            Assert.IsFalse(TypeManager.IsBufferType(index));
        }

        #endregion

        #region GetTypeSizeByIndex Tests

        [Test]
        public void GetTypeSizeByIndex_ReturnsCorrectSize()
        {
            var index = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();

            var size = TypeManager.GetTypeSizeByIndex(index);

            Assert.AreEqual(sizeof(int), size); // TypeTestComponentA has one int
        }

        [Test]
        public void GetTypeSizeByIndex_ReturnsCorrectSize_ForLargerType()
        {
            var index = TypeManager.GetComponentTypeIndex<TypeTestComponentB>();

            var size = TypeManager.GetTypeSizeByIndex(index);

            Assert.AreEqual(sizeof(float) * 2, size); // TypeTestComponentB has two floats
        }

        [Test]
        public void GetTypeSizeByIndex_ReturnsZero_ForInvalidIndex()
        {
            var size = TypeManager.GetTypeSizeByIndex(-1);
            Assert.AreEqual(0, size);

            var sizeForLargeIndex = TypeManager.GetTypeSizeByIndex(99999);
            Assert.AreEqual(0, sizeForLargeIndex);
        }

        #endregion

        #region IsBufferType Tests

        [Test]
        public void IsBufferType_ReturnsFalse_ForComponentType()
        {
            var index = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();

            Assert.IsFalse(TypeManager.IsBufferType(index));
        }

        [Test]
        public void IsBufferType_ReturnsTrue_ForBufferType()
        {
            var index = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            Assert.IsTrue(TypeManager.IsBufferType(index));
        }

        [Test]
        public void IsBufferType_ReturnsFalse_ForInvalidIndex()
        {
            Assert.IsFalse(TypeManager.IsBufferType(-1));
            Assert.IsFalse(TypeManager.IsBufferType(99999));
        }

        #endregion

        #region Thread Safety Tests

        [Test]
        public void GetComponentTypeIndex_IsThreadSafe()
        {
            const int threadCount = 10;
            const int iterationsPerThread = 100;

            var indices = new int[threadCount * iterationsPerThread];
            var threads = new System.Threading.Thread[threadCount];

            for (var t = 0; t < threadCount; t++)
            {
                var threadIndex = t;
                threads[t] = new System.Threading.Thread(() =>
                {
                    for (var i = 0; i < iterationsPerThread; i++)
                    {
                        indices[threadIndex * iterationsPerThread + i] =
                            TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
                    }
                });
            }

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            // All indices should be the same
            var expectedIndex = indices[0];
            foreach (var index in indices)
            {
                Assert.AreEqual(expectedIndex, index);
            }
        }

        #endregion

        #region Consistency Tests

        [Test]
        public void ComponentAndBuffer_GetDifferentIndices()
        {
            var componentIndex = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var bufferIndex = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            Assert.AreNotEqual(componentIndex, bufferIndex);
        }

        [Test]
        public void MultipleComponents_GetUniqueIndices()
        {
            var indexA = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var indexB = TypeManager.GetComponentTypeIndex<TypeTestComponentB>();
            var indexBuffer = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            // All should be unique
            Assert.AreNotEqual(indexA, indexB);
            Assert.AreNotEqual(indexA, indexBuffer);
            Assert.AreNotEqual(indexB, indexBuffer);
        }

        [Test]
        public void RepeatedCalls_ReturnConsistentIndices()
        {
            // First batch of calls
            var indexA1 = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var indexB1 = TypeManager.GetComponentTypeIndex<TypeTestComponentB>();
            var indexBuf1 = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            // Second batch of calls
            var indexA2 = TypeManager.GetComponentTypeIndex<TypeTestComponentA>();
            var indexB2 = TypeManager.GetComponentTypeIndex<TypeTestComponentB>();
            var indexBuf2 = TypeManager.GetBufferTypeIndex<TypeTestBufferElement>();

            Assert.AreEqual(indexA1, indexA2);
            Assert.AreEqual(indexB1, indexB2);
            Assert.AreEqual(indexBuf1, indexBuf2);
        }

        #endregion
    }
}
