using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;

namespace Ballistics
{
    /// Proxy bullet that records the bullet path for debugging in the editor
    public class BulletDebugProxy : IVisualBullet
    {
        private static ObjectPool<BulletDebugProxy> debugProxyPool;
        private static HashSet<BulletDebugProxy> activeDebugProxies;
        private static Delay.Handler<Executable<BulletDebugProxy>> executeDelayed;
        private static readonly System.Action<BulletDebugProxy> releaseAction = AddToCache;
        private const int LineCapacity = 90;
        private const float DestroyDelay = .9f;
        public static IVisualBullet Wrap(IVisualBullet bullet)
        {
            var debugProxy = debugProxyPool.Get();
            debugProxy.Bullet = bullet;
            activeDebugProxies.Add(debugProxy);
            return debugProxy;
        }

        private static void AddToCache(BulletDebugProxy proxy)
        {
            activeDebugProxies.Remove(proxy);
            debugProxyPool.Release(proxy);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            debugProxyPool = new(() => new(), actionOnGet: obj => obj.Clear(), defaultCapacity: 0, maxSize: 1000);
            activeDebugProxies = new();
            executeDelayed = new(0);

            Core.OnBeforeSceneChange += OnBeforeSceneChange;
            MultiLineRenderer.Instance.OnBeforeRender += RenderDebugBullets;
            Application.quitting += CleanUp;
        }

        private static void CleanUp()
        {
            Application.quitting -= CleanUp;
            Core.OnBeforeSceneChange -= OnBeforeSceneChange;
            MultiLineRenderer.Instance.OnBeforeRender -= RenderDebugBullets;
            executeDelayed.StopAll();
            debugProxyPool.Clear();
            activeDebugProxies.Clear();
        }

        private static void OnBeforeSceneChange()
        {
            executeDelayed.StopAll();
            foreach (var proxy in activeDebugProxies)
                debugProxyPool.Release(proxy);
            activeDebugProxies.Clear();
        }

        private static void RenderDebugBullets()
        {
            foreach (var proxy in activeDebugProxies)
                proxy.OnRender();
        }

        public IVisualBullet Bullet;
        private readonly Line[] LineBuffer = new Line[LineCapacity];
        private Line.Vertex Last;
        private int Count = 0;
        private int NextIndex = 0;

        public void OnRender()
        {
            for (var i = Count - 1; i >= 0; i--)
                MultiLineRenderer.Instance.AddLine(LineBuffer[i]);
        }

        public void InitializeBullet(in BulletPose pose, in float3 visualOffset)
        {
            Bullet?.InitializeBullet(pose, visualOffset);
            Last = new Line.Vertex(pose.Position, Color.white);
        }

        public void UpdateBullet(in BulletPose pose)
        {
            Bullet?.UpdateBullet(pose);
            var hue = math.frac(math.length(pose.Velocity) * .001f);
            var vertex = new Line.Vertex(pose.Position, Color.HSVToRGB(hue, 1, 1));
            LineBuffer[NextIndex] = new Line(Last, vertex);
            Last = vertex;
            NextIndex = (NextIndex + 1) % LineCapacity;
            Count = math.min(Count + 1, LineCapacity);
        }

        public void DestroyBullet()
        {
            Bullet?.DestroyBullet();
            Bullet = null;
            executeDelayed.In(DestroyDelay, new(releaseAction, this));
        }

        public void Clear()
        {
            Count = 0;
            NextIndex = 0;
        }
    }
}

