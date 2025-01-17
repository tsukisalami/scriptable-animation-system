using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// Basic ImpactObject, that automatically plays a particle system + sound on impact
    [AddComponentMenu("Ballistics/Impact Handler/Impact Object/Particles + Sound")]
    public class BasicImpactObject : ImpactObject
    {
        [Header("Particles")]
        public bool PlayParticles;
        public ParticleSystem ParticleSystem;

        [Header("Audio")]
        public AudioClip[] SoundEffects;
        public AudioSourcePreset AudioSourcePreset;

        public override void HandleSurfaceInteraction(in SurfaceInteractionInfo impact, HandledFlags flags)
        {
            if (PlayParticles)
                ParticleSystem.Play(true);

            if (SoundEffects.Length > 0)
                AudioManager.Play(SoundEffects[Random.Range(0, SoundEffects.Length)], impact.HitInfo.point, AudioSourcePreset);
        }
    }
}
