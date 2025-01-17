using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    [CreateAssetMenu(fileName = "New GenericImpactHandler", menuName = "Ballistics/Impact Handler/Generic Impact Handler", order = 1)]
    public class GenericImpactHandler : ImpactHandlerObject
    {
        [Tooltip("Check for ColliderImpactHandler component on hit objects.")]
        public bool CheckForPerColliderHandler = false;

        [Header("Physics")]
        [Tooltip("Apply forces, if hit collider has rigidbody component attached.")]
        public bool HandleRigidbodyForces = true;
        public float ForceMultiplier = 1.0f;

        [Header("Damage")]
        [Tooltip("Apply damage, if collider has an IDamageable component attached.")]
        public bool DealDamage = false;
        public float DamageMultiplier = 1.0f;

        [Header("Audio")]
        [Tooltip("Play a random impact sound from the given list.")]
        public AudioClip[] ImpactSounds;
        [InlineInspector] public AudioSourcePreset audioSourceSettings;

        public override void Initialize() { }

        public override HandledFlags HandleImpact(in ImpactInfo info, HandledFlags flags)
        {
            if (CheckForPerColliderHandler && info.HitInfo.collider.TryGetComponent<IImpactHandler>(out var handler)) {
                flags = handler.HandleImpact(info, flags);
            }

            if (!flags.HasFlag(HandledFlags.PHYSICS) && HandleRigidbodyForces) {
                flags |= HandledFlags.PHYSICS;
                var energyLoss = info.EnergyLossPerUnit * info.ImpactDepth;
                var deltaV = Mathf.Sqrt(2 * energyLoss);
                info.HitInfo.rigidbody?.AddForceAtPosition(ForceMultiplier * math.normalize(info.EntryVelocity) * deltaV * info.BulletInfo.Mass, info.HitInfo.point, ForceMode.Impulse);
            }

            if (!flags.HasFlag(HandledFlags.DAMAGE) && DealDamage) {
                flags |= HandledFlags.DAMAGE;
                if (info.HitInfo.collider.TryGetComponent<IDamageable>(out var damageable))
                    damageable.ApplyDamage(info.BulletInfo.Damage * (math.lengthsq(info.EntryVelocity) / (info.BulletInfo.Speed * info.BulletInfo.Speed)) * DamageMultiplier);
            }

            return flags;
        }

        public override HandledFlags HandleSurfaceInteraction(in SurfaceInteractionInfo info, HandledFlags flags)
        {
            if (CheckForPerColliderHandler && info.HitInfo.collider.TryGetComponent<IImpactHandler>(out var handler)) {
                flags = handler.HandleSurfaceInteraction(info, flags);
            }

            if (!flags.HasFlag(HandledFlags.PHYSICS) && HandleRigidbodyForces && info.Type == SurfaceInteractionInfo.InteractionType.RICOCHET) {
                flags |= HandledFlags.PHYSICS;
                info.HitInfo.rigidbody?.AddForceAtPosition(-ForceMultiplier * info.HitInfo.normal * math.length(info.Velocity * (1.0f - info.SpeedFactor)) * info.BulletInfo.Mass, info.HitInfo.point, ForceMode.Impulse);
            }

            if (!flags.HasFlag(HandledFlags.AUDIO) && ImpactSounds.Length > 0 && info.Type != SurfaceInteractionInfo.InteractionType.EXIT) {
                flags |= HandledFlags.AUDIO;
                AudioManager.Play(ImpactSounds[UnityEngine.Random.Range(0, ImpactSounds.Length)], info.HitInfo.point, audioSourceSettings);
            }

            return flags;
        }
    }
}

