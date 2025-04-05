using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[AddComponentMenu("Radial Menu Framework/RMF Element")]
public class RMF_RadialMenuElement : MonoBehaviour {

    // Element type enum
    public enum ElementType
    {
        Building,   // Spawns a prefab in build mode
        Folder,     // Opens a sub-menu
        Back,       // Returns to parent menu
        Marker      // Places a marker (to be implemented later)
    }

    [Header("Element Configuration")]
    [Tooltip("The type of element this is")]
    public ElementType elementType = ElementType.Building;

    [HideInInspector]
    public RectTransform rt;
    [HideInInspector]
    public RMF_RadialMenu parentRM;

    [Tooltip("Each radial element needs a button. This is generally a child one level below this primary radial element game object.")]
    public Button button;

    [Tooltip("This is the text label that will appear in the center of the radial menu when this option is moused over. Best to keep it short.")]
    public string label;

    [Header("Building Settings")]
    [Tooltip("The type of building this menu option will create (as a string)")]
    public string buildingType;

    [Header("Sub-Menu Settings")]
    [Tooltip("Reference to the sub-menu that will be opened when this element is clicked (for Folder type), or the parent menu (for Back type)")]
    public RMF_RadialMenu targetSubmenu;

    [HideInInspector]
    public float angleMin, angleMax;

    [HideInInspector]
    public float angleOffset;

    [HideInInspector]
    public bool active = false;

    [HideInInspector]
    public int assignedIndex = 0;

    private CanvasGroup cg;

    void Awake() {
        rt = gameObject.GetComponent<RectTransform>();
        if (gameObject.GetComponent<CanvasGroup>() == null)
            cg = gameObject.AddComponent<CanvasGroup>();
        else
            cg = gameObject.GetComponent<CanvasGroup>();

        if (rt == null) Debug.LogError("Radial Menu: Rect Transform missing on " + gameObject.name);
        if (button == null) Debug.LogError("Radial Menu: Button missing on " + gameObject.name);
    }

    void Start () {
        rt.rotation = Quaternion.Euler(0, 0, -angleOffset);

        if (parentRM.useLazySelection)
            cg.blocksRaycasts = false;
        else {
            EventTrigger t = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            if (t.triggers == null) t.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((eventData) => { setParentMenuLable(label); });

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((eventData) => { setParentMenuLable(""); });

            t.triggers.Add(enter);
            t.triggers.Add(exit);
        }

        if (button != null) {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }
	
    public void setAllAngles(float offset, float baseOffset) {
        angleOffset = offset;
        angleMin = offset - (baseOffset / 2f);
        angleMax = offset + (baseOffset / 2f);
    }

    public void highlightThisElement(PointerEventData p) {
        ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.selectHandler);
        active = true;
        setParentMenuLable(label);
    }

    public void setParentMenuLable(string l) {
        if (parentRM.textLabel != null)
            parentRM.textLabel.text = l;
    }

    public void unHighlightThisElement(PointerEventData p) {
        ExecuteEvents.Execute(button.gameObject, p, ExecuteEvents.deselectHandler);
        active = false;
        if (!parentRM.useLazySelection) setParentMenuLable(" ");
    }

    public void OnButtonClicked() {
        switch (elementType) {
            case ElementType.Building: HandleBuildingSelection(); break;
            case ElementType.Folder: OpenSubmenu(); break;
            case ElementType.Back: ReturnToPreviousMenu(); break;
            case ElementType.Marker: /* Marker placement to be implemented */ break;
        }
    }
    
    private void HandleBuildingSelection() {
        if (parentRM.buildSystem != null) {
            // First deactivate ALL active radial menus
            RMF_RadialMenu[] allMenus = FindObjectsOfType<RMF_RadialMenu>();
            foreach (RMF_RadialMenu menu in allMenus) {
                if (menu.gameObject.activeSelf) {
                    menu.gameObject.SetActive(false);
                }
            }
            
            // Then proceed with building selection
            parentRM.buildSystem.SelectBuildingByTypeString(buildingType);
        }
    }
    
    // Simplified submenu opening
    private void OpenSubmenu() {
        if (targetSubmenu != null && parentRM != null) {
            parentRM.gameObject.SetActive(false);
            targetSubmenu.gameObject.SetActive(true);
        }
    }
    
    // Simplified return to previous menu
    private void ReturnToPreviousMenu() {
        if (targetSubmenu != null && parentRM != null) { // targetSubmenu is the parent menu here
            parentRM.gameObject.SetActive(false);
            targetSubmenu.gameObject.SetActive(true);
        }
    }

    public void clickMeTest() {
        // Empty method
    }
}
