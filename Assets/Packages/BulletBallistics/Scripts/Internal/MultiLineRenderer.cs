using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ballistics
{
    /// Efficient drawing of many lines. Batch ~30k lines in one drawcall, without relying on instancing platform support.
    public class MultiLineRenderer : IDisposable
    {
        private static MultiLineRenderer instance;
        public static MultiLineRenderer Instance => instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            instance?.Dispose();
            instance = new();
        }

        public event Action OnBeforeRender;
        private readonly List<LineMesh> LineMeshPool = new();
        private int Count = 0;

        private MultiLineRenderer()
        {
            Application.quitting += Dispose;
            Core.OnBeforeRender += Render;
            Core.OnBeforeAssemblyReload += Dispose;
        }

        public void AddLine(in Line line)
        {
            Count++;
            var batch = Mathf.FloorToInt(Count / (float)LineMesh.Capacity);
            if (batch >= LineMeshPool.Count)
                LineMeshPool.Add(new LineMesh());
            LineMeshPool[batch].AddLine(line);
        }

        private void Render()
        {
            OnBeforeRender?.Invoke();
            for (var i = Mathf.CeilToInt(Count / (float)LineMesh.Capacity) - 1; i >= 0; i--) {
                LineMeshPool[i].Render(0);
                LineMeshPool[i].Clear(); // clear for next frame
            }
            Count = 0;
        }

        public void Dispose()
        {
            Application.quitting -= Dispose;
            Core.OnBeforeAssemblyReload -= Dispose;
            Core.OnBeforeRender -= Render;
            foreach (var mesh in LineMeshPool)
                mesh.Dispose();
            LineMeshPool.Clear();
        }

        private class LineMesh : IDisposable
        {
            public const int Capacity = ushort.MaxValue / 2;
            public const int IndicesPerLine = 2;
            private const MeshUpdateFlags UpdateFlags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds;
            private readonly Mesh mesh = new();
            private NativeArray<Line.Vertex> vertices;
            private int count = 0;

            public LineMesh()
            {
                var descr = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                descr[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3);
                descr[1] = new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float16, 4);
                mesh.SetVertexBufferParams(IndicesPerLine * Capacity, descr);
                descr.Dispose();
                mesh.SetIndexBufferParams(IndicesPerLine * Capacity, IndexFormat.UInt16);
                vertices = new NativeArray<Line.Vertex>(IndicesPerLine * Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                using (var indices = new NativeArray<ushort>(IndicesPerLine * Capacity, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)) {
                    new InitIndicesJob() { Indices = indices }.Schedule(Capacity * IndicesPerLine, 2048).Complete();
                    mesh.SetIndexBufferData(indices, 0, 0, Capacity * IndicesPerLine, UpdateFlags);
                }
                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, IndicesPerLine * Capacity, MeshTopology.Lines), UpdateFlags);
                mesh.bounds = Util.MaxBounds;
            }

            public void Clear() { count = 0; }

            public void AddLine(in Line line)
            {
                var baseIndex = count * IndicesPerLine;
                vertices[baseIndex + 0] = line.From;
                vertices[baseIndex + 1] = line.To;
                count++;
            }

            public void Render(int layer)
            {
                if (count > 0) {
                    mesh.SetVertexBufferData(vertices, 0, 0, count * IndicesPerLine, 0, UpdateFlags);
                    mesh.SetSubMesh(0, new SubMeshDescriptor(0, count * IndicesPerLine, MeshTopology.Lines), UpdateFlags);
                    Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, CachedMaterial.Line, layer, null, 0, null, false, false, false);
                }
            }

            public void Dispose()
            {
                vertices.Dispose();
                UnityEngine.Object.Destroy(mesh);
            }

            [BurstCompile]
            private struct InitIndicesJob : IJobParallelFor
            {
                [WriteOnly] public NativeArray<ushort> Indices;
                public void Execute(int index) { Indices[index] = (ushort)index; }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Line
    {
        [StructLayout(LayoutKind.Sequential)]
        public readonly struct Vertex
        {
            public readonly float3 Position;
            public readonly half4 Color;
            public Vertex(float3 position, Color color)
            {
                Position = position;
                Color = math.half4(new float4(color.r, color.g, color.b, color.a));
            }
        }
        public readonly Vertex From, To;
        public Line(Vertex from, Vertex to)
        {
            From = from;
            To = to;
        }
    }
}

