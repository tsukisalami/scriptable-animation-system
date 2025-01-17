using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace Ballistics
{
    public struct Option<T>
    {
        public static Option<T> None => default;
        public static Option<T> Some(T value) => new(value, value is not null);
        public static implicit operator Option<T>(T value) => Some(value);
        public static implicit operator bool(Option<T> opt) => opt.isSome;

        private bool isSome;
        private T value;

        private Option(T value, bool isSome)
        {
            this.value = value;
            this.isSome = isSome;
        }

        public void Set(T value)
        {
            this.value = value;
            isSome = true;
        }

        public bool TryGet(out T value)
        {
            value = this.value;
            return isSome;
        }

        public T ValueOr(T other)
        {
            return isSome ? value : other;
        }

        public void Reset()
        {
            isSome = false;
            value = default;
        }
    }

    public readonly struct ShaderPropertyIdentifier
    {
        public readonly int ShaderPropertyId;

        public ShaderPropertyIdentifier(string name) { ShaderPropertyId = Shader.PropertyToID(name); }
        public static implicit operator ShaderPropertyIdentifier(string name) => new(name);
        public static implicit operator int(ShaderPropertyIdentifier identifier) => identifier.ShaderPropertyId;
    }

    public readonly struct CachedMaterial : System.IDisposable
    {
        public static readonly CachedMaterial Line = new("Hidden/Ballistics/Line");
        public static readonly CachedMaterial Tracer = new("Hidden/Ballistics/Tracer");
        private readonly Material material;

        public CachedMaterial(string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader is not null) {
                material = new Material(shader) {
                    hideFlags = HideFlags.HideAndDontSave
                };
            } else {
                material = null;
                Debug.Log("Could not load shader " + shaderName);
            }
        }

        public Material Get() => material;

        public static implicit operator Material(CachedMaterial cached) => cached.material;

        public void Dispose()
        {
            Object.Destroy(material);
        }
    }

    public static class Util
    {
        public static readonly Bounds MaxBounds = new(Vector3.zero, Vector3.one * 1e12f);

        public static void EnqueueRange(this Queue<int> queue, int from, int to)
        {
            for (int i = from; i < to; i++)
                queue.Enqueue(i);
        }

        public static bool TryPopBack<T>(this List<T> list, out T elem)
        {
            var index = list.Count - 1;
            if (index >= 0) {
                elem = list[index];
                list.RemoveAt(index);
                return true;
            }
            elem = default;
            return false;
        }

        private static readonly string superscripts = @"⁰¹²³⁴⁵⁶⁷⁸⁹";
        public static void AppendSuperscript(this System.Text.StringBuilder sb, int value)
        {
            Stack<byte> digits = new();
            for (; value > 0; value /= 10)
                digits.Push((byte)(value % 10));
            while (digits.TryPop(out var digit))
                sb.Append(superscripts[digit]);
        }

        public static void Each<T>(this IEnumerable<T> ie, System.Action<T, int> action)
        {
            var i = 0;
            foreach (var e in ie) action(e, i++);
        }

        public static System.Text.StringBuilder AppendParenthesized(this System.Text.StringBuilder sb, string content, bool condition)
        {
            return condition ? sb.AppendParenthesized(content) : sb.Append(content);
        }

        public static System.Text.StringBuilder AppendParenthesized(this System.Text.StringBuilder sb, string content)
        {
            return sb.Append("(").Append(content).Append(")");
        }

        public static int ClampRepeating(int value, int length)
        {
            if (value < 0)
                return length - 1;
            if (value >= length)
                return 0;
            return value;
        }
    }

    public struct PlayerLoopEdit : System.IDisposable
    {
        private PlayerLoopSystem root;

        public static PlayerLoopEdit BeginEdit()
        {
            return new PlayerLoopEdit(PlayerLoop.GetCurrentPlayerLoop());
        }

        private PlayerLoopEdit(PlayerLoopSystem root)
        {
            this.root = root;
        }

        public enum InsertMode { Before, After }

        public bool Insert(PlayerLoopSystem playerLoopSystem, System.Type where, InsertMode mode = InsertMode.Before)
        {
            return Insert(ref root, playerLoopSystem, where, mode);
        }

        private bool Insert(ref PlayerLoopSystem system, PlayerLoopSystem playerLoopSystem, System.Type where, InsertMode mode)
        {
            if (system.subSystemList == null)
                return false;
            for (int i = 0; i < system.subSystemList.Length; i++) {
                var subsystem = system.subSystemList[i];
                if (subsystem.type != null && subsystem.type == where) {
                    i = (mode == InsertMode.Before) ? i : i + 1;
                    var newSubsystemList = new PlayerLoopSystem[system.subSystemList.Length + 1];
                    for (int j = 0; j < newSubsystemList.Length; j++)
                        newSubsystemList[j] = (j == i) ? playerLoopSystem : system.subSystemList[(j < i) ? j : j - 1];
                    system.subSystemList = newSubsystemList;
                    return true;
                } else if (Insert(ref system.subSystemList[i], playerLoopSystem, where, mode)) {
                    return true;
                }
            }
            return false;
        }

        public void Dispose()
        {
            Application.quitting += ResetPlayerLoopSystem;
            //DebugPlayerLoopSystem(root);
            PlayerLoop.SetPlayerLoop(root);
        }

        private void ResetPlayerLoopSystem()
        {
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());
        }

        public static void DebugPlayerLoopSystem(PlayerLoopSystem def)
        {
            var sb = new System.Text.StringBuilder();
            RecursivePlayerLoopPrint(def, sb, 0);
            Debug.Log(sb.ToString());
        }

        private static void RecursivePlayerLoopPrint(PlayerLoopSystem sys, System.Text.StringBuilder sb, int depth)
        {
            if (depth == 0) {
                sb.AppendLine("ROOT");
            } else if (sys.type != null) {
                for (int i = 0; i < depth; i++)
                    sb.Append("\t");
                sb.AppendLine(sys.type.Name);
            }
            if (sys.subSystemList == null)
                return;
            foreach (var s in sys.subSystemList)
                RecursivePlayerLoopPrint(s, sb, depth + 1);
        }
    }
}
