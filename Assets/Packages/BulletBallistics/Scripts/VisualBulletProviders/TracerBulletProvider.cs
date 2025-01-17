using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Ballistics
{
    /// efficient solution for drawing many bullet tracers in a single draw call
    [CreateAssetMenu(fileName = "Tracer Provider", menuName = "Ballistics/Visual Bullet Providers/Tracer Bullet Provider", order = 0)]
    public class TracerBulletProvider : VisualBulletProviderObject, IDisposable
    {
        [Layer] public int Layer;
        [Range(.0001f, .25f)] public float Width = .1f;
        public Color Color = Color.red;
        public int InitialPoolSize = 0;
        public int MaxPoolSize = 1000;
        private readonly List<Instance> instances = new();
        private UnityEngine.Pool.ObjectPool<Instance> Pool;
        private readonly List<TracerMesh> TracerMeshPool = new();
        private MaterialPropertyBlock TracerProperties;
        private static readonly ShaderPropertyIdentifier TracerWidthProperty = "_TracerWidth";
        private static readonly ShaderPropertyIdentifier TracerColorProperty = "_TracerColor";

        public override IVisualBullet GetVisualBullet()
        {
            return Pool.Get();
        }

        public override void Initialize()
        {
            Pool = new(() => new Instance(this),
                        (obj) => instances.Add(obj),
                        (obj) => instances.Remove(obj),
                        null,
                        true,
                        InitialPoolSize,
                        MaxPoolSize);
            TracerProperties = new();
            Core.OnBeforeRender += OnBeforeRender;
            Application.quitting += Dispose;
            Core.OnBeforeAssemblyReload += Dispose;
        }

        private void OnBeforeRender()
        {
            if (instances.Count == 0)
                return;
            var batchCount = Mathf.CeilToInt(instances.Count / (float)TracerMesh.Capacity);
            for (var count = TracerMeshPool.Count; count < batchCount; count++)
                TracerMeshPool.Add(new TracerMesh());
            for (var poolSize = TracerMeshPool.Count; poolSize > batchCount + 1; poolSize--) { // drop all but 1 spare
                var end = poolSize - 1;
                TracerMeshPool[end].Dispose();
                TracerMeshPool.RemoveAt(end);
            }

            TracerProperties.SetFloat(TracerWidthProperty, Width);
            TracerProperties.SetColor(TracerColorProperty, Color);
            for (var i = batchCount - 1; i >= 0; i--) { // rebuild and render tracers
                var tracers = TracerMeshPool[i];
                tracers.Clear();
                var from = Mathf.Min(instances.Count - 1, (i + 1) * TracerMesh.Capacity - 1);
                var to = i * TracerMesh.Capacity;
                // TODO: move this to a job?
                for (var n = from; n >= to; n--) {
                    var instance = instances[n];
                    tracers.AddTracer(instance.lastPosition2, instance.lastPosition, instance.currentPosition);
                }
                tracers.Render(TracerProperties, Layer);
            }
        }

        public void Dispose()
        {
            Core.OnBeforeAssemblyReload -= Dispose;
            Application.quitting -= Dispose;
            Core.OnBeforeRender -= OnBeforeRender;
            Pool.Clear();
            instances.Clear();
            foreach (var mesh in TracerMeshPool)
                mesh.Dispose();
            TracerMeshPool.Clear();
        }

        public class TracerMesh : IDisposable
        {
            private const ushort VertexCount = 6;
            public const int Capacity = ushort.MaxValue / VertexCount;
            private const ushort IndexCount = 12;
            private const MeshUpdateFlags UpdateFlags = MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontResetBoneBounds | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds;

            [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
            public struct Vertex
            {
                public float4 Position, Direction;
                public Vertex(float4 position, float4 direction) { Position = position; Direction = direction; }
            }

            private readonly Mesh mesh = new();
            private NativeArray<Vertex> vertices;
            private int count = 0;

            public TracerMesh()
            {
                var descr = new NativeArray<VertexAttributeDescriptor>(2, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
                descr[0] = new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 4);
                descr[1] = new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 4);
                mesh.SetVertexBufferParams(VertexCount * Capacity, descr);
                descr.Dispose();
                mesh.SetIndexBufferParams(IndexCount * Capacity, IndexFormat.UInt16);
                vertices = new NativeArray<Vertex>(VertexCount * Capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                using (var indices = new NativeArray<ushort>(IndexCount * Capacity, Allocator.TempJob, NativeArrayOptions.UninitializedMemory)) {
                    // TODO: cache this?
                    new InitIndicesJob() { Indices = indices, }.Schedule(Capacity, 512).Complete();
                    mesh.SetIndexBufferData(indices, 0, 0, indices.Length, UpdateFlags);
                }
                mesh.subMeshCount = 1;
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, IndexCount * Capacity), UpdateFlags);
                mesh.bounds = Util.MaxBounds;
            }

            public void Clear()
            {
                count = 0;
            }

            public void AddTracer(float3 a, float3 b, float3 c)
            {
                var baseVertex = count * VertexCount;
                var dir = c - b;
                var dir2 = b - a;
                var l1 = math.length(dir);
                var d = l1 / (l1 + math.length(dir2));
                vertices[baseVertex + 0] = new Vertex(new float4(a, -1), new float4(dir2, 1));
                vertices[baseVertex + 1] = new Vertex(new float4(a, 1), new float4(dir2, 1));
                vertices[baseVertex + 2] = new Vertex(new float4(b, -1), new float4((dir + dir2) * .5f, d));
                vertices[baseVertex + 3] = new Vertex(new float4(b, 1), new float4((dir + dir2) * .5f, d));
                vertices[baseVertex + 4] = new Vertex(new float4(c, -1), new float4(dir, 0));
                vertices[baseVertex + 5] = new Vertex(new float4(c, 1), new float4(dir, 0));
                count += 1;
            }

            public void Render(MaterialPropertyBlock properties, int layer)
            {
                mesh.SetVertexBufferData(vertices, 0, 0, count * VertexCount, 0, UpdateFlags);
                mesh.SetSubMesh(0, new SubMeshDescriptor(0, count * IndexCount), UpdateFlags);
                Graphics.DrawMesh(mesh, Vector3.zero, Quaternion.identity, CachedMaterial.Tracer, layer, null, 0, properties, false, false, false);
            }

            public void Dispose()
            {
                vertices.Dispose();
                Destroy(mesh);
            }

            [BurstCompile]
            struct InitIndicesJob : IJobParallelFor
            {
                [WriteOnly, NativeDisableParallelForRestriction] public NativeArray<ushort> Indices;
                public void Execute(int index)
                {
                    var baseVertex = index * VertexCount;
                    var baseIndex = index * IndexCount;
                    Indices[baseIndex + 0] = (ushort)(baseVertex + 0);
                    Indices[baseIndex + 1] = (ushort)(baseVertex + 1);
                    Indices[baseIndex + 2] = (ushort)(baseVertex + 2);
                    Indices[baseIndex + 3] = (ushort)(baseVertex + 3);
                    Indices[baseIndex + 4] = (ushort)(baseVertex + 2);
                    Indices[baseIndex + 5] = (ushort)(baseVertex + 1);
                    Indices[baseIndex + 6] = (ushort)(baseVertex + 2);
                    Indices[baseIndex + 7] = (ushort)(baseVertex + 3);
                    Indices[baseIndex + 8] = (ushort)(baseVertex + 4);
                    Indices[baseIndex + 9] = (ushort)(baseVertex + 5);
                    Indices[baseIndex + 10] = (ushort)(baseVertex + 4);
                    Indices[baseIndex + 11] = (ushort)(baseVertex + 3);
                }
            }
        }

        public class Instance : IVisualBullet
        {
            public float3 currentPosition, lastPosition, lastPosition2;
            public float4 offset;
            private readonly Action ReleaseAction;

            public Instance(TracerBulletProvider handler)
            {
                ReleaseAction = () => handler.Pool.Release(this);
            }

            public void InitializeBullet(in BulletPose pose, in float3 visualOffset)
            {
                currentPosition = pose.Position + visualOffset;
                lastPosition = currentPosition;
                lastPosition2 = currentPosition;
                offset = new float4(visualOffset, 1);
            }

            public void UpdateBullet(in BulletPose pose)
            {
                lastPosition2 = lastPosition;
                lastPosition = currentPosition;
                VisualBulletUtil.UpdateVisualOffset(
                    ref offset,
                    pose,
                    Core.CurrentTimeStep,
                    Core.Environment.VisualToPhysicalDistanceInv,
                    out currentPosition);
            }

            public void DestroyBullet()
            {
                // release next frame
                Delay.Execute.In(Time.unscaledDeltaTime + .001f, ReleaseAction);
            }
        }
    }
}