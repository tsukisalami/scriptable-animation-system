using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// Map of PhysicMaterials to BallisticMaterials
    public static class BallisticMaterialCache
    {
        private static Dictionary<PhysicsMaterial, IBallisticMaterial> Materials;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Materials = new();
            DefaultMaterial = null;
        }

        public static IBallisticMaterial DefaultMaterial { get => defaultMaterial; set { defaultMaterial = value; hasDefaultMaterial = defaultMaterial != null; } }
        private static IBallisticMaterial defaultMaterial = null;
        private static bool hasDefaultMaterial = false;


        public static bool Add(BallisticMaterial material)
        {
            if (material != null && material.PhysicMaterial != null)
                return Materials.TryAdd(material.PhysicMaterial, material);
            return false;
        }

        public static bool Add(PhysicsMaterial physicMaterial, IBallisticMaterial material)
        {
            if (material != null && physicMaterial != null)
                return Materials.TryAdd(physicMaterial, material);
            return false;
        }

        public static void Clear()
        {
            Materials.Clear();
        }

        public static bool TryGet(PhysicsMaterial physicMaterial, out IBallisticMaterial ballisticMaterial)
        {
            if (physicMaterial != null && Materials.TryGetValue(physicMaterial, out ballisticMaterial)) {
                return true;
            } else {
                ballisticMaterial = defaultMaterial;
                return hasDefaultMaterial;
            }
        }
    }
}



