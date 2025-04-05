using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

// Define an enum for building types to avoid string errors
public enum BuildingType
{
    Radio,
    FOB,
    AmmoCrate
}

public class BuildSystem : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RMF_RadialMenu buildMenu;
    [SerializeField] private InventoryEvents inventoryEvents;
    [SerializeField] public Camera playerCamera; // Made public for direct assignment
    [SerializeField] private PlayerStateManager playerStateManager; // Reference to our new player state manager
    
    [Header("Building Prefabs")]
    [SerializeField] private GameObject radioPrefab;  
    [SerializeField] private GameObject fobPrefab;    
    [SerializeField] private GameObject ammoCratePrefab;
    
    [Header("Placement Settings")]
    [SerializeField] private float radioDistance = 1.5f;
    [SerializeField] private float fobDistance = 5f;
    [SerializeField] private float ammocrateDistance = 1f;
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
    private Dictionary<BuildingType, GameObject> buildingPrefabs = new Dictionary<BuildingType, GameObject>();
    private Dictionary<BuildingType, float> buildingDistances = new Dictionary<BuildingType, float>();
    private bool isProcessingSelection = false;
    private BuildingType lastSelectedType;
    private bool isValidPlacement = true;      // Track if current placement is valid
    
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
        
        // Register building prefabs
        buildingPrefabs.Add(BuildingType.Radio, radioPrefab);
        buildingPrefabs.Add(BuildingType.FOB, fobPrefab);
        buildingPrefabs.Add(BuildingType.AmmoCrate, ammoCratePrefab);
        
        // Register distances
        buildingDistances.Add(BuildingType.Radio, radioDistance);
        buildingDistances.Add(BuildingType.FOB, fobDistance);
        buildingDistances.Add(BuildingType.AmmoCrate, ammocrateDistance);

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
        if (radioPrefab == null)
        {
            Debug.LogWarning("BuildSystem: Radio prefab is not assigned! Radio buildings will be represented by placeholder cubes.");
        }
        
        if (fobPrefab == null)
        {
            Debug.LogWarning("BuildSystem: FOB prefab is not assigned! FOB buildings will be represented by placeholder cubes.");
        }
        
        if (ammoCratePrefab == null)
        {
            Debug.LogWarning("BuildSystem: Ammo Crate prefab is not assigned! Ammo Crate buildings will be represented by placeholder cubes.");
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
    
    // This is the event handler - renamed to clarify separation of concerns
    private void HandleBuildingSelected(string buildingTypeStr)
    {
        // Skip processing if we're already handling a selection
        if (isProcessingSelection) return;
        
        // Try to parse the string as a BuildingType enum
        if (System.Enum.TryParse(buildingTypeStr, out BuildingType buildingType))
        {
            // Check if this is the same as our last selection - if so, skip it
            if (buildingType == lastSelectedType && currentPreview != null)
            {
                return;
            }
            
            // Actually select the building type
            SelectBuildingItem(buildingType);
        }
        else
        {
            Debug.LogError($"Invalid building type string: {buildingTypeStr}");
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
        
        BuildingType currentType = BuildingType.Radio; // Default
        
        // Get current type from the BuildPreview component
        BuildPreview preview = currentPreview.GetComponent<BuildPreview>();
        if (preview != null)
        {
            currentType = preview.buildingType;
        }
        else
        {
            // Try to determine from name if component not found
            string previewName = currentPreview.name;
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                if (previewName.Contains(type.ToString()))
                {
                    currentType = type;
                    break;
                }
            }
        }
        
        // Position preview
        PositionObject(currentPreview, currentType);
    }
    
    // Called by Unity Input System through the PlayerInput component
    public void OnToggleBuild(InputValue value)
    {
        // Only respond to the key being pressed
        if (value.isPressed)
        {
            Debug.Log("Build toggle key pressed");
            isKeyPressed = true;
            if (!isInBuildMode)
            {
                EnterBuildMode();
            }
        }
        else
        {
            // Key released
            Debug.Log("Build toggle key released");
            isKeyPressed = false;
            if (!isInBuildMode || currentPreview == null)
            {
                ExitBuildMode();
            }
        }
    }

    // For backward compatibility
    public void OnToggleBuild()
    {
        // Toggle build mode
        if (!isInBuildMode)
        {
            isKeyPressed = true;
            EnterBuildMode();
        }
        else
        {
            // If already in build mode, exit
            CancelBuilding();
        }
    }
    
    private void EnterBuildMode()
    {
        if (isInBuildMode)
        {
            Debug.Log("Already in build mode, ignoring request to enter build mode");
            return;
        }
        
        Debug.Log("Entering build mode");
        isInBuildMode = true;
        
        // Reset rotation for new build session
        currentRotation = 0f;
        
        // Use the new state manager if available, otherwise fallback to old system
        if (playerStateManager != null)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.BuildingMenu);
        }
        else if (inputStateManager != null)
        {
            // Legacy support
            inputStateManager.SetState(InputState.BuildMode);
            inputStateManager.SetBuildMenuOpen(true);
            
            // Show cursor and disable camera look
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.Confined;
        }
        
        // Show radial menu
        if (buildMenu != null)
        {
            buildMenu.gameObject.SetActive(true);
            
            // Ensure it's visible by enabling the canvas
            Canvas canvas = buildMenu.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }
    }
    
    private void ExitBuildMode()
    {
        if (!isInBuildMode) return;
        
        // First clear any preview
        ClearPreview();
        
        isInBuildMode = false;
        
        // Hide radial menu
        if (buildMenu != null)
        {
            buildMenu.gameObject.SetActive(false);
        }
        
        // Use the new state manager if available, otherwise fallback to old system
        if (playerStateManager != null)
        {
            playerStateManager.SetState(PlayerStateManager.PlayerState.Normal);
        }
        else if (inputStateManager != null)
        {
            // Legacy support
            inputStateManager.SetState(InputState.Normal);
            
            // Hide cursor and lock it
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else if (inventoryEvents != null)
        {
            // Direct event if nothing else is available
            inventoryEvents.RaiseInputStateChanged(InputState.Normal);
            
            // Hide cursor and lock it
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
    
    // This method is called directly from the RMF_RadialMenuElement buttons
    public void SelectBuildingItem(BuildingType buildingType)
    {
        // Set a flag to prevent duplicate processing 
        isProcessingSelection = true;
        lastSelectedType = buildingType;
        
        try
        {
            // Early exit if we're already showing this building type's preview
            if (currentPreview != null && currentPreview.name.Contains(buildingType.ToString()))
            {
                return;
            }
            
            // Try to find prefab in dictionary
            if (!buildingPrefabs.TryGetValue(buildingType, out selectedPrefab))
            {
                // If not in dictionary, check the prefab fields directly
                switch (buildingType)
                {
                    case BuildingType.Radio:
                        selectedPrefab = radioPrefab;
                        break;
                    case BuildingType.FOB:
                        selectedPrefab = fobPrefab;
                        break;
                    case BuildingType.AmmoCrate:
                        selectedPrefab = ammoCratePrefab;
                        break;
                }
                
                // If still null, log a warning
                if (selectedPrefab == null)
                {
                    Debug.LogWarning($"No prefab assigned for building type {buildingType}. A placeholder will be used.");
                }
            }
            
            // Continue even if selectedPrefab is null - the CreateBasicPreview method handles the fallback
            CreateBasicPreview(buildingType);
            
            // Hide menu but stay in build mode
            if (buildMenu != null)
            {
                buildMenu.gameObject.SetActive(false);
            }
            
            // Return to normal input state but stay in build mode for placement
            // This allows player to look around while placing
            RestoreNormalControls();
        }
        finally
        {
            // Reset the flag when we're done
            isProcessingSelection = false;
        }
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
    
    private void CreateBasicPreview(BuildingType buildingType)
    {
        // First make sure any existing preview is cleared
        ClearPreview();
        
        // Only continue if we have a valid prefab
        if (selectedPrefab == null) return;
        
        try
        {
            // Create a fresh parent container for the preview
            GameObject previewContainer = new GameObject($"PreviewContainer_{buildingType}");
            currentPreview = previewContainer;
            
            // Create the actual preview object as a child
            GameObject previewModel;
            
            // Use the actual prefab for all building types
            previewModel = Instantiate(selectedPrefab, previewContainer.transform);
            
            if (previewModel == null)
            {
                // Fallback to primitive cube if instantiation fails
                Debug.LogWarning($"Failed to instantiate prefab for {buildingType}. Creating fallback cube.");
                previewModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                previewModel.transform.SetParent(previewContainer.transform);
                
                // Scale based on type
                switch (buildingType)
                {
                    case BuildingType.FOB:
                        previewModel.transform.localScale = new Vector3(3f, 1f, 3f);
                        break;
                    case BuildingType.AmmoCrate:
                        previewModel.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        break;
                    default:
                        previewModel.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
                        break;
                }
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
            previewContainer.name = $"Preview_{buildingType}";
            
            // Add BuildPreview helper component to make preview more resilient
            BuildPreview previewHelper = previewContainer.AddComponent<BuildPreview>();
            previewHelper.buildingType = buildingType;
            
            // Position the preview
            PositionObject(previewContainer, buildingType);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating preview: {e.Message}\n{e.StackTrace}");
        }
    }
    
    // Helper method to position an object directly without relying on camera logic
    private void PositionObject(GameObject obj, BuildingType buildingType)
    {
        if (obj == null) return;
        
        // Get the appropriate distance for this building type
        float distance = 2f;
        if (buildingDistances != null && buildingDistances.TryGetValue(buildingType, out float typedDistance))
        {
            distance = typedDistance;
        }
        
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
    public BuildingType buildingType;
} 