using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ballistics
{
    [CustomEditor(typeof(PhysicsMaterial), true)]
    [CanEditMultipleObjects]
    public class PhysicMaterialCustomInspector : Editor
    {
        private SerializedObject ballisticMaterial;
        private List<PropertyField> fields = new();

        private void OnEnable()
        {
            ballisticMaterial = null;
            fields.Clear();
            var materials = new BallisticMaterial[targets.Length];
            for (int i = 0; i < materials.Length; i++) {
                if ((targets[i] as PhysicsMaterial).TryGetBallisticMaterial(out var mat))
                    materials[i] = mat;
                else
                    return;
            }
            ballisticMaterial = new SerializedObject(materials);
            fields = ballisticMaterial.GetFields();
        }

        public override void OnInspectorGUI()
        {
            if (ballisticMaterial != null) {
                if (!IsValid(ballisticMaterial)) {
                    OnEnable();
                } else {
                    using (ballisticMaterial.ScopedEdit())
                        BallisticEditorExtension.DrawFields(fields, true, true);

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Physic Material", EditorStyles.boldLabel);
                }
            }
            base.OnInspectorGUI();
        }

        private bool IsValid(SerializedObject obj)
        {
            for (int i = 0; i < obj.targetObjects.Length; i++) {
                if (obj.targetObjects[i] == null)
                    return false;
            }
            return true;
        }
    }
    public static class BallisticMaterialExtensions
    {
        private const string physicMaterialContext = "CONTEXT/PhysicMaterial/";
        private const string ballisticMaterialContext = "CONTEXT/BallisticMaterial/";
        private const string toBallisticMaterial = "To BallisticMaterial";
        private const string unlinkBallisticMaterial = "Unlink BallisticMaterial";
        private const string openMaterialBrowser = "Open BallisticMaterial Browser";

        // Create
        [MenuItem(BallisticEditorExtension.MenuPrefix + "Ballistic Material")]
        static void CreateBallisticMaterial()
        {
            var name = "New BallisticMaterial";
            var material = ScriptableObject.CreateInstance<BallisticMaterial>();
            material.name = name;
            material.SetPhysicMaterial(new PhysicsMaterial());
            BallisticEditorExtension.SaveAsset(material.PhysicMaterial, "New BallisticMaterial.physicMaterial");
            BallisticEditorExtension.Link(material, material.PhysicMaterial);
        }

        // Link
        [MenuItem(physicMaterialContext + toBallisticMaterial)] static void Link(MenuCommand command) => Link(command.context);
        [MenuItem(BallisticEditorExtension.MenuPrefix + toBallisticMaterial)] static void Link() => Link(Selection.activeObject);
        static void Link(Object context)
        {
            if (context is PhysicsMaterial) {
                var material = ScriptableObject.CreateInstance<BallisticMaterial>();
                material.name = context.name;
                material.SetPhysicMaterial(context as PhysicsMaterial);
                BallisticEditorExtension.Link(material, context);
                Selection.activeObject = material;
            }
        }

        [MenuItem(physicMaterialContext + toBallisticMaterial, true)] static bool ValidateLink(MenuCommand command) => ValidateLink(command.context);
        [MenuItem(BallisticEditorExtension.MenuPrefix + toBallisticMaterial, true)] static bool ValidateLink() => ValidateLink(Selection.activeObject);
        static bool ValidateLink(Object active) => active is PhysicsMaterial && !IsBallisticMaterial(active as PhysicsMaterial);

        // Unlink
        [MenuItem(physicMaterialContext + unlinkBallisticMaterial)] static void Unlink(MenuCommand command) => Unlink(command.context);
        [MenuItem(ballisticMaterialContext + unlinkBallisticMaterial)] static void Unlink2(MenuCommand command) => Unlink((command.context as BallisticMaterial).PhysicMaterial);
        [MenuItem(BallisticEditorExtension.MenuPrefix + unlinkBallisticMaterial)] static void Unlink() => Unlink(Selection.activeObject);
        static void Unlink(Object context)
        {
            if (context is PhysicsMaterial && TryGetBallisticMaterial(context as PhysicsMaterial, out var ballisticMaterial)) {
                if (!EditorUtility.DisplayDialog("Unlink BallisticMaterial", "This will remove the associated BallisticMaterial information from this PhysicMaterial.", "Confirm", "Cancel"))
                    return;
                BallisticEditorExtension.Unlink(context, ballisticMaterial);
            }
        }

        [MenuItem(physicMaterialContext + unlinkBallisticMaterial, true)] static bool ValidateUnlink(MenuCommand command) => ValidateUnlink(command.context);
        [MenuItem(BallisticEditorExtension.MenuPrefix + unlinkBallisticMaterial, true)] static bool ValidateUnlink() => ValidateUnlink(Selection.activeObject);
        static bool ValidateUnlink(Object active) => active is PhysicsMaterial && IsBallisticMaterial(active as PhysicsMaterial);

        // Open Window
        [MenuItem(physicMaterialContext + openMaterialBrowser)]
        [MenuItem(ballisticMaterialContext + openMaterialBrowser)]
        static void OpenMaterialBrowser()
        {
            BallisticMaterialBrowser.ShowWindow();
        }

        // Helpers
        public static IEnumerable<BallisticMaterial> GatherAll()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(BallisticMaterial).Name))
                yield return AssetDatabase.LoadAssetAtPath<BallisticMaterial>(AssetDatabase.GUIDToAssetPath(guid));
        }

        public static bool IsBallisticMaterial(PhysicsMaterial material) => material.TryGetBallisticMaterial(out var _);

        public static bool TryGetBallisticMaterial(this PhysicsMaterial physicMaterial, out BallisticMaterial material)
        {
            material = AssetDatabase.LoadAssetAtPath<BallisticMaterial>(AssetDatabase.GetAssetPath(physicMaterial));
            return material != null;
        }
    }
}

