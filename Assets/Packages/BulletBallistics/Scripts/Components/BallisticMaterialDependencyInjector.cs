using System.Collections.Generic;
using UnityEngine;

namespace Ballistics
{
    /// Inject references to BallisticMaterials into the scene (automatically as a build step)
    /// otherwise they would be stripped from the build, as they are not referenced directly, but indirectly via the PhysicMaterial
    /// Custom BallisicMaterials have to be referenced in the scene manually

    [AddComponentMenu("Ballistics/Custom Materials/Dependency Injector")]
    public class BallisticMaterialDependencyInjector : MonoBehaviour
    {
        [Header("Ballistic Materials")]
        [Tooltip("Reference custom ballistic materials here, so they are included in the build. For the default BallisticMaterial ScriptableObjects, this is done automatically.")]
        [SerializeField] public List<ScriptableObject> References = new();
    }
}
