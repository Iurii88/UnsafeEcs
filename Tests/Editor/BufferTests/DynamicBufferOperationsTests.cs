using System;
using NUnit.Framework;
using Unity.Collections;
using UnsafeEcs.Core.Components;
using UnsafeEcs.Core.DynamicBuffers;
using UnsafeEcs.Core.Entities;
using UnsafeEcs.Core.Worlds;
using UnsafeEcs.Tests.Editor.EntityManagerTests;

namespace UnsafeEcs.Tests.Editor.BufferTests
{
    public struct TestBufferElement : IBufferElement
    {
        public int value;
    }

    [TestFixture]
    public class DynamicBufferOperationsTests : UnsafeEcsQueryBaseTest
    {
        private Entity m_entity;
        private DynamicBuffer<TestBufferElement> m_buffer;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            m_entity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(m_entity);
            m_buffer = entityManager.GetBuffer<TestBufferElement>(m_entity);
        }

        #region Add Tests

        [Test]
        public void Add_IncreasesLength()
        {
            Assert.AreEqual(0, m_buffer.Length);

            m_buffer.Add(new TestBufferElement { value = 1 });

            Assert.AreEqual(1, m_buffer.Length);
        }

        [Test]
        public void Add_StoresValue()
        {
            m_buffer.Add(new TestBufferElement { value = 42 });

            Assert.AreEqual(42, m_buffer[0].value);
        }

