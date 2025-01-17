using System;
using UnityEngine;

namespace Ballistics
{
    public class CyclicObjectPool<T> : IDisposable where T : class
    {
        public class Node
        {
            public Node Previous;
            public Node Next;
            public T Data;

            public Node(T data)
            {
                Data = data;
                ResetLinks();
            }

            public void ResetLinks()
            {
                Previous = this;
                Next = this;
            }

            public bool TryGetNext(out Node next)
            {
                next = Next;
                return Next != this;
            }

            public bool TryGetPrevious(out Node previous)
            {
                previous = Previous;
                return Previous != this;
            }
        }

        /// double linked queue, to allow for O(1) enqueue, dequeue, and node removal (worse memory layout than basic Queue though)
        public class LinkedQueue
        {
            public int Count { get; private set; } = 0;

            private Option<Node> first;

            public void Enqueue(Node node)
            {
                if (first.TryGet(out var firstNode)) {
                    var last = firstNode.Previous;
                    // connect old and new last
                    last.Next = node;
                    node.Previous = last;
                    // connect last and first
                    node.Next = firstNode;
                    firstNode.Previous = node;
                } else {
                    node.ResetLinks();
                    first.Set(node);
                }
                Count++;
            }

            public bool TryDequeue(out Node node)
            {
                if (first.TryGet(out node)) {
                    if (node.TryGetNext(out var next)) {
                        if (node.TryGetPrevious(out var previous) && next != previous) {
                            // close loop
                            next.Previous = previous;
                            previous.Next = next;
                        } else {
                            next.ResetLinks();
                        }
                        first.Set(next);
                    } else {
                        first.Reset();
                    }

                    node.ResetLinks();
                    Count--;
                    return true;
                }
                return false;
            }

            public void Remove(Node node)
            {
                if (node.TryGetPrevious(out var previous) && node.TryGetNext(out var next)) {
                    // close loop
                    previous.Next = next;
                    next.Previous = previous;
                }

                if (first.TryGet(out var firstNode) && firstNode == node) {
                    if (node.TryGetNext(out var newFirst)) {
                        first.Set(newFirst);
                    } else {
                        first.Reset();
                    }
                }
                node.ResetLinks();
                Count--;
            }
        }

        private readonly LinkedQueue pool = new();
        private readonly LinkedQueue used = new();

        private Func<T> createFunc;
        private Option<Action<T>> actionOnGet;
        private Option<Action<T>> actionOnTake;
        private Option<Action<T>> actionOnRelease;
        private Option<Action<T>> actionOnDestroy;

        public int MaxSize { get; private set; }
        public int Count { get => pool.Count + used.Count; }

        public CyclicObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnTake = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null, int defaultCapacity = 10, int maxSize = 100)
        {
            MaxSize = maxSize;
            this.createFunc = createFunc;
            this.actionOnGet = actionOnGet;
            this.actionOnTake = actionOnTake;
            this.actionOnRelease = actionOnRelease;
            this.actionOnDestroy = actionOnDestroy;
            defaultCapacity = Mathf.Min(defaultCapacity, maxSize);
            for (int i = 0; i < defaultCapacity; i++)
                pool.Enqueue(new(createFunc.Invoke()));
        }

        private Node CreateOrTake()
        {
            if (pool.TryDequeue(out var node))
                return node;
            if (Count >= MaxSize && used.TryDequeue(out node)) { // try take oldest in use, or create new
                if (actionOnTake.TryGet(out var onTake))
                    onTake.Invoke(node.Data);
                return node;
            } else {
                return new(createFunc.Invoke());
            }
        }

        public Node Get()
        {
            var node = CreateOrTake();
            if (actionOnGet.TryGet(out var onGet))
                onGet.Invoke(node.Data);
            used.Enqueue(node);
            return node;
        }

        public void Release(Node node)
        {
            used.Remove(node);
            pool.Enqueue(node);
            if (actionOnRelease.TryGet(out var onRelease))
                onRelease.Invoke(node.Data);
        }

        public void RecallAll()
        {
            while (used.TryDequeue(out var node))
                pool.Enqueue(node);
        }

        public void Dispose()
        {
            if (actionOnDestroy.TryGet(out var onDestroy)) {
                while (used.TryDequeue(out var node))
                    onDestroy.Invoke(node.Data);
                while (pool.TryDequeue(out var node))
                    onDestroy.Invoke(node.Data);
            }
        }
    }
}