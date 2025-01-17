using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Ballistics
{

    public class ScenePostProcessor
    {
        // TODO: this way may be faster for large projects.. measure?
        // private static List<GameObject> rootObjects = new();
        // private static List<Collider> colliders = new();
        // private static void FindColls()
        // {
        //     var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        //     scene.GetRootGameObjects(rootObjects);

        //     for (int i = 0; i < rootObjects.Count; ++i) {
        //         rootObjects[i].GetComponentsInChildren(true, colliders);
        //         foreach (var collider in colliders) {
        //         }
        //     }
        // }

        [UnityEditor.Callbacks.PostProcessSceneAttribute(1)]
        public static void OnPostprocessScene()
        {
            var go = new GameObject("BallisticsDependencyInjector");
            var depInj = go.AddComponent<BallisticMaterialDependencyInjector>();
            depInj.References.AddRange(BallisticMaterialExtensions.GatherAll());
        }
    }

    public static class BallisticEditorExtension
    {
        public const string ScriptPropertyName = "m_Script";
        public const string MenuPrefix = "Assets/Create/Ballistics/";

        public static List<PropertyField> GetFields(this SerializedObject serializedObject)
        {
            var fields = new List<PropertyField>();
            var fieldsInfo = serializedObject.targetObject.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var property in serializedObject.EnumerateVisibleProperties(false)) {
                int index = System.Array.FindIndex(fieldsInfo, field => field.Name == property.propertyPath);
                var propertyField = new PropertyField {
                    SerializedProperty = property,
                    InlineInspector = property.propertyType == SerializedPropertyType.ObjectReference && fieldsInfo.HasAttribute<InlineInspectorAttribute>(index)
                };
                fields.Add(propertyField);
            }
            return fields;
        }

        public static IEnumerable<SerializedProperty> EnumerateVisibleProperties(this SerializedObject target, bool skipScriptProperty = true)
        {
            var it = target.GetIterator();
            it.NextVisible(true);
            if (skipScriptProperty && it.propertyPath.Equals(ScriptPropertyName))
                it.NextVisible(true);
            do {
                yield return it.Copy();
            } while (it.NextVisible(false));
        }

        public static void DrawFields(List<PropertyField> fields, bool firstFieldIsScript, bool hideScriptField = false)
        {
            if (fields.Count > 0 && firstFieldIsScript && !hideScriptField) {
                using (new EditorGUI.DisabledGroupScope(true)) // disable m_Script property
                    EditorGUILayout.PropertyField(fields[0].SerializedProperty);
            }
            for (int i = firstFieldIsScript ? 1 : 0; i < fields.Count; i++) {
                EditorGUILayout.PropertyField(fields[i].SerializedProperty);
                if (fields[i].InlineInspector)
                    DrawInlinedInspector(fields[i]);
            }
        }

        public static void DrawInlinedInspector(this PropertyField field)
        {
            if (field.SerializedProperty.objectReferenceValue != null && !field.SerializedProperty.hasMultipleDifferentValues) {
                var foldoutRect = GUILayoutUtility.GetLastRect();
                foldoutRect.yMin = foldoutRect.yMax - EditorGUIUtility.singleLineHeight;
                using (new IndentationScope(Mathf.Max(0, EditorGUI.indentLevel - 1)))
                    field.SerializedProperty.isExpanded = EditorGUI.Foldout(foldoutRect, field.SerializedProperty.isExpanded, "", true);
                if (field.SerializedProperty.isExpanded) {
                    if (field.InlinedObject == null || field.InlinedObject.targetObject != field.SerializedProperty.objectReferenceValue) {
                        field.InlinedObject = new SerializedObject(field.SerializedProperty.objectReferenceValue);
                        field.InlinedFields = new List<PropertyField>();
                        foreach (var property in field.InlinedObject.EnumerateVisibleProperties(true))
                            field.InlinedFields.Add(new PropertyField() { SerializedProperty = property, InlinedObject = null, InlineInspector = false, InlinedFields = null });
                    }
                    using (new EditorGUI.IndentLevelScope()) {
                        using (field.InlinedObject.ScopedEdit()) {
                            foreach (var inlined in field.InlinedFields)
                                EditorGUILayout.PropertyField(inlined.SerializedProperty);
                        }
                    }
                    EditorGUILayout.Space();
                }
            }
        }

        public static bool HasAttribute<T>(this System.Reflection.FieldInfo[] infos, int index)
        {
            if (index >= 0)
                return infos[index].GetCustomAttributes(typeof(T), false).Length > 0;
            return false;
        }

        public static SerializedPropertyEditScope ScopedEdit(this SerializedObject target)
        {
            return new SerializedPropertyEditScope(target);
        }

        public static void SaveAsset(Object asset, string name)
        {
            var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(ActiveProjectFolder(), name));
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static string ActiveProjectFolder()
        {
            if (Selection.assetGUIDs.Length == 0) {
                return "Assets/";
            } else {
                var selectionPath = AssetDatabase.GUIDToAssetPath(Selection.assetGUIDs[0]);
                return AssetDatabase.IsValidFolder(selectionPath) ? selectionPath : Path.GetDirectoryName(selectionPath);
            }
        }

        public static bool IsLinked(Object a, Object b)
        {
            return AssetDatabase.GetAssetPath(a).Equals(AssetDatabase.GetAssetPath(b));
        }

        public static void Link(Object obj, Object target)
        {
            AssetDatabase.AddObjectToAsset(obj, target);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
        }

        public static void Unlink(Object parent, Object child)
        {
            AssetDatabase.RemoveObjectFromAsset(child);
            Object.DestroyImmediate(child, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(parent));
            EditorUtility.SetDirty(parent);
        }

        public static Rect HSlice(this Rect rect, float from, float width)
        {
            return new Rect(rect.x + from, rect.y, width, rect.height);
        }
    }

    public struct SerializedPropertyEditScope : System.IDisposable
    {
        private readonly SerializedObject serializedObject;

        public SerializedPropertyEditScope(SerializedObject target)
        {
            serializedObject = target;
            serializedObject.Update();
        }

        public void Dispose()
        {
            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }
    }

    public class PropertyField
    {
        public SerializedProperty SerializedProperty;
        public bool InlineInspector;
        public SerializedObject InlinedObject;
        public List<PropertyField> InlinedFields;
    }

    public class IndentationScope : System.IDisposable
    {
        private readonly int oldLevel;
        public IndentationScope(int level)
        {
            oldLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = level;
        }

        public void Dispose()
        {
            EditorGUI.indentLevel = oldLevel;
        }
    }
}