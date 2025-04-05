# Building System Documentation

## Overview

The Building System provides a flexible way to manage and place different building types in your game. It integrates with the Radial Menu Framework to allow players to select and place buildings through an intuitive UI.

## Setup

### 1. Building System Configuration

1. The `BuildSystem` component should be attached to your player character.
2. In the inspector, you can define building types in the "Building Types" section:
   - Each entry consists of three key properties:
     - `buildingType` (string identifier): A unique name for this building type
     - `prefab` (GameObject): The building model to be instantiated
     - `placementDistance` (float): How far from the player this building will be placed when constructing
   - You can add as many building types as needed using the UI.
   - Legacy building types (Radio, FOB, AmmoCrate) are still supported for backward compatibility.

### 2. Radial Menu Setup

1. Create a main radial menu prefab with the `RMF_RadialMenu` component.
2. Assign this menu to the `BuildSystem`'s `buildMenu` field.
3. Create radial menu elements for each building type using the `RMF_RadialMenuElement` component.
4. Set the element type to "Building" and select the appropriate building type from the dropdown.

### 3. Sub-menu Organization

For player roles with different building permissions:

1. Create separate radial menu prefabs for each role (Squad Lead, Fire Team Lead, Normal Player).
2. For each role's menu, include only the building types that role can access.
3. Organize buildings using folders and sub-menus:
   - Create a radial menu element of type "Folder" to open a sub-menu.
   - Create a separate radial menu prefab for the sub-menu.
   - Assign the sub-menu to the folder element's `targetSubmenu` field.
   - Include a "Back" element in each sub-menu to return to the parent menu.

## Project Structure

For optimal organization, we recommend the following structure:

```
Assets/
  ├── Scripts/
  │     ├── Building/
  │     │     ├── BuildSystem.cs
  │     │     └── Editor/
  │     │           └── BuildSystemEditor.cs
  │     └── Player/
  │           └── PlayerStateManager.cs
  ├── Prefabs/
  │     ├── Buildings/
  │     │     ├── Radio.prefab
  │     │     ├── FOB.prefab
  │     │     └── AmmoCrate.prefab
  │     └── UI/
  │           ├── RadialMenus/
  │           │     ├── SquadLeadMenu.prefab
  │           │     ├── FireTeamLeadMenu.prefab
  │           │     └── SoldierMenu.prefab
  │           └── SubMenus/
  │                 ├── DefensiveStructures.prefab
  │                 └── SupportStructures.prefab
```

## Role-Based Menu Management

To manage different menus for different player roles:

1. Create a script to determine the player's role (e.g., `PlayerRoleManager`).
2. Maintain a dictionary of radial menu prefabs for each role.
3. When entering build mode, instantiate the appropriate menu based on the player's role.
4. You can also dynamically modify the available building options based on other factors like resources or game progress.

## Example Code

```csharp
// Example for role-based menu management
public class BuildMenuManager : MonoBehaviour
{
    [SerializeField] private GameObject squadLeadBuildMenu;
    [SerializeField] private GameObject fireTeamLeadBuildMenu;
    [SerializeField] private GameObject normalPlayerBuildMenu;
    
    [SerializeField] private BuildSystem buildSystem;
    [SerializeField] private PlayerRoleManager roleManager;
    
    private void Start()
    {
        // Set the appropriate build menu based on player role
        GameObject menuPrefab = null;
        
        switch (roleManager.GetPlayerRole())
        {
            case PlayerRole.SquadLead:
                menuPrefab = squadLeadBuildMenu;
                break;
            case PlayerRole.FireTeamLead:
                menuPrefab = fireTeamLeadBuildMenu;
                break;
            default:
                menuPrefab = normalPlayerBuildMenu;
                break;
        }
        
        if (menuPrefab != null)
        {
            // Instantiate menu and set it up
            GameObject menuInstance = Instantiate(menuPrefab, transform);
            buildSystem.buildMenu = menuInstance.GetComponent<RMF_RadialMenu>();
        }
    }
}
```

## Tips

1. **Parent-Child Structure**: Keep your radial menus and sub-menus as separate prefabs, not nested in the hierarchy.
2. **Menu Previews**: Use descriptive labels and icons for each building type to make selection intuitive.
3. **Custom Building Types**: You can easily add new building types in the editor without modifying code.
4. **Distance Settings**: Adjust placement distances for each building type to ensure comfortable placement.
5. **Testing**: Test the building placement in different environments to ensure proper ground detection. 