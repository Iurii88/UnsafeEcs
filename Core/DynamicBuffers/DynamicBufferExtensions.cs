using System;
using UnsafeEcs.Core.Components;

namespace UnsafeEcs.Core.DynamicBuffers
{
    public static class DynamicBufferExtensions
    {
        // Find first element matching a predicate
        public static bool TryFind<T>(this DynamicBuffer<T> buffer, Predicate<T> predicate, out T element, out int index)
            where T : unmanaged, IBufferElement
        {
            element = default;
            index = -1;

            for (var i = 0; i < buffer.Length; i++)
            {
                if (predicate(buffer[i]))
                {
                    element = buffer[i];
                    index = i;
                    return true;
                }
            }

            return false;
        }

        // Find first element of the specified value
        public static bool Contains<T>(this DynamicBuffer<T> buffer, T value)
            where T : unmanaged, IBufferElement, IEquatable<T>
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Equals(value))
                    return true;
            }

            return false;
        }

        // Insert an element at specified index
        public static void Insert<T>(this DynamicBuffer<T> buffer, int index, T element)
            where T : unmanaged, IBufferElement
        {
            if (index < 0 || index > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(index));

            var length = buffer.Length;
            buffer.ResizeUninitialized(length + 1);

            // Make room by shifting elements
            for (var i = length - 1; i >= index; i--)
                buffer[i + 1] = buffer[i];

            buffer[index] = element;
        }

        // Remove all elements matching a predicate
        public static int RemoveAll<T>(this DynamicBuffer<T> buffer, Predicate<T> match)
            where T : unmanaged, IBufferElement
        {
            var count = 0;
            var writeIndex = 0;

            // Read through buffer, only writing elements that don't match
            for (var readIndex = 0; readIndex < buffer.Length; readIndex++)
            {
                var item = buffer[readIndex];
                if (!match(item))
                {
                    if (writeIndex != readIndex)
                        buffer[writeIndex] = item;

                    writeIndex++;
                }
                else
                {
                    count++;
                }
            }

            // Resize buffer to new size
            if (count > 0)
                buffer.ResizeUninitialized(buffer.Length - count);

            return count;
        }

        // Find index of element
        public static int IndexOf<T>(this DynamicBuffer<T> buffer, T element)
            where T : unmanaged, IBufferElement, IEquatable<T>
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                if (buffer[i].Equals(element))
                    return i;
            }

            return -1;
        }

        // Reverse the buffer
        public static void Reverse<T>(this DynamicBuffer<T> buffer)
            where T : unmanaged, IBufferElement
        {
            var left = 0;
            var right = buffer.Length - 1;

            while (left < right)
            {
                // Swap elements
                (buffer[left], buffer[right]) = (buffer[right], buffer[left]);

                left++;
                right--;
            }
        }

        // Sort the buffer based on a comparison function
        public static void Sort<T>(this DynamicBuffer<T> buffer, Comparison<T> comparison)
            where T : unmanaged, IBufferElement
        {
            // Simple implementation of QuickSort
            QuickSort(buffer, 0, buffer.Length - 1, comparison);
        }

        private static void QuickSort<T>(DynamicBuffer<T> buffer, int left, int right, Comparison<T> comparison) where T : unmanaged, IBufferElement
        {
            while (true)
            {
                if (left < right)
                {
                    var pivotIndex = Partition(buffer, left, right, comparison);
                    QuickSort(buffer, left, pivotIndex - 1, comparison);
                    left = pivotIndex + 1;
                    continue;
                }

                break;
            }
        }

        private static int Partition<T>(DynamicBuffer<T> buffer, int left, int right, Comparison<T> comparison)
            where T : unmanaged, IBufferElement
        {
            var pivot = buffer[right];
            var i = left - 1;

            for (var j = left; j < right; j++)
            {
                if (comparison(buffer[j], pivot) <= 0)
                {
                    i++;
                    Swap(buffer, i, j);
                }
            }

            Swap(buffer, i + 1, right);
            return i + 1;
        }

        private static void Swap<T>(DynamicBuffer<T> buffer, int i, int j)
            where T : unmanaged, IBufferElement
        {
            (buffer[i], buffer[j]) = (buffer[j], buffer[i]);
        }
    }
}