        [Test]
        public void Add_MultipleElements()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            Assert.AreEqual(3, m_buffer.Length);
            Assert.AreEqual(1, m_buffer[0].value);
            Assert.AreEqual(2, m_buffer[1].value);
            Assert.AreEqual(3, m_buffer[2].value);
        }

        [Test]
        public void Add_ExpandsCapacity_WhenFull()
        {
            var initialCapacity = m_buffer.Capacity;

            // Add more elements than initial capacity
            for (var i = 0; i <= initialCapacity + 10; i++)
            {
                m_buffer.Add(new TestBufferElement { value = i });
            }

            Assert.Greater(m_buffer.Capacity, initialCapacity);
            Assert.AreEqual(initialCapacity + 11, m_buffer.Length);
        }

        #endregion

        #region Reserve Tests

        [Test]
        public void Reserve_IncreasesCapacity()
        {
            var initialCapacity = m_buffer.Capacity;

            m_buffer.Reserve(100);

            Assert.GreaterOrEqual(m_buffer.Capacity, 100);
        }

        [Test]
        public void Reserve_DoesNotChangeLength()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            var lengthBefore = m_buffer.Length;

            m_buffer.Reserve(100);

            Assert.AreEqual(lengthBefore, m_buffer.Length);
        }

        [Test]
        public void Reserve_PreservesExistingData()
        {
            m_buffer.Add(new TestBufferElement { value = 42 });
            m_buffer.Add(new TestBufferElement { value = 100 });

            m_buffer.Reserve(100);

            Assert.AreEqual(42, m_buffer[0].value);
            Assert.AreEqual(100, m_buffer[1].value);
        }

        [Test]
        public void Reserve_SmallerThanCapacity_DoesNothing()
        {
            m_buffer.Reserve(100);
            var capacityAfterFirstReserve = m_buffer.Capacity;

            m_buffer.Reserve(50);

            Assert.AreEqual(capacityAfterFirstReserve, m_buffer.Capacity);
        }

        #endregion

        #region AddRange Tests

        [Test]
        public unsafe void AddRange_AddsMultipleElements()
        {
            var elements = stackalloc TestBufferElement[3];
            elements[0] = new TestBufferElement { value = 10 };
            elements[1] = new TestBufferElement { value = 20 };
            elements[2] = new TestBufferElement { value = 30 };

            m_buffer.AddRange(elements, 3);

            Assert.AreEqual(3, m_buffer.Length);
            Assert.AreEqual(10, m_buffer[0].value);
            Assert.AreEqual(20, m_buffer[1].value);
            Assert.AreEqual(30, m_buffer[2].value);
        }

        [Test]
        public unsafe void AddRange_AppendsToExisting()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            var elements = stackalloc TestBufferElement[2];
            elements[0] = new TestBufferElement { value = 2 };
            elements[1] = new TestBufferElement { value = 3 };

            m_buffer.AddRange(elements, 2);

            Assert.AreEqual(3, m_buffer.Length);
            Assert.AreEqual(1, m_buffer[0].value);
            Assert.AreEqual(2, m_buffer[1].value);
            Assert.AreEqual(3, m_buffer[2].value);
        }

        [Test]
        public unsafe void AddRange_ZeroCount_DoesNothing()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            var elements = stackalloc TestBufferElement[1];
            elements[0] = new TestBufferElement { value = 999 };

            m_buffer.AddRange(elements, 0);

            Assert.AreEqual(1, m_buffer.Length);
        }

        [Test]
        public unsafe void AddRange_ExpandsCapacity_WhenNeeded()
        {
            var elementsToAdd = 100;
            var elements = stackalloc TestBufferElement[elementsToAdd];
            for (var i = 0; i < elementsToAdd; i++)
            {
                elements[i] = new TestBufferElement { value = i };
            }

            m_buffer.AddRange(elements, elementsToAdd);

            Assert.AreEqual(elementsToAdd, m_buffer.Length);
            Assert.GreaterOrEqual(m_buffer.Capacity, elementsToAdd);
        }

        #endregion

        #region RemoveAt Tests

        [Test]
        public void RemoveAt_DecreasesLength()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.RemoveAt(1);

            Assert.AreEqual(2, m_buffer.Length);
        }

        [Test]
        public void RemoveAt_ShiftsElements()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.RemoveAt(0);

            Assert.AreEqual(2, m_buffer[0].value);
            Assert.AreEqual(3, m_buffer[1].value);
        }

        [Test]
        public void RemoveAt_MiddleElement()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.RemoveAt(1);

            Assert.AreEqual(1, m_buffer[0].value);
            Assert.AreEqual(3, m_buffer[1].value);
        }

        [Test]
        public void RemoveAt_LastElement()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.RemoveAt(2);

            Assert.AreEqual(2, m_buffer.Length);
            Assert.AreEqual(1, m_buffer[0].value);
            Assert.AreEqual(2, m_buffer[1].value);
        }

        [Test]
        public void RemoveAt_ThrowsOnNegativeIndex()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            Assert.Throws<IndexOutOfRangeException>(() => m_buffer.RemoveAt(-1));
        }

        [Test]
        public void RemoveAt_ThrowsOnOutOfRangeIndex()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            Assert.Throws<IndexOutOfRangeException>(() => m_buffer.RemoveAt(1));
        }

        #endregion

        #region Clear Tests

        [Test]
        public void Clear_SetsLengthToZero()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });

            m_buffer.Clear();

            Assert.AreEqual(0, m_buffer.Length);
        }

        [Test]
        public void Clear_PreservesCapacity()
        {
            m_buffer.Reserve(100);
            m_buffer.Add(new TestBufferElement { value = 1 });
            var capacityBefore = m_buffer.Capacity;

            m_buffer.Clear();

            Assert.AreEqual(capacityBefore, m_buffer.Capacity);
        }

        #endregion

        #region CopyFrom Array Tests

        [Test]
        public void CopyFrom_Array_CopiesElements()
        {
            var array = new[]
            {
                new TestBufferElement { value = 10 },
                new TestBufferElement { value = 20 },
                new TestBufferElement { value = 30 }
            };

            m_buffer.CopyFrom(array);

            Assert.AreEqual(3, m_buffer.Length);
            Assert.AreEqual(10, m_buffer[0].value);
            Assert.AreEqual(20, m_buffer[1].value);
            Assert.AreEqual(30, m_buffer[2].value);
        }

        [Test]
        public void CopyFrom_Array_OverwritesExisting()
        {
            m_buffer.Add(new TestBufferElement { value = 999 });
            m_buffer.Add(new TestBufferElement { value = 888 });

            var array = new[]
            {
                new TestBufferElement { value = 1 }
            };

            m_buffer.CopyFrom(array);

            Assert.AreEqual(1, m_buffer.Length);
            Assert.AreEqual(1, m_buffer[0].value);
        }

        [Test]
        public void CopyFrom_Array_ThrowsOnNull()
        {
            Assert.Throws<ArgumentNullException>(() => m_buffer.CopyFrom((TestBufferElement[])null));
        }

        [Test]
        public void CopyFrom_EmptyArray_ClearsBuffer()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            m_buffer.CopyFrom(new TestBufferElement[0]);

            Assert.AreEqual(0, m_buffer.Length);
        }

        #endregion

        #region CopyFrom Buffer Tests

        [Test]
        public void CopyFrom_Buffer_CopiesElements()
        {
            // Create another entity with a buffer
            var otherEntity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(otherEntity);
            var otherBuffer = entityManager.GetBuffer<TestBufferElement>(otherEntity);

            otherBuffer.Add(new TestBufferElement { value = 100 });
            otherBuffer.Add(new TestBufferElement { value = 200 });

            m_buffer.CopyFrom(otherBuffer);

            Assert.AreEqual(2, m_buffer.Length);
            Assert.AreEqual(100, m_buffer[0].value);
            Assert.AreEqual(200, m_buffer[1].value);
        }

        [Test]
        public void CopyFrom_Buffer_OverwritesExisting()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            var otherEntity = entityManager.CreateEntity();
            entityManager.AddBuffer<TestBufferElement>(otherEntity);
            var otherBuffer = entityManager.GetBuffer<TestBufferElement>(otherEntity);
            otherBuffer.Add(new TestBufferElement { value = 99 });

            m_buffer.CopyFrom(otherBuffer);

            Assert.AreEqual(1, m_buffer.Length);
            Assert.AreEqual(99, m_buffer[0].value);
        }

        #endregion

        #region ResizeUninitialized Tests

        [Test]
        public void ResizeUninitialized_SetsLength()
        {
            m_buffer.ResizeUninitialized(10);

            Assert.AreEqual(10, m_buffer.Length);
        }

        [Test]
        public void ResizeUninitialized_ExpandsCapacity_WhenNeeded()
        {
            m_buffer.ResizeUninitialized(100);

            Assert.GreaterOrEqual(m_buffer.Capacity, 100);
            Assert.AreEqual(100, m_buffer.Length);
        }

        [Test]
        public void ResizeUninitialized_CanShrink()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.ResizeUninitialized(1);

            Assert.AreEqual(1, m_buffer.Length);
            Assert.AreEqual(1, m_buffer[0].value);
        }

        [Test]
        public void ResizeUninitialized_ToZero_ClearsBuffer()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            m_buffer.ResizeUninitialized(0);

            Assert.AreEqual(0, m_buffer.Length);
        }

        [Test]
        public void ResizeUninitialized_ThrowsOnNegative()
        {
            Assert.Throws<ArgumentException>(() => m_buffer.ResizeUninitialized(-1));
        }

        #endregion

        #region ToNativeArray Tests

        [Test]
        public void ToNativeArray_CreatesCopy()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            var array = m_buffer.ToNativeArray(Allocator.Temp);

            Assert.AreEqual(3, array.Length);
            Assert.AreEqual(1, array[0].value);
            Assert.AreEqual(2, array[1].value);
            Assert.AreEqual(3, array[2].value);

            array.Dispose();
        }

        [Test]
        public void ToNativeArray_ModifyingArray_DoesNotAffectBuffer()
        {
            m_buffer.Add(new TestBufferElement { value = 42 });

            var array = m_buffer.ToNativeArray(Allocator.Temp);
            array[0] = new TestBufferElement { value = 999 };

            Assert.AreEqual(42, m_buffer[0].value);

            array.Dispose();
        }

        [Test]
        public void ToNativeArray_EmptyBuffer_ReturnsEmptyArray()
        {
            var array = m_buffer.ToNativeArray(Allocator.Temp);

            Assert.AreEqual(0, array.Length);

            array.Dispose();
        }

        #endregion

        #region GetUnsafePtr Tests

        [Test]
        public unsafe void GetUnsafePtr_ReturnsValidPointer()
        {
            m_buffer.Add(new TestBufferElement { value = 42 });

            var ptr = m_buffer.GetUnsafePtr();

            Assert.AreEqual(42, ptr[0].value);
        }

        [Test]
        public unsafe void GetUnsafePtr_CanModifyData()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            var ptr = m_buffer.GetUnsafePtr();
            ptr[0] = new TestBufferElement { value = 999 };

            Assert.AreEqual(999, m_buffer[0].value);
        }

        #endregion

        #region Indexer Tests

        [Test]
        public void Indexer_Get_ReturnsCorrectValue()
        {
            m_buffer.Add(new TestBufferElement { value = 42 });

            Assert.AreEqual(42, m_buffer[0].value);
        }

        [Test]
        public void Indexer_Set_UpdatesValue()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            m_buffer[0] = new TestBufferElement { value = 999 };

            Assert.AreEqual(999, m_buffer[0].value);
        }

        [Test]
        public void Indexer_ThrowsOnNegativeIndex()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var _ = m_buffer[-1];
            });
        }

        [Test]
        public void Indexer_ThrowsOnOutOfRangeIndex()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                var _ = m_buffer[1];
            });
        }

        #endregion

        #region Length Property Tests

        [Test]
        public void Length_Set_CanIncrease()
        {
            m_buffer.Length = 10;

            Assert.AreEqual(10, m_buffer.Length);
        }

        [Test]
        public void Length_Set_CanDecrease()
        {
            m_buffer.Add(new TestBufferElement { value = 1 });
            m_buffer.Add(new TestBufferElement { value = 2 });
            m_buffer.Add(new TestBufferElement { value = 3 });

            m_buffer.Length = 1;

            Assert.AreEqual(1, m_buffer.Length);
        }

        [Test]
        public void Length_Set_ThrowsOnNegative()
        {
            Assert.Throws<ArgumentException>(() => m_buffer.Length = -1);
        }

        [Test]
        public void Length_Set_ExpandsCapacity_WhenNeeded()
        {
            m_buffer.Length = 100;

            Assert.GreaterOrEqual(m_buffer.Capacity, 100);
        }

        #endregion
    }
}
