using UnityEngine;

namespace Ballistics
{
    /// Note that you have to create a CustomEditor that inherits from 'BallisticsBaseInspector' for inline inspectors to work!
    /// See BulletBallistics/Editor/CoreCustomInspector.cs
    public class InlineInspectorAttribute : System.Attribute
    {
    }

    public class PhysicalUnitAttribute : PropertyAttribute
    {
        public readonly PhysicalType Type;

        public PhysicalUnitAttribute(PhysicalType type)
        {
            Type = type;
        }
    }

    public class LayerAttribute : PropertyAttribute
    {
    }
}