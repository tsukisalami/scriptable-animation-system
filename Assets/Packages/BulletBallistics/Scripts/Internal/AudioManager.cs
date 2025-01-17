using UnityEngine;

namespace Ballistics
{
    /// Efficient way to play a lot short audio effects
    public static class AudioManager
    {
        private class PooledAudioObject
        {
            public Transform Transform { get; private set; }
            public AudioSource Source { get; private set; }
            public Option<Delay.Handler<Executable<CyclicObjectPool<PooledAudioObject>.Node>>.Handle> Handle;

            public PooledAudioObject()
            {
                var go = new GameObject("PooledAudioSource");
                Object.DontDestroyOnLoad(go);
                Transform = go.transform;
                Source = go.AddComponent<AudioSource>();
            }
        }

        private static Delay.Handler<Executable<CyclicObjectPool<PooledAudioObject>.Node>> executeDelayed;
        private static CyclicObjectPool<PooledAudioObject> pool;
        private static readonly System.Action<CyclicObjectPool<PooledAudioObject>.Node> releaseAction = (node) => {
            node.Data.Handle.Reset();
            pool.Release(node);
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            executeDelayed = new(8);
            pool = new(
                () => new PooledAudioObject(),
                actionOnDestroy: obj => Object.Destroy(obj.Transform.gameObject),
                actionOnTake: obj => {
                    if (obj.Handle.TryGet(out var hnd)) {
                        hnd.Stop();
                        obj.Handle.Reset();
                    }
                    obj.Source.Stop();
                },
                maxSize: 8);
        }

        public static void Play(AudioClip clip, Vector3 position, AudioSourcePreset preset = null)
        {
            var obj = pool.Get();
            if (preset == null)
                AudioSourcePreset.SetDefault(obj.Data.Source);
            else
                preset.InitializeValues(obj.Data.Source);
            obj.Data.Transform.position = position;
            obj.Data.Source.PlayOneShot(clip);
            obj.Data.Handle.Set(executeDelayed.InCancelable(clip.length, releaseAction, obj));
        }
    }
}
