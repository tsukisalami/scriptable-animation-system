using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ballistics
{
    public class BallisticMaterialBrowser : EditorWindow
    {
        [MenuItem("Window/Ballistics/BallisticMaterial Browser")]
        public static void ShowWindow()
        {
            var window = GetWindow<BallisticMaterialBrowser>();
            window.titleContent = new GUIContent("Ballistic Materials", "Overview over all BallisticMaterials in the current project");
            window.Show();
        }

        private class Entry
        {
            private readonly BallisticMaterial BallisticMaterial;
            private readonly SerializedObject SerializedObject;
            public List<PropertyField> Fields = null;
            private bool expanded = false;

            public Entry(BallisticMaterial material)
            {
                BallisticMaterial = material;
                SerializedObject = new SerializedObject(material);
            }

            public void Draw()
            {
                using (new EditorGUI.IndentLevelScope()) {
                    using (new EditorGUI.DisabledGroupScope(true))
                        EditorGUILayout.ObjectField(BallisticMaterial.PhysicMaterial, typeof(PhysicsMaterial), false);
                }

                var foldoutRect = GUILayoutUtility.GetLastRect();
                foldoutRect.yMin = foldoutRect.yMax - EditorGUIUtility.singleLineHeight;
                expanded = EditorGUI.Foldout(foldoutRect, expanded, "", true);
                if (expanded) {
                    if (Fields is null)
                        Fields = SerializedObject.GetFields();
                    using (new EditorGUI.IndentLevelScope()) {
                        using (SerializedObject.ScopedEdit())
                            BallisticEditorExtension.DrawFields(Fields, true, true);
                    }
                }

                EditorGUILayout.Separator();
            }
        }

        private readonly List<Entry> Materials = new();
        private Vector2 scrollPos;

        void OnEnable()
        {
            Materials.Clear();
            foreach (var material in BallisticMaterialExtensions.GatherAll()) {
                if (material.PhysicMaterial != null)
                    Materials.Add(new Entry(material));
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUILayout.LabelField("Ballistic Material Browser", EditorStyles.largeLabel);
                if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.MaxWidth(60)))
                    OnEnable();
            }
            EditorGUILayout.Space();
            using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPos)) {
                using (new EditorGUI.IndentLevelScope()) {
                    foreach (var entry in Materials)
                        entry.Draw();
                }
                scrollPos = scrollView.scrollPosition;
            }
        }
    }
}

