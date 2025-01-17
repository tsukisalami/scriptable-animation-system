using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Ballistics
{
    public static class RaycastExtensions
    {
        private const int MaxSteps = 16;
        public static bool FindExit(this Collider collider, in float3 rayOrigin, in float3 rayDirection, out RaycastHit exitHitInfo)
        {
            exitHitInfo = default;
            bool foundExit = false;
            var bounds = collider.bounds;
            var maxDistance = bounds.size.magnitude;
            if (BoundsExtensions.FindExit(bounds.min, bounds.max, rayOrigin, rayDirection, out var exit)) {
                var backRay = new Ray(rayOrigin + rayDirection * (exit + BallisticsUtil.Epsilon), -rayDirection);
                // step backwards through collider
                var i = 0;
                while (collider.Raycast(backRay, out var hit, maxDistance)
                        && i <= MaxSteps    // limit iterations
                        && math.distancesq(hit.point, rayOrigin) > BallisticsUtil.Epsilon) { // when queries-hit-backfaces is enabled, the entry-point would be detected as the exit for non-convex mesh colliders
                    backRay.origin = hit.point - (Vector3)rayDirection * BallisticsUtil.Epsilon;
                    exitHitInfo = hit;
                    foundExit = true;
                    i++;
                }
            }
            return foundExit;
        }
    }

    [BurstCompile]
    public static class BoundsExtensions
    {
        [BurstCompile]
        public static bool FindExit(in float3 min, in float3 max, in float3 origin, in float3 direction, out float far)
        {
            var a = (min - origin) / direction;
            var b = (max - origin) / direction;
            float near = math.cmax(math.min(a, b));
            far = math.cmin(math.max(a, b));
            return far >= 0 && near <= far;
        }
    }
}