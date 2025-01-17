using UnityEditor;

namespace Ballistics
{
    [CustomEditor(typeof(BulletInfo))]
    [CanEditMultipleObjects]
    public class BulletInfoCustomInspector : Editor
    {
        private BulletInfo bulletInfo;

        private void OnEnable()
        {
            bulletInfo = target as BulletInfo;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (bulletInfo)
                EditorGUILayout.LabelField("maximum kinetic energy:\t " + (0.5f * bulletInfo.Mass * bulletInfo.Speed * bulletInfo.Speed).ToString() + " J", EditorStyles.miniLabel);
        }
    }
}