using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Ballistics
{
    /// structure allowing to split data into a managed and native part
    public interface ISplitable<TManaged, TNative>
        where TManaged : struct
        where TNative : struct
    {
        void ToManaged(ref TManaged managed);
        void ToNative(ref TNative native);
    }

    public class LinkedNativeData<TManaged, TNative> : IDisposable
        where TManaged : struct
        where TNative : struct
    {
        public NativeArray<TNative> Native;
        public TManaged[] Managed;

        /// [0;             ActiveCount - 1]    -> active indices
        /// [ActiveCount;   Capacity - 1]       -> unused indices
        public NativeArray<int> Indices;
        private int activeCount;

        public int FreeCount => Indices.Length - activeCount;
        public int ActiveCount => activeCount;

        public LinkedNativeData(int capacity)
        {
            Managed = new TManaged[capacity];
            Native = new NativeArray<TNative>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            activeCount = 0;
            Indices = new NativeArray<int>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < capacity; i++) {
                Indices[i] = i; // init iota
            }
        }

        public int CopyUsedIndices(ref NativeArray<int> indices)
        {
            NativeArray<int>.Copy(Indices, indices, activeCount);
            return activeCount;
        }

        public int Insert<T>(List<T> list, int startIndex) where T : ISplitable<TManaged, TNative>
        {
            var count = Mathf.Min(FreeCount, list.Count - startIndex);
            for (int i = startIndex; i < startIndex + count; i++) {
                var index = Indices[activeCount];     // next unused index
                activeCount++;
                list[i].ToManaged(ref Managed[index]);
                list[i].ToNative(ref Native.GetRef(index));
            }
            return startIndex + count;
        }

        public void MarkFree(int index)
        {
            for (var i = 0; i < activeCount; i++) { // O(n) but should not be a problem with a few hundred to thousand entries
                if (Indices[i] == index) {
                    Indices[i] = Indices[activeCount - 1]; // move right-most active index to now free spot (i)
                    Indices[activeCount - 1] = index; // move 'index' to new spot in free section
                    activeCount--;
                    return;
                }
            }
            Debug.LogWarningFormat("Ballistics: Double free index {0}.", index);
        }

        public void Dispose()
        {
            Native.Dispose();
            Indices.Dispose();
        }
    }
}