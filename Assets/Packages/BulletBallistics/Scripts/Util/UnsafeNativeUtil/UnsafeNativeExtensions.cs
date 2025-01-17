using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Ballistics
{
    public static class UnsafeNativeExtensions
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static ref T GetRef<T>(this NativeArray<T> array, int index) where T : struct
        {
            unsafe {
                return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
            }
        }
    }
}