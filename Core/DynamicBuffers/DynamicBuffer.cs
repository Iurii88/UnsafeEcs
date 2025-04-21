using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.DynamicBuffers
{
public readonly unsafe struct DynamicBuffer<T> where T : unmanaged, IBufferElement
    {
        private readonly BufferHeader* m_buffer;

        // Constructor used internally by the ECS system
        internal DynamicBuffer(BufferHeader* buffer)
        {
            m_buffer = buffer;
        }

        // Length of the buffer (number of elements)
        public int Length
        {
            get => m_buffer->length;
            set
            {
                if (value < 0)
                    throw new ArgumentException("Length cannot be negative");

                if (value > m_buffer->capacity)
                    Reserve(math.max(value, m_buffer->capacity * 2));

                m_buffer->length = value;
            }
        }

        // Capacity of the buffer (maximum number of elements without resizing)
        public int Capacity => m_buffer->capacity;

        // Element access via indexer
        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (index < 0 || index >= m_buffer->length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range of buffer length {m_buffer->length}");

                return ref UnsafeUtility.ArrayElementAsRef<T>(m_buffer->pointer, index);
            }
        }

        // Reserve capacity for at least the specified number of elements
        public void Reserve(int capacity)
        {
            if (capacity <= m_buffer->capacity)
                return;

            var newSize = UnsafeUtility.SizeOf<T>() * capacity;
            var newBuffer = (byte*)UnsafeUtility.Malloc(newSize, UnsafeUtility.AlignOf<T>(), Allocator.Persistent);

            // Copy existing elements if any
            if (m_buffer->length > 0 && m_buffer->pointer != null)
            {
                UnsafeUtility.MemCpy(newBuffer, m_buffer->pointer, UnsafeUtility.SizeOf<T>() * m_buffer->length);
                UnsafeUtility.Free(m_buffer->pointer, Allocator.Persistent);
            }

            m_buffer->pointer = newBuffer;
            m_buffer->capacity = capacity;
        }

        // Add an element to the buffer
        public void Add(T element)
        {
            if (m_buffer->length == m_buffer->capacity)
                Reserve(math.max(1, m_buffer->capacity * 2));

            UnsafeUtility.WriteArrayElement(m_buffer->pointer, m_buffer->length, element);
            m_buffer->length++;
        }

        // Add a range of elements
        public void AddRange(T* elements, int count)
        {
            if (count <= 0)
                return;

            var newLength = m_buffer->length + count;
            if (newLength > m_buffer->capacity)
                Reserve(math.max(newLength, m_buffer->capacity * 2));

            UnsafeUtility.MemCpy(m_buffer->pointer + m_buffer->length * UnsafeUtility.SizeOf<T>(), 
                elements, UnsafeUtility.SizeOf<T>() * count);
            
            m_buffer->length = newLength;
        }

        // Remove element at specified index
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= m_buffer->length)
                throw new IndexOutOfRangeException($"Index {index} is out of range of buffer length {m_buffer->length}");

            var elementSize = UnsafeUtility.SizeOf<T>();
            var dst = m_buffer->pointer + index * elementSize;
            var src = m_buffer->pointer + (index + 1) * elementSize;
            var bytesToMove = (m_buffer->length - index - 1) * elementSize;
            
            if (bytesToMove > 0)
                UnsafeUtility.MemMove(dst, src, bytesToMove);
            
            m_buffer->length--;
        }

        // Clear the buffer (resets length to 0 but keeps capacity)
        public void Clear()
        {
            m_buffer->length = 0;
        }

        // Returns a direct pointer to the buffer's data
        public T* GetUnsafePtr()
        {
            return (T*)m_buffer->pointer;
        }

        // Resize the buffer to the specified length
        public void ResizeUninitialized(int length)
        {
            if (length < 0)
                throw new ArgumentException("Length cannot be negative");

            if (length > m_buffer->capacity)
                Reserve(math.max(length, m_buffer->capacity * 2));

            m_buffer->length = length;
        }

        // Copy contents from an array
        public void CopyFrom(T[] array)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            ResizeUninitialized(array.Length);
            
            fixed (T* source = array)
            {
                UnsafeUtility.MemCpy(m_buffer->pointer, source, UnsafeUtility.SizeOf<T>() * array.Length);
            }
        }

        // Copy contents from another buffer
        public void CopyFrom(DynamicBuffer<T> buffer)
        {
            ResizeUninitialized(buffer.Length);
            UnsafeUtility.MemCpy(m_buffer->pointer, buffer.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * buffer.Length);
        }

        // Convert to NativeArray (creates a copy)
        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            var array = new NativeArray<T>(m_buffer->length, allocator, NativeArrayOptions.UninitializedMemory);
            UnsafeUtility.MemCpy(array.GetUnsafePtr(), m_buffer->pointer, UnsafeUtility.SizeOf<T>() * m_buffer->length);
            return array;
        }
    }
}