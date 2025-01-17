using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// delayed execution without relying on coroutines, invoke, or async

    public interface IUpdatable { public void Update(float deltaTime); }
    public interface IExecutable { public void Execute(); }

    public readonly struct Executable<T> : IExecutable
    {
        public readonly Action<T> Action;
        public readonly T Context;
        public Executable(Action<T> action, T context)
        {
            Action = action;
            Context = context;
        }
        public void Execute() => Action.Invoke(Context);
    }

    public readonly struct Executable : IExecutable
    {
        public readonly Action Action;
        public Executable(Action action)
        {
            Action = action;
        }
        public void Execute() => Action.Invoke();
    }


    public static class Delay
    {
        private static List<IUpdatable> updatables;
        private static Handler<Executable> execute;
        private static Handler<Executable<GameObject>> executeCtxGO;

        public static Handler<Executable> Execute => execute;
        public static Handler<Executable<GameObject>> ExecuteCtxGO => executeCtxGO;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            updatables = new List<IUpdatable>();
            execute = new(8);
            executeCtxGO = new(8);
        }

        private static void Register(IUpdatable updatable) => updatables.Add(updatable);
        public static void Update() => Update(Time.deltaTime);
        public static void Update(float deltaTime)
        {
            for (int i = 0; i < updatables.Count; i++)
                updatables[i].Update(deltaTime);
        }

        public class Handler<T> : IUpdatable where T : struct, IExecutable
        {
            public readonly struct Handle
            {
                private readonly Handler<T> handler;
                private readonly uint id;

                public Handle(Handler<T> handler, uint id)
                {
                    this.id = id;
                    this.handler = handler;
                }

                public void Stop()
                {
                    handler.Stop(id);
                }
            }

            private struct Entry
            {
                public T Action;
                public float Time;
                public uint Id;

                public Entry(T action, float time, uint id)
                {
                    Action = action;
                    Time = time;
                    Id = id;
                }
            }

            private uint id = 0;
            private int size = 0;
            private Entry[] actionsQueue;

            public Handler(int initialCapacity)
            {
                actionsQueue = new Entry[initialCapacity];
                Register(this);
            }

            public Handle InCancelable(float seconds, in T action) => new(this, Add(seconds, action));
            public void In(float seconds, in T action) => Add(seconds, action);

            public void Update(float deltaTime)
            {
                int offset = 0;
                for (int i = 0; i < size; i++) { // O(size)
                    ref var handle = ref actionsQueue[i];
                    if (handle.Time == float.NaN) {
                        offset++; // disable stopped handles
                    } else {
                        handle.Time -= deltaTime;
                        if (handle.Time <= 0) {
                            handle.Action.Execute();
                            offset++;
                        } else if (offset > 0) {
                            // compact; handle ids will always remain in ascending order
                            actionsQueue[i - offset] = handle;
                        }
                    }
                }
                size -= offset;
            }

            public void SetCapacity(int capacity)
            {
                Array.Resize(ref actionsQueue, Mathf.Max(size, capacity));
            }

            private uint Add(float seconds, in T action)
            {
                if (size >= actionsQueue.Length)
                    SetCapacity((size + 1) * 2);
                ref var handle = ref actionsQueue[size++];
                handle.Action = action;
                handle.Time = seconds;
                handle.Id = id;
                return id++;
            }

            private void Stop(uint nodeId)
            {
                // binary search id -> O(log n)
                // TODO: linear search is probably fine lol
                var min = 0;
                var max = size;
                while (min < max) {
                    var mid = (max - min) / 2 + min;
                    ref var elem = ref actionsQueue[mid];
                    if (elem.Id < nodeId) {
                        min = mid + 1;
                    } else if (elem.Id > nodeId) {
                        max = mid;
                    } else {
                        elem.Time = float.NaN;  // mark stopped; do not remove because of O(n) array copy
                        return;
                    }
                }
                Debug.LogWarning($"Invalid Handle! Already stopped? {nodeId} {min} {max}");
            }

            public void StopAll()
            {
                size = 0;
                id = 0;
            }
        }
    }

    public static class DelayHandlerExtensions
    {
        public static void In(this Delay.Handler<Executable> handler, float seconds, Action action) => handler.In(seconds, new(action));
        public static Delay.Handler<Executable>.Handle InCancelable(this Delay.Handler<Executable> handler, float seconds, Action action) => handler.InCancelable(seconds, new(action));
        public static void In<Ctx>(this Delay.Handler<Executable<Ctx>> handler, float seconds, Action<Ctx> action, Ctx context) => handler.In(seconds, new(action, context));
        public static Delay.Handler<Executable<Ctx>>.Handle InCancelable<Ctx>(this Delay.Handler<Executable<Ctx>> handler, float seconds, Action<Ctx> action, Ctx context) => handler.InCancelable(seconds, new(action, context));
    }
}