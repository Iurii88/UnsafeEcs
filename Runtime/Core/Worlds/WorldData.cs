using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace UnsafeEcs.Core.Worlds
{
    /// <summary>
    /// A type-based data container for storing single instances of data per type.
    /// Use this to store world-scoped data that can be accessed by any system.
    /// Unlike ManagedStorage which stores multiple instances with references,
    /// WorldData stores exactly one instance per type for direct access.
    /// </summary>
    public class WorldData
    {
        private readonly Dictionary<Type, object> m_dataByType = new();

        /// <summary>
        /// Sets data of type T. Replaces any existing data of the same type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetData<T>(T data)
        {
            m_dataByType[typeof(T)] = data;
        }

        /// <summary>
        /// Gets data of type T. Throws KeyNotFoundException if not found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetData<T>()
        {
            return (T)m_dataByType[typeof(T)];
        }

        /// <summary>
        /// Tries to get data of type T.
        /// </summary>
        /// <returns>True if data exists, false otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetData<T>(out T data)
        {
            if (m_dataByType.TryGetValue(typeof(T), out var obj))
            {
                data = (T)obj;
                return true;
            }

            data = default;
            return false;
        }

        /// <summary>
        /// Checks if data of type T exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasData<T>()
        {
            return m_dataByType.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Removes data of type T.
        /// </summary>
        /// <returns>True if data was removed, false if it didn't exist.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveData<T>()
        {
            return m_dataByType.Remove(typeof(T));
        }

        /// <summary>
        /// Gets data of type T, or creates and stores it using the factory if not found.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrCreateData<T>(Func<T> factory)
        {
            if (TryGetData<T>(out var existing))
                return existing;

            var data = factory();
            SetData(data);
            return data;
        }

        /// <summary>
        /// Gets data of type T, or creates and stores a new instance using default constructor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetOrCreateData<T>() where T : new()
        {
            if (TryGetData<T>(out var existing))
                return existing;

            var data = new T();
            SetData(data);
            return data;
        }

        /// <summary>
        /// Clears all stored data.
        /// </summary>
        public void Clear()
        {
            m_dataByType.Clear();
        }
    }
}
