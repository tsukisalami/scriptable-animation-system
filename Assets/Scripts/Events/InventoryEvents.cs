using UnityEngine;

public enum InputState
{
    Normal,           // Regular gameplay
    HotbarActive,     // When hotbar is visible
    AttachmentMode,   // When modifying weapon attachments
    ActionLocked      // During animations or other locked states
}

[CreateAssetMenu(fileName = "InventoryEvents", menuName = "WW2/Events/InventoryEvents")]
public class InventoryEvents : ScriptableObject
{
    // Category/Item Events
    public event System.Action<int> OnCategorySelected;        // categoryIndex
    public event System.Action<int, int> OnItemSelected;       // categoryIndex, itemIndex
    public event System.Action<int> OnItemConsumed;           // categoryIndex
    public event System.Action<InputState> OnInputStateChanged;
    
    // Attachment Events
    public event System.Action<int> OnAttachmentModeChanged;  // attachmentIndex
    public event System.Action OnAttachmentModeExit;
    
    // Methods to raise events
    public void RaiseCategorySelected(int categoryIndex) => OnCategorySelected?.Invoke(categoryIndex);
    public void RaiseItemSelected(int categoryIndex, int itemIndex) => OnItemSelected?.Invoke(categoryIndex, itemIndex);
    public void RaiseItemConsumed(int categoryIndex) => OnItemConsumed?.Invoke(categoryIndex);
    public void RaiseInputStateChanged(InputState newState) => OnInputStateChanged?.Invoke(newState);
    public void RaiseAttachmentModeChanged(int attachmentIndex) => OnAttachmentModeChanged?.Invoke(attachmentIndex);
    public void RaiseAttachmentModeExit() => OnAttachmentModeExit?.Invoke();
} 