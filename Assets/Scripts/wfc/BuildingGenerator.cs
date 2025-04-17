using UnityEngine;
using WFCBuildingGenerator;
using System.Collections.Generic;

public class QuickStartBuildingGenerator : MonoBehaviour
{
    [Header("Building Settings")]
    public Vector3Int buildingSize = new Vector3Int(8, 6, 8);
    public int seed = 0;
    public bool useRandomSeed = true;
    
    public bool buildGrid = false;
    public float buildingSpacing = 40f;
    public int gridSize = 3;
    
    // Reference to the procedural building generator
    private ProceduralBuildingGenerator buildingGenerator;
    
    // Track generated buildings
    private List<Transform> buildings = new List<Transform>();
    
    
    // Configure the building generator with optimal settings
    private void ConfigureBuildingGenerator()
    {
        buildingGenerator =  gameObject.GetComponent<ProceduralBuildingGenerator>();
        
        buildingGenerator.buildingDimensions = buildingSize;
        buildingGenerator.seed = useRandomSeed ? Random.Range(0, 10000) : seed;
        
        // Set material colors for better aerial view
        buildingGenerator.buildingBaseColor = new Color(0.8f, 0.8f, 0.8f);
        buildingGenerator.roofColor = new Color(0.3f, 0.3f, 0.3f);
        buildingGenerator.windowColor = new Color(0.1f, 0.3f, 0.6f, 0.8f);
        
        // Set window probability
        buildingGenerator.windowProbability = 0.4f;
        
        // Optimize detail level for aerial view
        buildingGenerator.detailProbability = 0.1f;
        
        // Set building height range
        buildingGenerator.minFloors = 3;
        buildingGenerator.maxFloors = 8;
    }
    
    // Generate a single building
    public void GenerateSingleBuilding()
    {
        // Configure building generator
        ConfigureBuildingGenerator();
        
        ClearBuildings();
        
        // Set a new random seed if needed
        if (useRandomSeed)
            buildingGenerator.seed = Random.Range(0, 10000);
            
        // Generate the building
        buildingGenerator.GenerateBuilding();
        
        // Add to the list
        buildings.Add(buildingGenerator.transform.GetChild(buildingGenerator.transform.childCount - 1));
    }
    
    // Generate a grid of buildings
    public void GenerateBuildingGrid()
    {
        // Configure building generator
        ConfigureBuildingGenerator();
        
        ClearBuildings();
        
        // Calculate grid offsets
        float halfGrid = (gridSize - 1) * buildingSpacing * 0.5f;
        
        // Generate buildings in a grid pattern
        for (int x = 0; x < gridSize; x++)
        {
            for (int z = 0; z < gridSize; z++)
            {
                // Calculate position offset
                Vector3 offset = new Vector3(
                    x * buildingSpacing - halfGrid,
                    0,
                    z * buildingSpacing - halfGrid
                );
                
                // Generate building with a new random seed
                buildingGenerator.seed = Random.Range(0, 10000);
                
                // Randomize building size slightly
                buildingGenerator.buildingDimensions = new Vector3Int(
                    buildingSize.x + Random.Range(-2, 3),
                    buildingSize.y + Random.Range(-1, 2),
                    buildingSize.z + Random.Range(-2, 3)
                );
                
                // Ensure minimum size
                buildingGenerator.buildingDimensions = new Vector3Int(
                    Mathf.Max(3, buildingGenerator.buildingDimensions.x),
                    Mathf.Max(2, buildingGenerator.buildingDimensions.y),
                    Mathf.Max(3, buildingGenerator.buildingDimensions.z)
                );
                
                // Generate the building
                buildingGenerator.GenerateBuilding();
                
                // Get the latest building and move it to the grid position
                Transform building = buildingGenerator.transform.GetChild(buildingGenerator.transform.childCount - 1);
                building.position = offset;
                
                // Add to the list
                buildings.Add(building);
            }
        }
        
    }
    
    // Clear all generated buildings
    public void ClearBuildings()
    {
        foreach (Transform building in buildings)
        {
            if (building != null)
                DestroyImmediate(building.gameObject);
        }
        
        buildings.Clear();
    }
}

#if UNITY_EDITOR
// Custom inspector
[UnityEditor.CustomEditor(typeof(QuickStartBuildingGenerator))]
public class QuickStartBuildingGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        QuickStartBuildingGenerator quickStart = (QuickStartBuildingGenerator)target;
        
        GUILayout.Space(10);
        GUILayout.Label("Building Generation", UnityEditor.EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Single Building"))
        {
            quickStart.GenerateSingleBuilding();
        }
        
        if (GUILayout.Button("Generate Building Grid"))
        {
            quickStart.GenerateBuildingGrid();
        }
        
        if (GUILayout.Button("Clear All Buildings"))
        {
            quickStart.ClearBuildings();
        }
        
        // Extra help text
        GUILayout.Space(10);
        GUILayout.Label("Instructions:", UnityEditor.EditorStyles.boldLabel);
        UnityEditor.EditorGUILayout.HelpBox(
            "1. Add this script to an empty GameObject\n" +
            "2. Click 'Generate Single Building' or 'Generate Building Grid'\n" +
            "3. Adjust building dimensions and seed for different results", 
            UnityEditor.MessageType.Info);
    }
}
#endif