using UnityEngine;

namespace Ballistics
{
    /// Spray controller, that enables Counter-Strike-style spray patterns
    [AddComponentMenu("Ballistics/Weapon Controller/Default Spread Controller")]
    public class DefaultSpreadController : SpreadController
    {
        [Tooltip("Maps each bullet index (x-axis) in the sequence to a horizontal deflection angle (y-axis).")]
        public AnimationCurve SpreadAngleHorizontal = AnimationCurve.Constant(0, 1, 0);

        [Tooltip("Maps each bullet index (x-axis) in the sequence to a vertical deflection angle (y-axis).")]
        public AnimationCurve SpreadAngleVertical = AnimationCurve.Constant(0, 1, 0);

        [Tooltip("Maps each bullet index (x-axis) in the sequence to an additional random deflection angle (y-axis).")]
        public AnimationCurve SpreadAngleRandom = AnimationCurve.Linear(0, 0, 10, 1);

        [Min(.001f), Tooltip("Time in seconds (since the last shot) after which the spread pattern is reset to the beginning.")]
        public float RecoveryTime = .4f;

        private int currentShot = 0;
        private float timer = 0;
        private float baseSpread;
        private float spreadFactor = 1;

        void Update()
        {
            if (timer > 0) {
                timer -= Time.deltaTime;
                if (timer <= 0)
                    currentShot = 0;
            }
        }

        public override void BulletFired()
        {
            timer = RecoveryTime;
            currentShot++;
        }

        public override Vector3 CalculateShootDirection(Weapon weapon)
        {
            var vertical = SpreadAngleVertical.Evaluate(currentShot) * spreadFactor;
            var horizontal = SpreadAngleHorizontal.Evaluate(currentShot) * spreadFactor;
            var random = (SpreadAngleRandom.Evaluate(currentShot) + baseSpread) * spreadFactor;
            var spawn = weapon.BulletSpawnPoint;
            var patternQuat = Quaternion.AngleAxis(horizontal, spawn.up) * Quaternion.AngleAxis(-vertical, spawn.right);
            var fwd = patternQuat * spawn.forward;
            var right = patternQuat * spawn.right;
            return Quaternion.AngleAxis(Random.value * 360f, fwd) * Quaternion.AngleAxis(Random.value * random, right) * fwd;
        }

        public void SetBaseSpread(float spread, float factor)
        {
            baseSpread = spread;
            spreadFactor = factor;
        }
    }
}
