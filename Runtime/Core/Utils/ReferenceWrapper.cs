using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace UnsafeEcs.Core.Utils
{
    public readonly unsafe struct ReferenceWrapper<T> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] public readonly T* ptr;

        public ref T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref *ptr;
        }

        public ReferenceWrapper(ref T value)
        {
            fixed (T* localPtr = &value)
            {
                ptr = localPtr;
            }
        }
    }
}