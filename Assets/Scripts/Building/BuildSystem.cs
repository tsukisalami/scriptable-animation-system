using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Serializable class for building entries
[System.Serializable]
public class BuildingEntry
{
    [Tooltip("The name/identifier of the building type")]
    public string buildingType;
    
    [Tooltip("The prefab for this building type")]
    public GameObject prefab;
    
    [Tooltip("The placement distance for this building type")]
    public float placementDistance = 2f;
}

public class BuildSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RMF_RadialMenu buildMenu;
    [SerializeField] private InventoryEvents inventoryEvents;
    [SerializeField] public Camera playerCamera; // Made public for direct assignment
    [SerializeField] private PlayerStateManager playerStateManager; // Reference to our new player state manager
    
    [Header("Building Types")]
    [Tooltip("Define your building types here with their corresponding prefabs")]
    public List<BuildingEntry> buildingEntries = new List<BuildingEntry>();
    
    [Header("Placement Settings")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private Material invalidPlacementMaterial;  // Material for invalid placement
    [SerializeField] private LayerMask collisionCheckLayers;    // Layers that make placement invalid
    [SerializeField] private float rotationSpeed = 45f; // Rotation speed in degrees per second
    
    // State tracking
    private bool isInBuildMode = false;
    private bool isKeyPressed = false;
    private GameObject currentPreview;
    private GameObject selectedPrefab;
    private InputStateManager inputStateManager;
    private float currentRotation = 0f; // Current rotation offset in degrees
    private bool isProcessingSelection = false;
    private bool isValidPlacement = true;      // Track if current placement is valid
    private string currentBuildingTypeStr = ""; // Track current building type as string
    
    private void Start()
    {
        inputStateManager = GetComponent<InputStateManager>();
        
        // Try to find the PlayerStateManager if not already assigned
        if (playerStateManager == null)
        {
            playerStateManager = GetComponent<PlayerStateManager>();
            
            // If still null, try to find in parent or scene
            if (playerStateManager == null)
            {
                playerStateManager = GetComponentInParent<PlayerStateManager>();
                
                if (playerStateManager == null)
                {
                    playerStateManager = FindObjectOfType<PlayerStateManager>();
                    
                    if (playerStateManager == null)
                    {
                        Debug.LogWarning("BuildSystem: PlayerStateManager not found! Falling back to legacy input system.");
                    }
                }
            }
        }
        
        // Validate prefabs and show helpful warnings
        ValidateBuildingPrefabs();

        // Make sure the menu is always hidden at start
        if (buildMenu != null)
        {
            buildMenu.gameObject.SetActive(false);
            
            // Also ensure canvas is inactive but ready
            Canvas canvas = buildMenu.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }
        
        if (inventoryEvents != null)
        {
            // Subscribe to building selection event
            inventoryEvents.OnBuildingSelected += HandleBuildingSelected;
        }
        
        // Ensure initial state is not in build mode
        isInBuildMode = false;
        isKeyPressed = false;
        
        // Explicitly initialize as not in build mode to prevent initial state confusion
        if (playerStateManager != null)
        {
            // Ensure we start in normal state - but only if we're not already in a different state
            var currentState = playerStateManager.GetCurrentState();
            if (currentState == PlayerStateManager.PlayerState.BuildingMenu || 
                currentState == PlayerStateManager.PlayerState.Building)
            {
                playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
            }
        }
        else if (inputStateManager != null)
        {
            // Only reset to normal if we're in BuildMode
            if (inputStateManager.GetCurrentState() == InputState.BuildMode)
            {
                inputStateManager.SetState(InputState.Normal);
            }
        }
    }
    
    // Helper method to validate all building prefabs are assigned
    private void ValidateBuildingPrefabs()
    {
        // Check if building entries list has items
        if (buildingEntries.Count == 0)
        {
            Debug.LogWarning("BuildSystem: No building entries defined. Consider adding some building types to the buildingEntries list.");
        }
        else
        {
            // Validate entries in the list
            foreach (var entry in buildingEntries)
            {
                if (string.IsNullOrEmpty(entry.buildingType))
                {
                    Debug.LogWarning("BuildSystem: Found entry with empty building type!");
                }
                
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"BuildSystem: Prefab is missing for building type: {entry.buildingType}");
                }
            }
        }
        
        // Validate materials
        if (previewMaterial == null)
        {
            Debug.LogWarning("BuildSystem: Preview Material is not assigned! Fallback transparent blue material will be used.");
        }
        
        if (invalidPlacementMaterial == null)
        {
            Debug.LogWarning("BuildSystem: Invalid Placement Material is not assigned! Fallback transparent red material will be used.");
        }
    }
    
    // Get a building entry by its type string
    public BuildingEntry GetBuildingEntryByType(string typeString)
    {
        if (string.IsNullOrEmpty(typeString)) return null;
        
        // Look for matching entry in our list
        foreach (var entry in buildingEntries)
        {
            if (entry.buildingType.Equals(typeString, System.StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }
        
        return null;
    }
    
    // This is the event handler - renamed to clarify separation of concerns
    private void HandleBuildingSelected(string buildingTypeStr)
    {
        // Skip processing if we're already handling a selection
        if (isProcessingSelection) return;
        
        // Store the building type string
        currentBuildingTypeStr = buildingTypeStr;
        
        // Check if we can find the building type in our entries
        BuildingEntry entry = GetBuildingEntryByType(buildingTypeStr);
        
        if (entry != null)
        {
            // Found the building type in our entries, use it directly
            SelectBuildingByEntry(entry);
        }
        else
        {
            Debug.LogError($"Building type not found: {buildingTypeStr}");
        }
    }
    
    private void Update()
    {
        if (isInBuildMode && currentPreview != null)
        {
            // Handle rotation with Q/E keys
            if (Input.GetKey(KeyCode.Q))
            {
                currentRotation -= rotationSpeed * Time.deltaTime;
                PositionCurrentPreview(); // Update position and rotation
            }
            else if (Input.GetKey(KeyCode.E))
            {
                currentRotation += rotationSpeed * Time.deltaTime;
                PositionCurrentPreview(); // Update position and rotation
            }
            
            // Place building on left click (only when menu is closed)
            if (Input.GetMouseButtonDown(0) && (buildMenu == null || !buildMenu.gameObject.activeSelf))
            {
                // Only place if the position is valid
                if (isValidPlacement)
                {
                    PlaceBuilding();
                }
                return; // Exit early to avoid further processing
            }
            
            // Cancel building on right click
            if (Input.GetMouseButtonDown(1))
            {
                CancelBuilding();
                return; // Exit early to avoid further processing
            }
            
            // Reposition the preview to follow the player
            if (playerCamera != null)
            {
                PositionCurrentPreview();
            }
        }
        
        // Handle key release manually since we can't rely on context.canceled
        if (isKeyPressed && Input.GetKeyUp(KeyCode.T))
        {
            isKeyPressed = false;
            
            if (!isInBuildMode || currentPreview == null)
            {
                ExitBuildMode();
            }
        }
    }
    
    // Position the current preview using the current building type
    private void PositionCurrentPreview()
    {
        if (currentPreview == null) return;
        
        // Default values
        string currentTypeStr = "";
        float distance = 2f;
        
        // Get current type from the BuildPreview component
        BuildPreview preview = currentPreview.GetComponent<BuildPreview>();
        if (preview != null)
        {
            // Get the type information
            currentTypeStr = preview.buildingTypeStr;
            
            // Try to find a matching entry for the distance
            BuildingEntry entry = GetBuildingEntryByType(currentTypeStr);
            if (entry != null)
            {
                distance = entry.placementDistance;
                
                // Use string-based positioning
                PositionObjectByString(currentPreview, currentTypeStr, distance);
                return;
            }
        }
        else
        {
            // Try to determine from name if component not found
            string previewName = currentPreview.name;
            
            // Try string-based first
            foreach (var entry in buildingEntries)
            {
                if (previewName.Contains(entry.buildingType))
                {
                    PositionObjectByString(currentPreview, entry.buildingType, entry.placementDistance);
                    return;
                }
            }
        }
        
        // Fallback to a default distance if we couldn't determine the type
        PositionObjectByString(currentPreview, currentTypeStr, distance);
    }
    
    // Called by Unity Input System through the PlayerInput component
    public void OnToggleBuild(InputValue value)
    {
        if (value.isPressed)
        {
            isKeyPressed = true;
            if (!isInBuildMode)
            {
                EnterBuildMode();
            }
            // If already in build mode, pressing T again does nothing until released
        }
        else // Key released
        {
            isKeyPressed = false;
            // Exit build mode only if we are not currently placing an item (preview is null)
            if (isInBuildMode && currentPreview == null)
            {
                ExitBuildMode();
            }
        }
    }

    // For backward compatibility - simplified toggle logic
    public void OnToggleBuild()
    {
        if (!isInBuildMode)
        {
            EnterBuildMode();
        }
        else
        {
            // If already in build mode, just exit (or cancel if placing)
            if (currentPreview != null)
                CancelBuilding();
            else
                ExitBuildMode();
        }
    }
    
    private void EnterBuildMode()
    {
        if (isInBuildMode) return;
        isInBuildMode = true;
        currentRotation = 0f;
        
        if (playerStateManager != null)
            playerStateManager.SetState(PlayerStateManager.PlayerState.BuildingMenu);
        
        if (buildMenu != null)
        {
            Canvas canvas = buildMenu.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = true;
            if (!buildMenu.gameObject.activeSelf) buildMenu.gameObject.SetActive(true);
        }
    }
    
    private void ExitBuildMode()
    {
        if (!isInBuildMode) return;
        ClearPreview();
        isInBuildMode = false;
        
        if (buildMenu != null)
        {
            // Deactivate the main menu
            buildMenu.gameObject.SetActive(false);
            
            // Find and deactivate ANY active radial menu (including sub-menus)
            RMF_RadialMenu[] allMenus = FindObjectsOfType<RMF_RadialMenu>();
            foreach(RMF_RadialMenu menu in allMenus)
            {
                if (menu.gameObject.activeSelf)
                    menu.gameObject.SetActive(false);
            }

            // Optionally disable the parent canvas
            Canvas canvas = buildMenu.GetComponentInParent<Canvas>();
            if (canvas != null) canvas.enabled = false;
        }
        
        if (playerStateManager != null)
            playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
    }
    
    // Helper method to restore normal controls but stay in build mode
    private void RestoreNormalControls()
    {
        // Use the new state manager if available
        if (playerStateManager != null)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.Building);
        }
        else
        {
            // Legacy support - hide cursor and lock it to allow camera movement
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            
            // Keep tracking that we're in build mode, but allow normal camera controls
            if (inputStateManager != null)
            {
                // Keep build mode active at the system level, but enable normal gameplay controls
                inputStateManager.SetState(InputState.BuildMode);
                inputStateManager.SetBuildMenuOpen(false);
            }
            else if (inventoryEvents != null)
            {
                // Tell other systems we're in normal input state (for camera movement)
                // but we're still technically in build mode for our own tracking
                inventoryEvents.RaiseInputStateChanged(InputState.BuildMode);
            }
        }
    }
    
    // New method that selects a building by entry
    private void SelectBuildingByEntry(BuildingEntry entry)
    {
        if (entry == null) return;
        
        isProcessingSelection = true;
        try
        {
            // Early exit if we're already showing this building type's preview
            if (currentPreview != null && currentPreview.name.Contains(entry.buildingType))
            {
                return;
            }
            
            selectedPrefab = entry.prefab;
            
            // Continue even if selectedPrefab is null - the CreateBasicPreview method handles the fallback
            CreateBasicPreviewFromString(entry.buildingType, entry.placementDistance);
            
            // Hide all menus (including sub-menus) but stay in build mode
            if (buildMenu != null)
            {
                buildMenu.gameObject.SetActive(false);
                
                // Find and deactivate ANY active radial menu (including sub-menus)
                RMF_RadialMenu[] allMenus = FindObjectsOfType<RMF_RadialMenu>();
                foreach(RMF_RadialMenu menu in allMenus)
                {
                    if (menu.gameObject.activeSelf)
                        menu.gameObject.SetActive(false);
                }
            }
            
            // Return to normal input state but stay in build mode for placement
            RestoreNormalControls();
        }
        finally
        {
            isProcessingSelection = false;
        }
    }
    
    // Select a building by type string (for direct use by RMF_RadialMenuElement)
    public void SelectBuildingByTypeString(string buildingTypeStr)
    {
        if (string.IsNullOrEmpty(buildingTypeStr)) return;
        
        // Store the string for later reference
        currentBuildingTypeStr = buildingTypeStr;
        
        // Try to find in building entries
        BuildingEntry entry = GetBuildingEntryByType(buildingTypeStr);
        if (entry != null)
        {
            SelectBuildingByEntry(entry);
            return;
        }
        
        Debug.LogError($"Building type not found: {buildingTypeStr}");
    }

    private void CreateBasicPreviewFromString(string buildingTypeStr, float placementDistance)
    {
        // First make sure any existing preview is cleared
        ClearPreview();
        
        // Only continue if we have a valid prefab
        if (selectedPrefab == null) return;
        
        try
        {
            // Create a fresh parent container for the preview
            GameObject previewContainer = new GameObject($"PreviewContainer_{buildingTypeStr}");
            currentPreview = previewContainer;
            
            // Create the actual preview object as a child
            GameObject previewModel;
            
            // Use the actual prefab
            previewModel = Instantiate(selectedPrefab, previewContainer.transform);
            
            if (previewModel == null)
            {
                // Fallback to primitive cube if instantiation fails
                Debug.LogWarning($"Failed to instantiate prefab for {buildingTypeStr}. Creating fallback cube.");
                previewModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                previewModel.transform.SetParent(previewContainer.transform);
                
                // Set a default scale
                previewModel.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            }
            
            previewModel.transform.localPosition = Vector3.zero;
            
            // Apply a holographic material to all renderers
            Renderer[] renderers = previewModel.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                // Simple material assignment - just set the shader
                if (previewMaterial != null)
                {
                    renderer.material = previewMaterial;
                }
                else
                {
                    // Basic blue translucent material as fallback
                    renderer.material.shader = Shader.Find("Standard");
                    renderer.material.color = new Color(0.5f, 0.7f, 1f, 0.5f);
                }
            }
            
            // Disable all colliders
            foreach (Collider collider in previewModel.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            
            // Set name for identification
            previewContainer.name = $"Preview_{buildingTypeStr}";
            
            // Add BuildPreview helper component to make preview more resilient
            BuildPreview previewHelper = previewContainer.AddComponent<BuildPreview>();
            previewHelper.buildingTypeStr = buildingTypeStr;
            
            // Position the preview
            PositionObjectByString(previewContainer, buildingTypeStr, placementDistance);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating preview: {e.Message}\n{e.StackTrace}");
        }
    }

    // Helper method to position an object using a string building type
    private void PositionObjectByString(GameObject obj, string buildingTypeStr, float distance)
    {
        if (obj == null) return;
        
        // Use either the camera or this transform
        Transform referenceTransform = (playerCamera != null) ? playerCamera.transform : this.transform;
        
        // Get forward direction (excluding Y component)
        Vector3 forward = referenceTransform.forward;
        Vector3 forwardNoY = new Vector3(forward.x, 0, forward.z).normalized;
        
        // If forward direction is invalid, use a fallback
        if (forwardNoY.magnitude < 0.01f)
        {
            forwardNoY = Vector3.forward;
        }
        
        // Calculate position in front of reference
        Vector3 position = referenceTransform.position + forwardNoY * distance;
        
        // Use character's center of mass height for raycasting (approximately waist height)
        Vector3 characterCenter = transform.position + Vector3.up * 1.0f; // Approximating waist height
        Vector3 raycastOrigin = new Vector3(characterCenter.x, characterCenter.y, characterCenter.z);
        Vector3 raycastDirection = forwardNoY;
        
        // Set the position
        obj.transform.position = position;
        
        // Set Y rotation to match reference transform PLUS rotation offset
        obj.transform.rotation = Quaternion.Euler(0, referenceTransform.eulerAngles.y + currentRotation, 0);
        
        // Try to adjust height based on ground using improved raycasting
        RaycastHit hit;
        bool foundValidSurface = false;
        
        // Cast multiple rays to find valid placement surface - start with direct ray
        if (Physics.Raycast(raycastOrigin, raycastDirection, out hit, distance * 2f, groundMask))
        {
            // If hit something in front, place on that surface
            Vector3 onGroundPos = new Vector3(hit.point.x, hit.point.y, hit.point.z);
            obj.transform.position = onGroundPos;
            foundValidSurface = true;
        }
        else if (Physics.Raycast(position + Vector3.up * 2f, Vector3.down, out hit, 5f, groundMask))
        {
            // Fallback to downward ray from original position
            Vector3 onGroundPos = new Vector3(position.x, hit.point.y, position.z);
            obj.transform.position = onGroundPos;
            foundValidSurface = true;
        }
        else if (Physics.Raycast(raycastOrigin, Vector3.down, out hit, 5f, groundMask)) 
        {
            // Last resort - raycast directly down from character
            Vector3 onGroundPos = new Vector3(position.x, hit.point.y, position.z);
            obj.transform.position = onGroundPos;
            foundValidSurface = true;
        }
        
        // Check for collisions with other objects to determine valid placement
        isValidPlacement = foundValidSurface;
        
        // If we have a collision check layer, do the check
        if (collisionCheckLayers != 0)
        {
            // Get the bounds of the object for overlap check
            Bounds bounds = GetObjectBounds(obj);
            
            // Check if there are any colliders inside our bounds
            Collider[] colliders = Physics.OverlapBox(
                bounds.center, 
                bounds.extents * 0.9f, // Slightly smaller to prevent false positives
                obj.transform.rotation,
                collisionCheckLayers
            );
            
            // If any colliders were found, mark placement as invalid
            if (colliders.Length > 0)
            {
                isValidPlacement = false;
            }
        }
        
        // Apply the appropriate material based on placement validity
        ApplyPlacementMaterial(obj, isValidPlacement);
    }
    
    // Helper method to get the combined bounds of an object and all its children
    private Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.zero);
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        if (renderers.Length > 0)
        {
            // Initialize with the first renderer's bounds
            bounds = renderers[0].bounds;
            
            // Encapsulate all other renderers
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }
        else
        {
            // Fallback if no renderers are found
            bounds = new Bounds(obj.transform.position, Vector3.one);
        }
        
        return bounds;
    }
    
    // Helper method to apply the appropriate material based on placement validity
    private void ApplyPlacementMaterial(GameObject obj, bool isValid)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (isValid)
            {
                // Apply valid placement material
                if (previewMaterial != null)
                {
                    renderer.material = previewMaterial;
                }
                else
                {
                    // Basic blue translucent material as fallback
                    renderer.material.shader = Shader.Find("Standard");
                    renderer.material.color = new Color(0.5f, 0.7f, 1f, 0.5f);
                }
            }
            else
            {
                // Apply invalid placement material
                if (invalidPlacementMaterial != null)
                {
                    renderer.material = invalidPlacementMaterial;
                }
                else
                {
                    // Basic red translucent material as fallback
                    renderer.material.shader = Shader.Find("Standard");
                    renderer.material.color = new Color(1f, 0.3f, 0.3f, 0.5f);
                }
            }
        }
    }
    
    private void PlaceBuilding()
    {
        if (currentPreview == null || selectedPrefab == null || !isValidPlacement) return;
        
        // Get current position and rotation of preview
        Vector3 position = currentPreview.transform.position;
        Quaternion rotation = currentPreview.transform.rotation;
        
        // First, clear the preview
        ClearPreview();
        
        // Create the actual building
        GameObject building = Instantiate(selectedPrefab, position, rotation);
        
        // Exit build mode (which will handle cursor visibility)
        ExitBuildMode();
    }
    
    private void CancelBuilding()
    {
        ClearPreview();
        ExitBuildMode();
    }
    
    private void ClearPreview()
    {
        // Clear preview
        if (currentPreview != null)
        {
            // First disable the GameObject to ensure it's not visible
            currentPreview.SetActive(false);
            
            // Destroy all child objects first to prevent any lingering references
            foreach (Transform child in currentPreview.transform)
            {
                Destroy(child.gameObject);
            }
            
            // Destroy the preview object
            Destroy(currentPreview);
            currentPreview = null;
        }
    }
    
    private void OnDestroy()
    {
        // Clean up and unsubscribe from events
        ClearPreview();
        
        if (inventoryEvents != null)
        {
            inventoryEvents.OnBuildingSelected -= HandleBuildingSelected;
        }
    }
    
    // Handle building rotation input
    public void OnRotateBuildingLeft(InputValue value)
    {
        if (!isInBuildMode || currentPreview == null) return;
        
        if (value.isPressed)
        {
            currentRotation -= rotationSpeed * Time.deltaTime * 10;
            PositionCurrentPreview();
        }
    }
    
    public void OnRotateBuildingRight(InputValue value)
    {
        if (!isInBuildMode || currentPreview == null) return;
        
        if (value.isPressed)
        {
            currentRotation += rotationSpeed * Time.deltaTime * 10;
            PositionCurrentPreview();
        }
    }
}

// Add helper component to make preview objects more identifiable and resilient
public class BuildPreview : MonoBehaviour
{
    // New string-based identifier
    public string buildingTypeStr;
} 