using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{

    [AddComponentMenu("Ballistics/Bullet/Cinematic Bullet Follower")]
    public class CinematicBulletFollower : MonoBehaviour
    {
        public Camera Camera;
        public float Distance = 1;
        public float MouseSensitivity = 1;
        public float CameraDisableDelay = .5f;

        [Range(0, .1f)] public float MouseSmoothing = .001f;
        [Range(0, .1f)] public float VelocitySmoothing = .01f;

        private CinematicBulletProxy proxy;
        private Vector3 velocity;
        private BulletPose lastPose;
        public Vector2 targetOffset;
        private Vector2 offset;

        private void Start()
        {
            Core.OnBeforeRender += UpdatePos;
        }

        private void OnDestroy()
        {
            Core.OnBeforeRender -= UpdatePos;
        }

        public void Shoot(Weapon weapon, float zeroingAngle)
        {
            if (proxy != null) {
                Stop();
                StopAllCoroutines();
            }
            Camera.enabled = true;
            proxy = new(weapon.GetVisualBullet());
            proxy.Stopped += Stop;
            proxy.Updated += UpdateBulletPose;
            targetOffset = Vector2.zero;
            offset = Vector2.zero;
            velocity = Vector3.zero;
            var spawnPoint = weapon.BulletSpawnPoint.position;
            weapon.Shoot(spawnPoint,
                        Zeroing.Apply(weapon.BulletSpawnPoint.forward, zeroingAngle),
                        weapon.VisualSpawnPoint.position - spawnPoint,
                        proxy);
        }

        private void UpdatePos()
        {
            targetOffset += new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * MouseSensitivity;
            offset = Vector2.Lerp(offset, targetOffset, 1.0f - Mathf.Pow(MouseSmoothing, Time.unscaledDeltaTime));
            offset.y = Mathf.Clamp(offset.y, -45, 45);
            velocity = Vector3.Lerp(velocity, lastPose.Velocity, 1.0f - Mathf.Pow(VelocitySmoothing, Time.unscaledDeltaTime));

            var targetPose = (Vector3)lastPose.Position;
            var right = Vector3.Cross(velocity, Vector3.up);
            var back = Vector3.Cross(right, Vector3.up).normalized;

            var dir = Quaternion.AngleAxis(offset.x, Vector3.up) * Quaternion.AngleAxis(offset.y, right) * back;

            targetPose += dir * Distance;
            transform.position = targetPose;
            transform.LookAt(lastPose.Position, Vector3.up);
        }

        private void UpdateBulletPose(BulletPose pose)
        {
            lastPose = pose;
        }

        public void Stop()
        {
            if (proxy != null) {
                proxy.Stopped -= Stop;
                proxy.Updated -= UpdateBulletPose;
                proxy = null;
                StartCoroutine(DisableCameraIn(CameraDisableDelay));
            }
        }

        public System.Collections.IEnumerator DisableCameraIn(float time)
        {
            if (time > 0)
                yield return new WaitForSecondsRealtime(time);
            Camera.enabled = false;
        }

        private class CinematicBulletProxy : IVisualBullet
        {
            private readonly IVisualBullet Target;
            public event System.Action<BulletPose> Updated = delegate { };
            public event System.Action Stopped = delegate { };

            public CinematicBulletProxy(IVisualBullet target)
            {
                Target = target;
            }

            public void DestroyBullet()
            {
                Stopped.Invoke();
                Target.DestroyBullet();
            }

            public void InitializeBullet(in BulletPose pose, in float3 visualOffset)
            {
                Updated.Invoke(pose);
                Target.InitializeBullet(pose, visualOffset);
            }

            public void UpdateBullet(in BulletPose pose)
            {
                Updated.Invoke(pose);
                Target.UpdateBullet(pose);
            }
        }
    }
}

