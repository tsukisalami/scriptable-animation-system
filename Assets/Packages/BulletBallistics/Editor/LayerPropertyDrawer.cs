using UnityEditor;
using UnityEngine;

namespace Ballistics
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    class LayerAttributeEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
                property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}

