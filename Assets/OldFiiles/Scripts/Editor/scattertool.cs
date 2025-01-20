using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ScatterTool : EditorWindow
{
    private enum ToolMode { Selection, Placement, Layers }
    private enum SelectionShape { Rectangle, Triangle, FreeDraw }
    private ToolMode currentMode = ToolMode.Selection;
    private SelectionShape selectionShapeMode = SelectionShape.Rectangle;

    // Persistent Selection Data
    private List<Vector3> waypoints = new List<Vector3>();
    private Vector3 selectionPosition;
    private Vector3 selectionScale = Vector3.one;
    private bool useSelectionBounds = true;

    // Layer System
    private List<Layer> layers = new List<Layer>();
    private int currentLayerIndex = 0;

    // Painting settings
    private float brushSize = 1f;
    private float brushStrength = 1f;
    private float minSizeVariation = 1f;
    private float maxSizeVariation = 1f;
    private float rotationVariation = 0f;

    [MenuItem("Tools/Scatter Tool")]
    public static void ShowWindow()
    {
        GetWindow<ScatterTool>("Scatter Tool");
    }

    void OnGUI()
    {
        DrawToolModeButtons();
        DrawCurrentModeUI();
    }

    private void DrawToolModeButtons()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Selection Mode")) currentMode = ToolMode.Selection;
        if (GUILayout.Button("Placement Mode")) currentMode = ToolMode.Placement;
        if (GUILayout.Button("Layer Control Mode")) currentMode = ToolMode.Layers;
        GUILayout.EndHorizontal();
    }

    private void DrawCurrentModeUI()
    {
        switch (currentMode)
        {
            case ToolMode.Selection:
                DrawSelectionModeUI();
                break;
            case ToolMode.Placement:
                DrawPlacementModeUI();
                break;
            case ToolMode.Layers:
                DrawLayerControlModeUI();
                break;
        }
    }

    private void DrawSelectionModeUI()
    {
        GUILayout.Label("Selection Mode", EditorStyles.boldLabel);

        // Selection Shape Mode Dropdown
        selectionShapeMode = (SelectionShape)EditorGUILayout.EnumPopup("Selection Shape Mode", selectionShapeMode);

        if (GUILayout.Button("Clear Waypoints"))
        {
            waypoints.Clear();
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Create Shape"))
        {
            CreateShape();
            SceneView.RepaintAll();
        }

        useSelectionBounds = EditorGUILayout.Toggle("Use Selection Bounds", useSelectionBounds);
    }

    private void DrawPlacementModeUI()
    {
        GUILayout.Label("Placement Mode", EditorStyles.boldLabel);

        if (layers.Count > 0)
        {
            EditorGUILayout.LabelField("Current Layer: " + layers[currentLayerIndex].name);
            if (GUILayout.Button("Scatter Prefabs"))
            {
                ScatterPrefabs(layers[currentLayerIndex]);
            }

            useSelectionBounds = EditorGUILayout.Toggle("Use Selection Bounds", useSelectionBounds);

            GUILayout.Label("Manual Painting", EditorStyles.boldLabel);
            brushSize = EditorGUILayout.FloatField("Brush Size", brushSize);
            brushStrength = EditorGUILayout.FloatField("Brush Strength", brushStrength);
            minSizeVariation = EditorGUILayout.FloatField("Min Size Variation", minSizeVariation);
            maxSizeVariation = EditorGUILayout.FloatField("Max Size Variation", maxSizeVariation);
            rotationVariation = EditorGUILayout.FloatField("Rotation Variation", rotationVariation);

            if (GUILayout.Button("Paint Prefabs"))
            {
                PaintPrefabs(layers[currentLayerIndex]);
            }
        }
        else
        {
            GUILayout.Label("No layers available. Add a layer in Layer Control Mode.");
        }
    }

    private void DrawLayerControlModeUI()
    {
        GUILayout.Label("Layer Control Mode", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Layer"))
        {
            layers.Add(new Layer { name = "New Layer", prefabs = new List<GameObject>() });
        }

        if (layers.Count > 0)
        {
            currentLayerIndex = EditorGUILayout.Popup("Current Layer", currentLayerIndex, layers.ConvertAll(l => l.name).ToArray());
            Layer currentLayer = layers[currentLayerIndex];

            currentLayer.name = EditorGUILayout.TextField("Layer Name", currentLayer.name);

            GUILayout.Label("Prefabs in Layer", EditorStyles.boldLabel);
            for (int i = 0; i < currentLayer.prefabs.Count; i++)
            {
                currentLayer.prefabs[i] = (GameObject)EditorGUILayout.ObjectField(currentLayer.prefabs[i], typeof(GameObject), false);
            }

            if (GUILayout.Button("Add Prefab"))
            {
                currentLayer.prefabs.Add(null);
            }

            GUILayout.Label("Avoidance Settings", EditorStyles.boldLabel);
            currentLayer.avoidanceEnabled = EditorGUILayout.Toggle("Enable Avoidance", currentLayer.avoidanceEnabled);

            if (currentLayer.avoidanceEnabled)
            {
                if (GUILayout.Button("Add Avoidance Prefab"))
                {
                    currentLayer.avoidancePrefabs.Add(null);
                }

                for (int i = 0; i < currentLayer.avoidancePrefabs.Count; i++)
                {
                    currentLayer.avoidancePrefabs[i] = (GameObject)EditorGUILayout.ObjectField(currentLayer.avoidancePrefabs[i], typeof(GameObject), false);
                }
            }
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (currentMode == ToolMode.Selection)
        {
            HandleSelectionInput();

            Handles.color = new Color(0, 1, 0, 0.25f);
            DrawSelectionShape();
        }

        if (currentMode == ToolMode.Placement && Event.current.type == EventType.MouseDown)
        {
            PaintPrefabs(layers[currentLayerIndex]);
            Event.current.Use();
        }
    }

    private void HandleSelectionInput()
    {
        Event guiEvent = Event.current;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 point = hit.point;

                // Handle selection based on shape mode
                if (selectionShapeMode == SelectionShape.FreeDraw)
                {
                    waypoints.Add(point);
                    SceneView.RepaintAll();
                }
                else if (selectionShapeMode == SelectionShape.Rectangle && waypoints.Count < 4)
                {
                    waypoints.Add(point);
                    if (waypoints.Count == 4) { CloseShape(); }
                    SceneView.RepaintAll();
                }
                else if (selectionShapeMode == SelectionShape.Triangle && waypoints.Count < 3)
                {
                    waypoints.Add(point);
                    if (waypoints.Count == 3) { CloseShape(); }
                    SceneView.RepaintAll();
                }
            }

            guiEvent.Use();
        }
    }

    private void CreateShape()
    {
        waypoints.Clear();
        Vector3 center = selectionPosition;

        if (selectionShapeMode == SelectionShape.Rectangle)
        {
            waypoints.Add(center + new Vector3(-selectionScale.x / 2, 0, -selectionScale.z / 2));
            waypoints.Add(center + new Vector3(selectionScale.x / 2, 0, -selectionScale.z / 2));
            waypoints.Add(center + new Vector3(selectionScale.x / 2, 0, selectionScale.z / 2));
            waypoints.Add(center + new Vector3(-selectionScale.x / 2, 0, selectionScale.z / 2));
        }
        else if (selectionShapeMode == SelectionShape.Triangle)
        {
            waypoints.Add(center + new Vector3(-selectionScale.x / 2, 0, -selectionScale.z / 2));
            waypoints.Add(center + new Vector3(selectionScale.x / 2, 0, -selectionScale.z / 2));
            waypoints.Add(center + new Vector3(0, 0, selectionScale.z / 2));
        }
    }

    private void DrawSelectionShape()
    {
        if (waypoints.Count > 0)
        {
            Handles.color = Color.green;

            for (int i = 0; i < waypoints.Count; i++)
            {
                // Draw numbered waypoint
                Handles.Label(waypoints[i], (i + 1).ToString());

                // Draw waypoint move handles
                EditorGUI.BeginChangeCheck();
                Vector3 newPos = Handles.PositionHandle(waypoints[i], Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    waypoints[i] = newPos;
                    SceneView.RepaintAll();
                }

                // Draw connection lines
                if (i > 0)
                {
                    Handles.DrawLine(waypoints[i - 1], waypoints[i]);
                }
                if (waypoints.Count > 2 && i == waypoints.Count - 1 && selectionShapeMode == SelectionShape.FreeDraw)
                {
                    Handles.DrawLine(waypoints[i], waypoints[0]);  // Close the shape
                }
            }
        }
    }

    private void CloseShape()
    {
        if (selectionShapeMode == SelectionShape.Rectangle)
        {
            // Close the rectangle shape (first to last waypoint connection is automatic for rect)
            Handles.DrawLine(waypoints[3], waypoints[0]);
        }
        else if (selectionShapeMode == SelectionShape.Triangle)
        {
            // Close the triangle shape (first to last waypoint connection is automatic for tri)
            Handles.DrawLine(waypoints[2], waypoints[0]);
        }
    }

    private bool IsWithinSelectionBounds(Vector3 position)
    {
        if (selectionShapeMode == SelectionShape.Rectangle || selectionShapeMode == SelectionShape.Triangle)
        {
            // Use a basic bounding box check
            return position.x >= selectionPosition.x - selectionScale.x / 2 &&
                   position.x <= selectionPosition.x + selectionScale.x / 2 &&
                   position.z >= selectionPosition.z - selectionScale.z / 2 &&
                   position.z <= selectionPosition.z + selectionScale.z / 2;
        }
        else if (selectionShapeMode == SelectionShape.FreeDraw)
        {
            // Use a point-in-polygon test for free-draw shapes
            return IsPointInPolygon(position, waypoints);
        }
        return false;
    }

    private bool IsPointInPolygon(Vector3 point, List<Vector3> polygon)
    {
        bool isInside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            if (((polygon[i].z > point.z) != (polygon[j].z > point.z)) &&
                (point.x < (polygon[j].x - polygon[i].x) * (point.z - polygon[i].z) / (polygon[j].z - polygon[i].z) + polygon[i].x))
            {
                isInside = !isInside;
            }
        }
        return isInside;
    }

    private void ScatterPrefabs(Layer layer)
    {
        if (layer.prefabs.Count == 0) return;

        int scatterCount = 100; // Adjust based on desired density
        for (int i = 0; i < scatterCount; i++)
        {
            Vector3 randomPos = new Vector3(
                Random.Range(selectionPosition.x - selectionScale.x / 2, selectionPosition.x + selectionScale.x / 2),
                0,
                Random.Range(selectionPosition.z - selectionScale.z / 2, selectionPosition.z + selectionScale.z / 2)
            );

            if (useSelectionBounds && !IsWithinSelectionBounds(randomPos)) continue;

            RaycastHit hit;
            if (Physics.Raycast(randomPos + Vector3.up * 1000, Vector3.down, out hit))
            {
                Vector3 position = hit.point;
                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                GameObject prefab = layer.prefabs[Random.Range(0, layer.prefabs.Count)];
                GameObject instance = Instantiate(prefab, position, rotation);

                float randomScale = Random.Range(minSizeVariation, maxSizeVariation);
                instance.transform.localScale = Vector3.one * randomScale;

                if (layer.avoidanceEnabled && IsOverlappingAvoidance(instance, layer))
                {
                    DestroyImmediate(instance);
                    continue;
                }
            }
        }
    }

    private void PaintPrefabs(Layer layer)
    {
        if (layer.prefabs.Count == 0) return;

        GameObject prefab = layer.prefabs[Random.Range(0, layer.prefabs.Count)];
        RaycastHit hit;
        if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(Event.current.mousePosition), out hit))
        {
            if (useSelectionBounds && !IsWithinSelectionBounds(hit.point)) return;

            Vector3 position = hit.point;
            Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            GameObject instance = Instantiate(prefab, position, rotation);

            float randomScale = Random.Range(minSizeVariation, maxSizeVariation);
            instance.transform.localScale = Vector3.one * randomScale;

            if (layer.avoidanceEnabled && IsOverlappingAvoidance(instance, layer))
            {
                DestroyImmediate(instance);
            }
        }
    }

    private bool IsOverlappingAvoidance(GameObject instance, Layer layer)
    {
        Collider[] hitColliders = Physics.OverlapSphere(instance.transform.position, 1f);
        foreach (var collider in hitColliders)
        {
            foreach (var avoidancePrefab in layer.avoidancePrefabs)
            {
                if (collider.gameObject.name.Contains(avoidancePrefab.name))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private class Layer
    {
        public string name;
        public List<GameObject> prefabs;
        public bool avoidanceEnabled = false;
        public List<GameObject> avoidancePrefabs = new List<GameObject>();
    }
}
