using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TimeScaleController : MonoBehaviour
{
    [Range(0f, 2f)]
    [SerializeField] private float timeScale = 1f;
    
    private float lastTimeScale;

    // Property to handle timeScale access
    public float TimeScale
    {
        get => timeScale;
        set
        {
            timeScale = value;
            ApplyTimeScale();
        }
    }

    private void ApplyTimeScale()
    {
        if (timeScale != lastTimeScale)
        {
            Time.timeScale = timeScale;
            lastTimeScale = timeScale;
            Debug.Log($"Time Scale changed to: {timeScale}");
        }
    }

    private void OnValidate()
    {
        ApplyTimeScale();
    }

    private void Start()
    {
        ApplyTimeScale();
        Debug.Log($"TimeScaleController started. Initial time scale: {timeScale}");
    }

    private void Update()
    {
        ApplyTimeScale();
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        Debug.Log("TimeScaleController disabled. Time scale reset to 1.0");
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TimeScaleController))]
    public class TimeScaleControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TimeScaleController controller = (TimeScaleController)target;

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Time Scale Control", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time Scale:", GUILayout.Width(70));
            float newTimeScale = EditorGUILayout.Slider(controller.TimeScale, 0f, 2f);
            EditorGUILayout.LabelField(newTimeScale.ToString("F2"), GUILayout.Width(40));
            EditorGUILayout.EndHorizontal();

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(controller, "Change Time Scale");
                controller.TimeScale = newTimeScale;
                EditorUtility.SetDirty(controller);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "0.0 = Paused\n" +
                "0.5 = Half Speed\n" +
                "1.0 = Normal Speed\n" +
                "2.0 = Double Speed", 
                MessageType.Info);

            // Display current actual time scale
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Current Time.timeScale:", GUILayout.Width(140));
                EditorGUILayout.LabelField(Time.timeScale.ToString("F2"));
            }

            // Add a test button
            if (GUILayout.Button("Test Time Scale"))
            {
                Debug.Log($"Current Time.timeScale: {Time.timeScale}");
                // Create a visual test by spawning a cube that falls
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = Vector3.up * 5;
                cube.AddComponent<Rigidbody>();
                EditorApplication.QueuePlayerLoopUpdate(); // Force a physics update
            }
        }
    }
#endif
} 