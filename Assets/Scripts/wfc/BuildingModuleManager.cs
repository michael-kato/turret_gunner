using UnityEngine;
using System.Collections.Generic;
using WFCBuildingGenerator;

// This class demonstrates how to create and configure building modules
public class BuildingModuleDefinitions : MonoBehaviour
{
    [Header("Module Prefabs")]
    public List<GameObject> floorPrefabs = new List<GameObject>();
    public List<GameObject> wallPrefabs = new List<GameObject>();
    public List<GameObject> windowPrefabs = new List<GameObject>();
    public List<GameObject> doorPrefabs = new List<GameObject>();
    public List<GameObject> roofPrefabs = new List<GameObject>();
    public List<GameObject> cornerPrefabs = new List<GameObject>();
    public List<GameObject> columnPrefabs = new List<GameObject>();
    public List<GameObject> stairPrefabs = new List<GameObject>();

    [Header("Module Weights")]
    [Range(0f, 1f)] public float standardWallWeight = 0.7f;
    [Range(0f, 1f)] public float windowWeight = 0.3f;
    [Range(0f, 1f)] public float doorWeight = 0.1f;
    [Range(0f, 1f)] public float cornerWeight = 0.2f;
    [Range(0f, 1f)] public float stairsWeight = 0.05f;

    // Create a fully configured module manager with all building components
    public BuildingModuleManager CreateModuleManager()
    {
        BuildingModuleManager manager = new BuildingModuleManager();

        // Register all modules
        RegisterFloorModules(manager);
        RegisterWallModules(manager);
        RegisterWindowModules(manager);
        RegisterDoorModules(manager);
        RegisterRoofModules(manager);
        RegisterCornerModules(manager);
        RegisterColumnModules(manager);
        RegisterStairModules(manager);

        // Set up compatibility rules
        DefineCompatibilityRules(manager);

        return manager;
    }

