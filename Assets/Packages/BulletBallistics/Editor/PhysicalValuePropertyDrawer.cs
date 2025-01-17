using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ballistics
{
    [CustomPropertyDrawer(typeof(PhysicalUnitAttribute))]
    public class PhysicalValuePropertyDrawer : PropertyDrawer
    {
        private const string BallisticsPhysicalUnitPrefix = "Ballistics_PhysicalType_";
        private const float MinMaxSliderLabelWidth = 50;
        private static void SetDefaultRepresentation(SerializedProperty property, int index)
        {
            EditorPrefs.SetInt(BallisticsPhysicalUnitPrefix + property.name, index);
        }

        private static PhysicalValue TryGetRepresentation(PhysicalType type, SerializedProperty property)
        {
            PhysicalValue value = new();
            value.Set(type, EditorPrefs.GetInt(BallisticsPhysicalUnitPrefix + property.name, 0));
            return value;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var width = Mathf.Min(position.width - 150, 40);
            var unit = attribute as PhysicalUnitAttribute;
            var PhysicalValue = TryGetRepresentation(unit.Type, property);
            position.xMax -= width;

            using (var check = new EditorGUI.ChangeCheckScope()) {
                if (property.propertyType == SerializedPropertyType.Float) {
                    var baseValue = PhysicalValue.FromSi(property.doubleValue);
                    if (unit.Type == PhysicalType.PERCENTAGE)
                        baseValue = EditorGUI.Slider(position, label, (float)baseValue, 0f, (float)PhysicalValue.FromSi(1));
                    else
                        baseValue = EditorGUI.FloatField(position, label, (float)baseValue);
                    if (check.changed)
                        property.doubleValue = PhysicalValue.ToSi(baseValue);
                } else if (property.propertyType == SerializedPropertyType.Vector2) {
                    var vec = PhysicalValue.FromSi(property.vector2Value);
                    if (unit.Type == PhysicalType.PERCENTAGE)
                        vec = MinMaxSliderWithLabels(position, label, vec, 0, (float)PhysicalValue.FromSi(1));
                    else
                        vec = EditorGUI.Vector2Field(position, label, vec);
                    if (check.changed)
                        property.vector2Value = PhysicalValue.ToSi(vec);
                } else if (property.propertyType == SerializedPropertyType.Vector3) {
                    var vec = PhysicalValue.FromSi(property.vector3Value);
                    vec = EditorGUI.Vector3Field(position, label, vec);
                    if (check.changed)
                        property.vector3Value = PhysicalValue.ToSi(vec);
                } else {
                    EditorGUI.PropertyField(position, property);
                }
            }

            var representations = PhysicalValue.Representations();
            var active = representations[PhysicalValue.ActiveRepresentation];
            if (EditorGUI.DropdownButton(new Rect(position.xMax + 2, position.yMin, width - 2, position.height), new GUIContent(active.Abbreviation, active.FullName), FocusType.Passive, EditorStyles.miniButtonRight)) {
                void callback(object data) => SetDefaultRepresentation(property, (int)data);
                GUI.FocusControl(null);
                var popup = new GenericMenu();
                foreach (var rep in representations)
                    popup.AddItem(new GUIContent(rep.Value.FullName), rep.Value == active, callback, rep.Key);
                popup.ShowAsContext();
            }
        }

        private Vector2 MinMaxSliderWithLabels(Rect position, GUIContent label, Vector2 val, float min, float max)
        {
            position = EditorGUI.PrefixLabel(position, label);
            using (new IndentationScope(0)) {
                var labelWidth = Mathf.Min(MinMaxSliderLabelWidth * 2, position.width) * .5f;
                var sliderWidth = position.width - labelWidth * 2;
                if (sliderWidth >= MinMaxSliderLabelWidth)
                    EditorGUI.MinMaxSlider(position.HSlice(labelWidth + 2, sliderWidth - 4), ref val.x, ref val.y, min, max);
                val.x = EditorGUI.FloatField(position.HSlice(0, labelWidth), val.x);
                val.y = EditorGUI.FloatField(position.HSlice(sliderWidth + labelWidth, labelWidth), val.y);
            }
            return val;
        }
    }
}

