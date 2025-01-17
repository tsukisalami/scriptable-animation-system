using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    [AddComponentMenu("Ballistics/Zeroing Crosshair Generator")]
    public class ZeroingCrosshairGenerator : MonoBehaviour
    {
        [Tooltip("Target weapon")]
        public Weapon Weapon;

        [Tooltip("As the scope is usually above the barrel, we have to aim slightly upwards to hit where the scope is looking.")]
        public bool CorrectScopeOffset = true;

        [Tooltip("Distances for which zeroing indicators are generated.")]
        public List<float> Distances = new();

        [Header("Visual")]
        public Material Material;
        [Layer] public int Layer = 0;
        [Range(.001f, 1f)] public float Offset = .5f;
        [Range(0, 85f)] public float Angle = 25f;
        [Range(.001f, 1f)] public float Length = .05f;
        [Range(.001f, 1f)] public float Thickness = .1f;
        [Range(.001f, .1f)] public float Scale = .1f;

        [Header("Advanced")]
        public bool ContinuousUpdate = false;

        [Tooltip("When drag is enabled, the flight path can not be calculated analytically. This sets the time step of the numerical simulation.")]
        [PhysicalUnit(PhysicalType.TIME)] public float TimeStep = .015f;

        private Mesh mesh;
        private Zeroing.SimulateJobHandle handle;

        private void Start()
        {
            if (!Weapon || !Weapon.BulletSpawnPoint)
                return;
            mesh = new Mesh();
            Recalculate();
        }

        private void OnDestroy()
        {
            Destroy(mesh);
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (ContinuousUpdate)
                Recalculate();
        }
#endif

        private void LateUpdate()
        {
            if (mesh)
                Graphics.DrawMesh(mesh, Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one), Material, Mathf.Clamp(Layer, 0, 31));
        }

        /// Recalculate zeroing for the given configuration. This is an expensive operation! Only call on change!
        public void Recalculate()
        {
            if (Distances.Count == 0 || !Weapon || !Weapon.BulletInfo || !Core.Environment.EnableGravity) {
                mesh.Clear();
                return;
            }
            var gravity = -Unity.Mathematics.math.length(Core.Environment.Gravity);
#if !BB_NO_AIR_RESISTANCE
            if (Core.Environment.EnableAirResistance) {
                if (handle.IsActive)
                    handle.Dispose();
                handle = Zeroing.ApproximateZeroingAnglesWithDrag(Distances, Weapon.BulletInfo, gravity, Core.Environment.AirDensity, TimeStep);
                Core.OnUpdateCompleted += Complete;
            } else
#endif
            {
                UpdateMesh(Zeroing.ZeroingAnglesNoDrag(Distances, Weapon.BulletInfo.Speed, gravity));
            }
        }

        private void Complete()
        {
            Core.OnUpdateCompleted -= Complete;
            var result = handle.Get();
            handle.Dispose();
            UpdateMesh(result);
        }

        private void UpdateMesh(Zeroing.Result[] data)
        {
            if (CorrectScopeOffset) {
                var scopeHeight = Vector3.Dot(transform.position - Weapon.BulletSpawnPoint.position, transform.up);
                for (var i = data.Length - 1; i > 0; i--)
                    data[i].Angle += Mathf.Atan2(scopeHeight, data[i].Distance) * Mathf.Rad2Deg;
            }

            var count = data.Length;
            var vertices = new Vector3[count * 6];
            var normals = new Vector3[count * 6];
            var indices = new int[count * 12];

            var length = Length * Scale;
            var thickness = Thickness * Scale;
            var minAngle = Mathf.Atan2(thickness, length) * Mathf.Rad2Deg;
            var angle = Mathf.Clamp(Angle, 0, 90f - minAngle) * Mathf.Deg2Rad;
            var angleInv = Mathf.Clamp(90 - Angle, minAngle, 90f) * Mathf.Deg2Rad;
            var sin = Mathf.Sin(angle);
            var cos = Mathf.Cos(angle);
            var down = thickness / Mathf.Sin(angleInv);
            var top = thickness / Mathf.Tan(angleInv);

            for (var i = 0; i < count; i++) {
                var pos = Vector3.forward * Offset + Vector3.down * Mathf.Tan(data[i].Angle * Mathf.Deg2Rad) * Offset;
                var vertex = i * 6;
                var index = i * 12;
                vertices[vertex + 0] = pos;
                vertices[vertex + 1] = pos + new Vector3(cos, -sin, 0) * length;
                vertices[vertex + 2] = pos + Vector3.down * down;
                vertices[vertex + 3] = pos + new Vector3(cos, -sin, 0) * (length - top) + Vector3.down * down;
                vertices[vertex + 4] = pos + new Vector3(-cos, -sin, 0) * length;
                vertices[vertex + 5] = pos + new Vector3(-cos, -sin, 0) * (length - top) + Vector3.down * down;
                indices[index + 0] = vertex + 0; indices[index + 1] = vertex + 1; indices[index + 2] = vertex + 2;
                indices[index + 3] = vertex + 2; indices[index + 4] = vertex + 1; indices[index + 5] = vertex + 3;
                indices[index + 6] = vertex + 0; indices[index + 7] = vertex + 2; indices[index + 8] = vertex + 4;
                indices[index + 9] = vertex + 5; indices[index + 10] = vertex + 4; indices[index + 11] = vertex + 2;
            }

            mesh.SetVertices(vertices);
            System.Array.Fill(normals, Vector3.back);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);
        }
    }
}