using System.Collections.Generic;
using UnityEditor;

namespace Ballistics
{
    public abstract class BallisticsBaseInspector : Editor
    {
        private bool firstFieldIsScript;
        private List<PropertyField> fields;

        private void Init()
        {
            fields = serializedObject.GetFields();
            firstFieldIsScript = fields.Count > 0 && fields[0].SerializedProperty.name.Equals(BallisticEditorExtension.ScriptPropertyName);
        }

        private void OnEnable()
        {
            Init();
        }

        public override void OnInspectorGUI()
        {
            if (fields == null)
                Init();
            using (serializedObject.ScopedEdit())
                BallisticEditorExtension.DrawFields(fields, firstFieldIsScript);
        }
    }

    [CustomEditor(typeof(Weapon), true), CanEditMultipleObjects]
    public class WeaponCustomInspector : BallisticsBaseInspector
    { }

    [CustomEditor(typeof(EnvironmentProvider), true), CanEditMultipleObjects]
    public class BulletHandlerCustomInspector : BallisticsBaseInspector
    { }

    [CustomEditor(typeof(BallisticSettings), true), CanEditMultipleObjects]
    public class BallisticSettingsCustomInspector : BallisticsBaseInspector
    { }

    [CustomEditor(typeof(BallisticMaterial), true), CanEditMultipleObjects]
    public class BallisticMaterialCustomInspector : BallisticsBaseInspector
    { }

    [CustomEditor(typeof(GenericImpactHandler), true), CanEditMultipleObjects]
    public class GlobalImpactHandlerCustomInspector : BallisticsBaseInspector
    { }
}