    // Register floor modules
    private void RegisterFloorModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < floorPrefabs.Count; i++)
        {
            BuildingModule floor = new BuildingModule($"Floor_{i}", ModuleType.Floor);
            
            // Floor connects to other floors on X and Z axes
            floor.Connections[0] = ConnectionType.Floor; // X+
            floor.Connections[1] = ConnectionType.Floor; // X-
            floor.Connections[2] = ConnectionType.Wall;  // Y+ (connects to walls above)
            floor.Connections[3] = ConnectionType.None;  // Y- (nothing below ground floor)
                                                         // or ConnectionType.Floor for upper floors
            floor.Connections[4] = ConnectionType.Floor; // Z+
            floor.Connections[5] = ConnectionType.Floor; // Z-
            
            manager.AddModule(floor, floorPrefabs[i], 1.0f);
        }

        // Add special ground floor
        if (floorPrefabs.Count > 0)
        {
            BuildingModule groundFloor = new BuildingModule("Ground_Floor", ModuleType.Floor);
            groundFloor.Connections[0] = ConnectionType.Floor; // X+
            groundFloor.Connections[1] = ConnectionType.Floor; // X-
            groundFloor.Connections[2] = ConnectionType.Wall;  // Y+ (connects to walls above)
            groundFloor.Connections[3] = ConnectionType.None;  // Y- (nothing below)
            groundFloor.Connections[4] = ConnectionType.Floor; // Z+
            groundFloor.Connections[5] = ConnectionType.Floor; // Z-
            
            manager.AddModule(groundFloor, floorPrefabs[0], 1.0f);
        }
    }

    // Register wall modules
    private void RegisterWallModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < wallPrefabs.Count; i++)
        {
            BuildingModule wall = new BuildingModule($"Wall_{i}", ModuleType.Wall);
            
            wall.Connections[0] = ConnectionType.Wall;  // X+
            wall.Connections[1] = ConnectionType.Wall;  // X-
            wall.Connections[2] = ConnectionType.Wall;  // Y+ (connects to walls above or roof)
            wall.Connections[3] = ConnectionType.Floor; // Y- (connects to floor below)
            wall.Connections[4] = ConnectionType.Wall;  // Z+
            wall.Connections[5] = ConnectionType.Wall;  // Z-
            
            manager.AddModule(wall, wallPrefabs[i], standardWallWeight);
        }
    }

    // Register window modules
    private void RegisterWindowModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < windowPrefabs.Count; i++)
        {
            BuildingModule window = new BuildingModule($"Window_{i}", ModuleType.Window);
            
            window.Connections[0] = ConnectionType.Wall;   // X+
            window.Connections[1] = ConnectionType.Wall;   // X-
            window.Connections[2] = ConnectionType.Wall;   // Y+ (wall above)
            window.Connections[3] = ConnectionType.Floor;  // Y- (floor below)
            window.Connections[4] = ConnectionType.Window; // Z+ (facade facing outward)
            window.Connections[5] = ConnectionType.Wall;   // Z- (internal wall)
            
            manager.AddModule(window, windowPrefabs[i], windowWeight);

            // Create rotated variants for all four sides of the building
            CreateRotatedVariants(manager, window, windowPrefabs[i], windowWeight);
        }
    }

    // Register door modules
    private void RegisterDoorModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < doorPrefabs.Count; i++)
        {
            BuildingModule door = new BuildingModule($"Door_{i}", ModuleType.Door);
            
            door.Connections[0] = ConnectionType.Wall;  // X+
            door.Connections[1] = ConnectionType.Wall;  // X-
            door.Connections[2] = ConnectionType.Wall;  // Y+ (wall above)
            door.Connections[3] = ConnectionType.Floor; // Y- (floor below)
            door.Connections[4] = ConnectionType.Door;  // Z+ (facade facing outward)
            door.Connections[5] = ConnectionType.Wall;  // Z- (internal wall)
            
            manager.AddModule(door, doorPrefabs[i], doorWeight);
            
            // Create rotated variants for all four sides of the building
            CreateRotatedVariants(manager, door, doorPrefabs[i], doorWeight);
        }
    }

    // Register roof modules
    private void RegisterRoofModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < roofPrefabs.Count; i++)
        {
            BuildingModule roof = new BuildingModule($"Roof_{i}", ModuleType.Roof);
            
            roof.Connections[0] = ConnectionType.Roof;  // X+
            roof.Connections[1] = ConnectionType.Roof;  // X-
            roof.Connections[2] = ConnectionType.None;  // Y+ (nothing above roof)
            roof.Connections[3] = ConnectionType.Wall;  // Y- (wall below)
            roof.Connections[4] = ConnectionType.Roof;  // Z+
            roof.Connections[5] = ConnectionType.Roof;  // Z-
            
            manager.AddModule(roof, roofPrefabs[i], 1.0f);
        }
    }

    // Register corner modules
    private void RegisterCornerModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < cornerPrefabs.Count; i++)
        {
            BuildingModule corner = new BuildingModule($"Corner_{i}", ModuleType.Corner);
            
            corner.Connections[0] = ConnectionType.Wall;  // X+
            corner.Connections[1] = ConnectionType.None;  // X- (outer corner)
            corner.Connections[2] = ConnectionType.Wall;  // Y+ (wall above)
            corner.Connections[3] = ConnectionType.Floor; // Y- (floor below)
            corner.Connections[4] = ConnectionType.None;  // Z+ (outer corner)
            corner.Connections[5] = ConnectionType.Wall;  // Z-
            
            manager.AddModule(corner, cornerPrefabs[i], cornerWeight);
            
            // Create rotated variants for all four corners
            BuildingModule cornerVariant1 = new BuildingModule($"Corner_{i}_Var1", ModuleType.Corner);
            cornerVariant1.Connections[0] = ConnectionType.None;  // X+ (outer corner)
            cornerVariant1.Connections[1] = ConnectionType.Wall;  // X-
            cornerVariant1.Connections[2] = ConnectionType.Wall;  // Y+
            cornerVariant1.Connections[3] = ConnectionType.Floor; // Y-
            cornerVariant1.Connections[4] = ConnectionType.None;  // Z+ (outer corner)
            cornerVariant1.Connections[5] = ConnectionType.Wall;  // Z-
            manager.AddModule(cornerVariant1, cornerPrefabs[i], cornerWeight);
            
            BuildingModule cornerVariant2 = new BuildingModule($"Corner_{i}_Var2", ModuleType.Corner);
            cornerVariant2.Connections[0] = ConnectionType.None;  // X+ (outer corner)
            cornerVariant2.Connections[1] = ConnectionType.Wall;  // X-
            cornerVariant2.Connections[2] = ConnectionType.Wall;  // Y+
            cornerVariant2.Connections[3] = ConnectionType.Floor; // Y-
            cornerVariant2.Connections[4] = ConnectionType.Wall;  // Z+
            cornerVariant2.Connections[5] = ConnectionType.None;  // Z- (outer corner)
            manager.AddModule(cornerVariant2, cornerPrefabs[i], cornerWeight);
            
            BuildingModule cornerVariant3 = new BuildingModule($"Corner_{i}_Var3", ModuleType.Corner);
            cornerVariant3.Connections[0] = ConnectionType.Wall;  // X+
            cornerVariant3.Connections[1] = ConnectionType.None;  // X- (outer corner)
            cornerVariant3.Connections[2] = ConnectionType.Wall;  // Y+
            cornerVariant3.Connections[3] = ConnectionType.Floor; // Y-
            cornerVariant3.Connections[4] = ConnectionType.Wall;  // Z+
            cornerVariant3.Connections[5] = ConnectionType.None;  // Z- (outer corner)
            manager.AddModule(cornerVariant3, cornerPrefabs[i], cornerWeight);
        }
    }

    // Register column modules
    private void RegisterColumnModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < columnPrefabs.Count; i++)
        {
            BuildingModule column = new BuildingModule($"Column_{i}", ModuleType.Column);
            
            column.Connections[0] = ConnectionType.Wall;  // X+
            column.Connections[1] = ConnectionType.Wall;  // X-
            column.Connections[2] = ConnectionType.Wall;  // Y+ (wall or column above)
            column.Connections[3] = ConnectionType.Floor; // Y- (floor or column below)
            column.Connections[4] = ConnectionType.Wall;  // Z+
            column.Connections[5] = ConnectionType.Wall;  // Z-
            
            manager.AddModule(column, columnPrefabs[i], 0.3f);
        }
    }

    // Register stair modules
    private void RegisterStairModules(BuildingModuleManager manager)
    {
        for (int i = 0; i < stairPrefabs.Count; i++)
        {
            BuildingModule stairs = new BuildingModule($"Stairs_{i}", ModuleType.Stairs);
            
            stairs.Connections[0] = ConnectionType.Wall;  // X+
            stairs.Connections[1] = ConnectionType.Wall;  // X-
            stairs.Connections[2] = ConnectionType.Floor; // Y+ (floor above at the top of the stairs)
            stairs.Connections[3] = ConnectionType.Floor; // Y- (floor below at the bottom of the stairs)
            stairs.Connections[4] = ConnectionType.Wall;  // Z+
            stairs.Connections[5] = ConnectionType.Wall;  // Z-
            
            manager.AddModule(stairs, stairPrefabs[i], stairsWeight);
            
            // Create rotated variants for different stair orientations
            CreateRotatedVariants(manager, stairs, stairPrefabs[i], stairsWeight);
        }
    }

    // Helper method to create rotated variants of a module
    private void CreateRotatedVariants(BuildingModuleManager manager, BuildingModule baseModule, GameObject prefab, float weight)
    {
        // Create a module rotated 90 degrees (around Y axis)
        BuildingModule rotated90 = new BuildingModule($"{baseModule.Name}_Rot90", baseModule.Type);
        rotated90.Connections[0] = baseModule.Connections[4]; // X+ <- Z+
        rotated90.Connections[1] = baseModule.Connections[5]; // X- <- Z-
        rotated90.Connections[2] = baseModule.Connections[2]; // Y+ stays the same
        rotated90.Connections[3] = baseModule.Connections[3]; // Y- stays the same
        rotated90.Connections[4] = baseModule.Connections[1]; // Z+ <- X-
        rotated90.Connections[5] = baseModule.Connections[0]; // Z- <- X+
        manager.AddModule(rotated90, prefab, weight);
        
        // Create a module rotated 180 degrees
        BuildingModule rotated180 = new BuildingModule($"{baseModule.Name}_Rot180", baseModule.Type);
        rotated180.Connections[0] = baseModule.Connections[1]; // X+ <- X-
        rotated180.Connections[1] = baseModule.Connections[0]; // X- <- X+
        rotated180.Connections[2] = baseModule.Connections[2]; // Y+ stays the same
        rotated180.Connections[3] = baseModule.Connections[3]; // Y- stays the same
        rotated180.Connections[4] = baseModule.Connections[5]; // Z+ <- Z-
        rotated180.Connections[5] = baseModule.Connections[4]; // Z- <- Z+
        manager.AddModule(rotated180, prefab, weight);
        
        // Create a module rotated 270 degrees
        BuildingModule rotated270 = new BuildingModule($"{baseModule.Name}_Rot270", baseModule.Type);
        rotated270.Connections[0] = baseModule.Connections[5]; // X+ <- Z-
        rotated270.Connections[1] = baseModule.Connections[4]; // X- <- Z+
        rotated270.Connections[2] = baseModule.Connections[2]; // Y+ stays the same
        rotated270.Connections[3] = baseModule.Connections[3]; // Y- stays the same
        rotated270.Connections[4] = baseModule.Connections[0]; // Z+ <- X+
        rotated270.Connections[5] = baseModule.Connections[1]; // Z- <- X-
        manager.AddModule(rotated270, prefab, weight);
    }

    // Define compatibility rules between all modules
    private void DefineCompatibilityRules(BuildingModuleManager manager)
    {
        List<int> allModuleIds = manager.GetAllModuleIds();
        
        foreach (int moduleIdA in allModuleIds)
        {
            BuildingModule moduleA = manager.GetModule(moduleIdA);
            
            foreach (int moduleIdB in allModuleIds)
            {
                BuildingModule moduleB = manager.GetModule(moduleIdB);
                
                // For each of the 6 directions
                for (int dir = 0; dir < 6; dir++)
                {
                    int oppositeDir = Direction.GetOpposite(dir);
                    ConnectionType connectionA = moduleA.Connections[dir];
                    ConnectionType connectionB = moduleB.Connections[oppositeDir];
                    
                    // Modules are compatible if they have matching connection types
                    bool compatible = (connectionA != ConnectionType.None && 
                                     connectionB != ConnectionType.None && 
                                     connectionA == connectionB);
                    
                    manager.SetCompatibilityRule(moduleIdA, moduleIdB, dir, compatible);
                }
            }
        }
        
        // Add special rules here, such as:
        // - Ensure doors only appear on the ground floor
        // - Certain modules can only appear at specific positions
        // - Frequency of windows on each floor
    }

    // Method to add additional constraints for specific building types
    public void AddArchitecturalStyle(BuildingModuleManager manager, ArchitecturalStyle style)
    {
        switch (style)
        {
            case ArchitecturalStyle.Modern:
                // Modern buildings have more windows, flat roofs
                AdjustModuleWeights(manager, ModuleType.Window, 0.6f);
                AdjustModuleWeights(manager, ModuleType.Door, 0.1f);
                // Add more specific rules for modern style
                break;
                
            case ArchitecturalStyle.Classical:
                // Classical buildings have more columns, specific roof types
                AdjustModuleWeights(manager, ModuleType.Column, 0.5f);
                AdjustModuleWeights(manager, ModuleType.Window, 0.3f);
                // Add more specific rules for classical style
                break;
                
            case ArchitecturalStyle.Industrial:
                // Industrial buildings have specific wall types, fewer windows
                AdjustModuleWeights(manager, ModuleType.Window, 0.2f);
                AdjustModuleWeights(manager, ModuleType.Door, 0.15f);
                // Add more specific rules for industrial style
                break;
                
            case ArchitecturalStyle.Medieval:
                // Medieval buildings have specific wall/roof combinations
                AdjustModuleWeights(manager, ModuleType.Window, 0.15f);
                AdjustModuleWeights(manager, ModuleType.Door, 0.1f);
                // Add more specific rules for medieval style
                break;
        }
    }
    
    // Helper method to adjust weights for a specific module type
    private void AdjustModuleWeights(BuildingModuleManager manager, ModuleType type, float newWeight)
    {
        List<int> allModuleIds = manager.GetAllModuleIds();
        
        foreach (int moduleId in allModuleIds)
        {
            BuildingModule module = manager.GetModule(moduleId);
            if (module.Type == type)
            {
                // Adjust the weight using reflection or a method in the module manager
                // This is a simplified example - you would need to add a method to BuildingModuleManager
                // manager.SetModuleWeight(moduleId, newWeight);
            }
        }
    }
}

// Enum for different architectural styles
public enum ArchitecturalStyle
{
    Modern,
    Classical,
    Industrial,
    Medieval
}
