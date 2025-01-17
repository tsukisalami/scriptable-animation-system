using UnityEngine;
using UnityEngine.Events;

namespace Ballistics
{
    // Simple example of an IDamageable component
    [AddComponentMenu("Ballistics/Impact Handler/Living Entity")]
    public class LivingEntity : MonoBehaviour, IDamageable
    {
        public float Health { get; private set; }
        public OnDamagedEvent OnDamaged = new();

        public void ApplyDamage(float amount)
        {
            Health = Mathf.Max(0, Health - amount);
            OnDamaged.Invoke(new(this, amount, Health));
        }
    }

    public readonly struct DamageInfo
    {
        public readonly IDamageable Context;
        public readonly float DamageAmount;
        public readonly float NewHealth;

        public DamageInfo(IDamageable ctxt, float damageAmount, float health)
        {
            Context = ctxt;
            DamageAmount = damageAmount;
            NewHealth = health;
        }
    }

    [System.Serializable]
    public class OnDamagedEvent : UnityEvent<DamageInfo> { }
}


