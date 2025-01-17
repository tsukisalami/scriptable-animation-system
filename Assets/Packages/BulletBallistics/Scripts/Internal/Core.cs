using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// Entry point for simulating bullets.
    /// Holds the configuration of the ballistic environment.
    /// Synchronizes bullets with the playerloop.
    public static class Core
    {
        private static List<BulletInstance> queuedBullets;
        private static BulletUpdateLoopHandler bulletUpdateLoop;
        public static BulletUpdateLoopHandler BulletUpdateLoop => bulletUpdateLoop;
        public static float CurrentTimeStep => bulletUpdateLoop.TimeStep;
        public static int ActiveBullets { get => bulletUpdateLoop.ActiveCount(); }
        public static void AddBullet(in BulletInstance bullet)
        {
            queuedBullets.Add(bullet);
        }

        public static bool EnableDebug = false;
        public static BallisticSettings BallisticSettings;
        public static Vector3 WindVelocity = Vector3.zero;
        public static Vector3 Gravity = new(0, -9.81f, 0);
        public static Environment Environment { get; private set; }
        public static void UpdateEnvironment() => Environment = new Environment(BallisticSettings, WindVelocity, Gravity);
        public static IImpactHandler GlobalImpactHandler = null;

        public static event Action OnBeginUpdate = delegate { };
        public static event Action OnUpdateCompleted = delegate { };
        public static event Action OnBeforeRender = delegate { };
        public static event Action OnBeforeSceneChange = delegate { };
        public static event Action OnBeforeAssemblyReload = delegate { };

        internal static void InitializeUpdate()
        {
            UpdateEnvironment();
            OnBeginUpdate.Invoke();
            bulletUpdateLoop.Consume(ref queuedBullets);
            bulletUpdateLoop.Environment = Environment;
            bulletUpdateLoop.GlobalImpactHandler = GlobalImpactHandler;
            bulletUpdateLoop.InitializeUpdate(Time.deltaTime);
        }

        // TODO: this is not called yet
        internal static void FixedUpdate()
        {
            bulletUpdateLoop.DisposeUnusedBatches();
        }

        internal static void Update()
        {
            bulletUpdateLoop.Update();
        }

        internal static void CompleteUpdates()
        {
            // ensure all bullets are fully processed
            if (bulletUpdateLoop.IsActive()) {
                do {
                    bulletUpdateLoop.CompleteStep();
                } while (bulletUpdateLoop.Update());
            }
            OnUpdateCompleted.Invoke();
            OnBeforeRender.Invoke();
        }

        public static void PrepareForSceneChange()
        {
            bulletUpdateLoop.Reset();
            OnBeforeSceneChange.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            queuedBullets = new();
            bulletUpdateLoop = new();
            GlobalImpactHandler = null;

            Application.quitting += Dispose;
#if UNITY_EDITOR
            // cleanup before reloading assemblies in the editor
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += CleanupBeforeAssemblyReload;
            UnityEditor.SceneView.beforeSceneGui += BeforeSceneViewRender;
#endif
        }

        private static void Dispose()
        {
            Application.quitting -= Dispose;
            bulletUpdateLoop.Dispose();
#if UNITY_EDITOR
            UnityEditor.SceneView.beforeSceneGui -= BeforeSceneViewRender;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= CleanupBeforeAssemblyReload;
#endif
        }

#if UNITY_EDITOR
        internal static void CleanupBeforeAssemblyReload()
        {
            OnBeforeAssemblyReload.Invoke();
            PrepareForSceneChange();
            bulletUpdateLoop.Dispose();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        static void DidReload()
        {
            if (UnityEditor.EditorApplication.isPlaying)
                Initialize();
        }

        internal static void BeforeSceneViewRender(UnityEditor.SceneView _)
        {
            if (UnityEditor.EditorApplication.isPaused)
                OnBeforeRender.Invoke();
        }
#endif
    }
}