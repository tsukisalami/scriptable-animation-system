using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Ballistics
{
    public class BallisticsCompilationConfigWindow : EditorWindow
    {
        [MenuItem("Window/Ballistics/Ballistic Config")]
        public static void ShowWindow()
        {
            var window = GetWindow<BallisticsCompilationConfigWindow>(true, "Ballistics Configuration", true);
            window.ShowUtility();
        }

        private struct BallisticsDefine
        {
            private const string BallisticsDefinePrefix = "BB_";
            public readonly string Name;
            public readonly GUIContent Label;
            public bool Active;
            public bool Enable;

            public BallisticsDefine(string name, GUIContent label)
            {
                Name = BallisticsDefinePrefix + name;
                Label = label;
                Active = false;
                Enable = false;
            }
        }

        private readonly BallisticsDefine[] defines = new BallisticsDefine[] {
            new BallisticsDefine("NO_AIR_RESISTANCE", new ("Disable Air Resistance", "Disable air resistance and wind forces on the projectiles.")),
            new BallisticsDefine("NO_SPIN", new ("Disable Bullet Spin", "Disable magnus effect due to spinning bullets (only when air resistance is enabled).")),
            new BallisticsDefine("NO_DISTANCE_TRACKING", new ("Disable Bullet Distance Tracking", "Traveled distance is not calculated and saved for each bullet..")),

        };
        private int activeTarget;

        private static List<NamedBuildTarget> AvailableBuildTargets;
        private static string[] AvailableBuildTargetNames;

        private void OnEnable()
        {
            AvailableBuildTargets = NamedBuildTargets();
            AvailableBuildTargetNames = new string[AvailableBuildTargets.Count];
            for (int i = 0; i < AvailableBuildTargets.Count; i++)
                AvailableBuildTargetNames[i] = AvailableBuildTargets[i].TargetName;
            activeTarget = AvailableBuildTargets.IndexOf(NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup));
            Init();
        }

        private void Init()
        {
            if (activeTarget < 0 || activeTarget >= AvailableBuildTargets.Count)
                return;
            var activeDefineList = ActiveDefines(AvailableBuildTargets[activeTarget]);
            for (var i = 0; i < defines.Length; i++) {
                var enabled = activeDefineList.Contains(defines[i].Name);
                defines[i].Active = enabled;
                defines[i].Enable = enabled;
            }
        }

        private bool CheckUnmodified()
        {
            return System.Array.TrueForAll(defines, define => define.Active == define.Enable);
        }

        private void OnGUI()
        {
            using (new EditorGUI.IndentLevelScope()) {
                EditorGUILayout.Space();
                {
                    var newActiveTarget = EditorGUILayout.Popup("Build Target", activeTarget, AvailableBuildTargetNames);
                    if (activeTarget != newActiveTarget) {
                        activeTarget = newActiveTarget;
                        Init();
                    }
                }
                EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - EditorGUIUtility.singleLineHeight * 2;
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Configuration", EditorStyles.largeLabel);
                using (new EditorGUI.IndentLevelScope()) {
                    var style = new GUIStyle(EditorStyles.label);
                    for (var i = 0; i < defines.Length; i++) {
                        style.fontStyle = defines[i].Enable != defines[i].Active ? FontStyle.Bold : FontStyle.Normal;// = style.active;
                        using (new EditorGUILayout.HorizontalScope()) {
                            EditorGUILayout.PrefixLabel(defines[i].Label, EditorStyles.toggle, style);
                            using (new IndentationScope(0))
                                defines[i].Enable = EditorGUILayout.Toggle(defines[i].Enable);
                        }
                    }
                }

                EditorGUILayout.Space();

                using (new GUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();


                    if (GUILayout.Button("Apply To All Targets", EditorStyles.miniButton)) {
                        foreach (var target in AvailableBuildTargets)
                            Apply(target, defines);
                    }

                    EditorGUILayout.Space();

                    using (new EditorGUI.DisabledGroupScope(CheckUnmodified())) {
                        if (GUILayout.Button("Apply", EditorStyles.miniButton)) {
                            if (activeTarget >= 0 && activeTarget < AvailableBuildTargets.Count)
                                Apply(AvailableBuildTargets[activeTarget], defines);
                            Init();
                        }
                        if (GUILayout.Button("Revert", EditorStyles.miniButton))
                            Init();
                    }
                }
            }
        }

        private static List<NamedBuildTarget> NamedBuildTargets()
        {
            var targets = new List<NamedBuildTarget>();
            foreach (BuildTargetGroup group in System.Enum.GetValues(typeof(BuildTargetGroup))) {
                try {
                    targets.Add(NamedBuildTarget.FromBuildTargetGroup(group));
                } catch (System.Exception) { }
            }
            return targets;
        }

        private static List<string> ActiveDefines(NamedBuildTarget target)
        {
            var activeDefineList = new List<string>();
            PlayerSettings.GetScriptingDefineSymbols(target, out var activeDefines);
            activeDefineList.AddRange(activeDefines);
            return activeDefineList;
        }

        private static void Apply(NamedBuildTarget target, BallisticsDefine[] defines)
        {
            var activeDefineList = ActiveDefines(target);
            foreach (var define in defines) {
                if (define.Enable) {
                    if (!activeDefineList.Contains(define.Name))
                        activeDefineList.Add(define.Name);
                } else {
                    activeDefineList.Remove(define.Name);
                }
            }
            try {
                PlayerSettings.SetScriptingDefineSymbols(target, activeDefineList.ToArray());
            } catch (System.Exception) { }
        }
    }
}
