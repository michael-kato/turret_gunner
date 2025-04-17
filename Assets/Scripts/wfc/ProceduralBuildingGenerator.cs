using System.Collections.Generic;
using UnityEngine;
using WFCBuildingGenerator;

public class ProceduralBuildingGenerator : MonoBehaviour
{
    [Header("Generation Settings")]
    public Vector3Int buildingDimensions = new Vector3Int(5, 5, 5);
    public int seed = 0;
    public float unitSize = 2f; // Size of each building block unit in world units
    
    [Header("Building Structure")]
    [Range(0f, 1f)] public float windowProbability = 0.4f;
    [Range(0f, 1f)] public float detailProbability = 0.2f;
    public int minFloors = 3;
    public int maxFloors = 8;
    
    [Header("Procedural Materials")]
    public Color buildingBaseColor = new Color(0.8f, 0.8f, 0.8f);
    public Color roofColor = new Color(0.3f, 0.3f, 0.3f);
    public Color windowColor = new Color(0.1f, 0.3f, 0.6f);
    [Range(0f, 1f)] public float roughness = 0.7f;
    [Range(0f, 1f)] public float metallic = 0.1f;

    // Reference to WFC algorithm and building container
    private WaveFunctionCollapse wfc;
    private BuildingModuleManager moduleManager;
    private Transform buildingContainer;
    
    // Cached materials
    private Material buildingMaterial;
    private Material roofMaterial;
    private Material glassMaterial;
    
    /// <summary>
    /// Create a new building, duh!
    /// </summary>
    // Create a new building
    public void GenerateBuilding()
    {
        CleanupExistingBuilding();
        InitializeMaterials();
        
        // Create building container
        buildingContainer = new GameObject("Procedural_Building").transform;
        buildingContainer.SetParent(transform);
        
        // Randomize building dimensions if needed
        RandomizeBuildingDimensions();
        
        // Initialize WFC with procedural module system
        InitializeModuleSystem();
        
        // Run WFC algorithm
        wfc = new WaveFunctionCollapse(buildingDimensions, moduleManager, seed);
        bool success = wfc.RunAlgorithm();
        
        if (success)
        {
            GenerateMeshes();
        }
        else
        {
            Debug.LogError("Building generation failed. Try a different seed.");
        }
    }
    
    // Clean up any existing building
    private void CleanupExistingBuilding()
    {
        if (buildingContainer != null)
        {
            DestroyImmediate(buildingContainer.gameObject);
        }
    }
    
    // Create materials for the building
    private void InitializeMaterials()
    {
        // Building material (walls, etc.)
        buildingMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        buildingMaterial.color = buildingBaseColor;
        buildingMaterial.SetFloat("_Glossiness", 1f - roughness);
        buildingMaterial.SetFloat("_Metallic", metallic);
        
        // Roof material
        roofMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        roofMaterial.color = roofColor;
        roofMaterial.SetFloat("_Glossiness", 1f - roughness * 0.8f);
        roofMaterial.SetFloat("_Metallic", metallic * 1.2f);
        
        // Glass material for windows
        glassMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        glassMaterial.color = windowColor;
        glassMaterial.SetFloat("_Glossiness", 0.9f);
        glassMaterial.SetFloat("_Metallic", 0.8f);
    }
    
    // Randomize building dimensions for variety
    private void RandomizeBuildingDimensions()
    {
        if (seed != 0)
        {
            // Use the seed to initialize random generator
            Random.InitState(seed);
        }
        
        int floors = Random.Range(minFloors, maxFloors + 1);
        
        // Keep the x and z dimensions, just randomize height
        buildingDimensions = new Vector3Int(
            buildingDimensions.x,
            floors,
            buildingDimensions.z
        );
    }
    
    // Set up the module system
    private void InitializeModuleSystem()
    {
        moduleManager = new BuildingModuleManager();
        
        // Register module types for procedural generation
        RegisterModules();
        DefineCompatibilityRules();
    }
    
    // Register modules for procedural generation
    private void RegisterModules()
    {
        // We'll use module IDs:
        // 0: Empty (air)
        // 1: Wall
        // 2: Window
        // 3: Floor
        // 4: Roof
        // 5: Corner
        // 6: Detail (facade decoration)
        
        // Empty module (air)
        BuildingModule emptyModule = new BuildingModule("Empty", ModuleType.Special);
        for (int i = 0; i < 6; i++)
        {
            emptyModule.Connections[i] = ConnectionType.None;
        }
        moduleManager.AddModule(emptyModule, null, 0.0f); // Weight 0 as we don't want to randomly select this
        
        // Wall module
        BuildingModule wallModule = new BuildingModule("Wall", ModuleType.Wall);
        wallModule.Connections[0] = ConnectionType.Wall;  // X+
        wallModule.Connections[1] = ConnectionType.Wall;  // X-
        wallModule.Connections[2] = ConnectionType.Wall;  // Y+ (can connect to walls above)
        wallModule.Connections[3] = ConnectionType.Floor; // Y- (connects to floor below)
        wallModule.Connections[4] = ConnectionType.Wall;  // Z+
        wallModule.Connections[5] = ConnectionType.Wall;  // Z-
        moduleManager.AddModule(wallModule, null, 0.7f);
        
        // Window module
        BuildingModule windowModule = new BuildingModule("Window", ModuleType.Window);
        windowModule.Connections[0] = ConnectionType.Wall;   // X+
        windowModule.Connections[1] = ConnectionType.Wall;   // X-
        windowModule.Connections[2] = ConnectionType.Wall;   // Y+
        windowModule.Connections[3] = ConnectionType.Floor;  // Y-
        windowModule.Connections[4] = ConnectionType.Window; // Z+ (facade)
        windowModule.Connections[5] = ConnectionType.Wall;   // Z-
        moduleManager.AddModule(windowModule, null, windowProbability);
        
        // Create window variants for other sides of the building
        CreateWindowVariants();
        
        // Floor module
        BuildingModule floorModule = new BuildingModule("Floor", ModuleType.Floor);
        floorModule.Connections[0] = ConnectionType.Floor; // X+
        floorModule.Connections[1] = ConnectionType.Floor; // X-
        floorModule.Connections[2] = ConnectionType.Wall;  // Y+ (walls above)
        floorModule.Connections[3] = ConnectionType.Wall;  // Y- (walls below)
        floorModule.Connections[4] = ConnectionType.Floor; // Z+
        floorModule.Connections[5] = ConnectionType.Floor; // Z-
        moduleManager.AddModule(floorModule, null, 1.0f);
        
        // Roof module
        BuildingModule roofModule = new BuildingModule("Roof", ModuleType.Roof);
        roofModule.Connections[0] = ConnectionType.Roof;  // X+
        roofModule.Connections[1] = ConnectionType.Roof;  // X-
        roofModule.Connections[2] = ConnectionType.None;  // Y+ (nothing above roof)
        roofModule.Connections[3] = ConnectionType.Wall;  // Y- (walls below)
        roofModule.Connections[4] = ConnectionType.Roof;  // Z+
        roofModule.Connections[5] = ConnectionType.Roof;  // Z-
        moduleManager.AddModule(roofModule, null, 1.0f);
        
        // Corner module
        BuildingModule cornerModule = new BuildingModule("Corner", ModuleType.Corner);
        cornerModule.Connections[0] = ConnectionType.Wall;  // X+
        cornerModule.Connections[1] = ConnectionType.None;  // X- (outside)
        cornerModule.Connections[2] = ConnectionType.Wall;  // Y+
        cornerModule.Connections[3] = ConnectionType.Floor; // Y-
        cornerModule.Connections[4] = ConnectionType.None;  // Z+ (outside)
        cornerModule.Connections[5] = ConnectionType.Wall;  // Z-
        moduleManager.AddModule(cornerModule, null, 0.3f);
        
        // Create corner variants for other corners
        CreateCornerVariants();
        
        // Detail module (decorative elements)
        BuildingModule detailModule = new BuildingModule("Detail", ModuleType.Decoration);
        detailModule.Connections[0] = ConnectionType.Wall;  // X+
        detailModule.Connections[1] = ConnectionType.Wall;  // X-
        detailModule.Connections[2] = ConnectionType.Wall;  // Y+
        detailModule.Connections[3] = ConnectionType.Floor; // Y-
        detailModule.Connections[4] = ConnectionType.Wall;  // Z+
        detailModule.Connections[5] = ConnectionType.Wall;  // Z-
        moduleManager.AddModule(detailModule, null, detailProbability);
    }
    
    // Create window variants for all sides of the building
    private void CreateWindowVariants()
    {
        // Window on X- side
        BuildingModule windowXNeg = new BuildingModule("Window_X-", ModuleType.Window);
        windowXNeg.Connections[0] = ConnectionType.Wall;   // X+
        windowXNeg.Connections[1] = ConnectionType.Window; // X- (facade)
        windowXNeg.Connections[2] = ConnectionType.Wall;   // Y+
        windowXNeg.Connections[3] = ConnectionType.Floor;  // Y-
        windowXNeg.Connections[4] = ConnectionType.Wall;   // Z+
        windowXNeg.Connections[5] = ConnectionType.Wall;   // Z-
        moduleManager.AddModule(windowXNeg, null, windowProbability);
        
        // Window on Z- side
        BuildingModule windowZNeg = new BuildingModule("Window_Z-", ModuleType.Window);
        windowZNeg.Connections[0] = ConnectionType.Wall;   // X+
        windowZNeg.Connections[1] = ConnectionType.Wall;   // X-
        windowZNeg.Connections[2] = ConnectionType.Wall;   // Y+
        windowZNeg.Connections[3] = ConnectionType.Floor;  // Y-
        windowZNeg.Connections[4] = ConnectionType.Wall;   // Z+
        windowZNeg.Connections[5] = ConnectionType.Window; // Z- (facade)
        moduleManager.AddModule(windowZNeg, null, windowProbability);
        
        // Window on X+ side
        BuildingModule windowXPos = new BuildingModule("Window_X+", ModuleType.Window);
        windowXPos.Connections[0] = ConnectionType.Window; // X+ (facade)
        windowXPos.Connections[1] = ConnectionType.Wall;   // X-
        windowXPos.Connections[2] = ConnectionType.Wall;   // Y+
        windowXPos.Connections[3] = ConnectionType.Floor;  // Y-
        windowXPos.Connections[4] = ConnectionType.Wall;   // Z+
        windowXPos.Connections[5] = ConnectionType.Wall;   // Z-
        moduleManager.AddModule(windowXPos, null, windowProbability);
    }
    
    // Create corner variants for all corners of the building
    private void CreateCornerVariants()
    {
        // Corner: X+, Z+
        BuildingModule cornerXPosZPos = new BuildingModule("Corner_X+Z+", ModuleType.Corner);
        cornerXPosZPos.Connections[0] = ConnectionType.None;  // X+ (outside)
        cornerXPosZPos.Connections[1] = ConnectionType.Wall;  // X-
        cornerXPosZPos.Connections[2] = ConnectionType.Wall;  // Y+
        cornerXPosZPos.Connections[3] = ConnectionType.Floor; // Y-
        cornerXPosZPos.Connections[4] = ConnectionType.None;  // Z+ (outside)
        cornerXPosZPos.Connections[5] = ConnectionType.Wall;  // Z-
        moduleManager.AddModule(cornerXPosZPos, null, 0.3f);
        
        // Corner: X-, Z+
        BuildingModule cornerXNegZPos = new BuildingModule("Corner_X-Z+", ModuleType.Corner);
        cornerXNegZPos.Connections[0] = ConnectionType.Wall;  // X+
        cornerXNegZPos.Connections[1] = ConnectionType.None;  // X- (outside)
        cornerXNegZPos.Connections[2] = ConnectionType.Wall;  // Y+
        cornerXNegZPos.Connections[3] = ConnectionType.Floor; // Y-
        cornerXNegZPos.Connections[4] = ConnectionType.None;  // Z+ (outside)
        cornerXNegZPos.Connections[5] = ConnectionType.Wall;  // Z-
        moduleManager.AddModule(cornerXNegZPos, null, 0.3f);
        
        // Corner: X-, Z-
        BuildingModule cornerXNegZNeg = new BuildingModule("Corner_X-Z-", ModuleType.Corner);
        cornerXNegZNeg.Connections[0] = ConnectionType.Wall;  // X+
        cornerXNegZNeg.Connections[1] = ConnectionType.None;  // X- (outside)
        cornerXNegZNeg.Connections[2] = ConnectionType.Wall;  // Y+
        cornerXNegZNeg.Connections[3] = ConnectionType.Floor; // Y-
        cornerXNegZNeg.Connections[4] = ConnectionType.Wall;  // Z+
        cornerXNegZNeg.Connections[5] = ConnectionType.None;  // Z- (outside)
        moduleManager.AddModule(cornerXNegZNeg, null, 0.3f);
    }
    
    // Define compatibility rules between modules
    private void DefineCompatibilityRules()
    {
        List<int> allModuleIds = moduleManager.GetAllModuleIds();
        
        // For each module pair, define compatibility in each direction
        foreach (int moduleIdA in allModuleIds)
        {
            BuildingModule moduleA = moduleManager.GetModule(moduleIdA);
            
            foreach (int moduleIdB in allModuleIds)
            {
                BuildingModule moduleB = moduleManager.GetModule(moduleIdB);
                
                // For each of the 6 directions (X+, X-, Y+, Y-, Z+, Z-)
                for (int dir = 0; dir < 6; dir++)
                {
                    int oppositeDir = Direction.GetOpposite(dir);
                    ConnectionType connectionA = moduleA.Connections[dir];
                    ConnectionType connectionB = moduleB.Connections[oppositeDir];
                    
                    // Modules are compatible if they have matching connection types
                    bool compatible = (connectionA != ConnectionType.None && 
                                      connectionB != ConnectionType.None && 
                                      connectionA == connectionB);
                    
                    moduleManager.SetCompatibilityRule(moduleIdA, moduleIdB, dir, compatible);
                }
            }
        }
        
        // Add special rules for roof modules (only at the top)
        foreach (int moduleId in allModuleIds)
        {
            BuildingModule module = moduleManager.GetModule(moduleId);
            
            // Ensure roof modules can only appear at the top level
            if (module.Type == ModuleType.Roof)
            {
                // Roof modules can't have anything above them
                for (int otherModuleId = 0; otherModuleId < allModuleIds.Count; otherModuleId++)
                {
                    // Direction 2 is Y+ (above)
                    moduleManager.SetCompatibilityRule(moduleId, otherModuleId, 2, false);
                }
            }
        }
        
        // Add any additional special rules here
    }
    
    // Generate the building meshes based on the WFC result
    private void GenerateMeshes()
    {
        Dictionary<Vector3Int, int> collapsedGrid = wfc.GetCollapsedGrid();
        
        // Create GameObject hierarchy for the building
        Transform exteriorParent = new GameObject("Exterior").transform;
        exteriorParent.SetParent(buildingContainer);
        
        // Generate the actual geometry
        foreach (var entry in collapsedGrid)
        {
            Vector3Int pos = entry.Key;
            int moduleId = entry.Value;
            
            // Skip empty modules
            if (moduleId == 0) continue;
            
            // Get module type
            BuildingModule module = moduleManager.GetModule(moduleId);
            if (module == null) continue;
            
            // Generate geometry based on module type
            switch (module.Type)
            {
                case ModuleType.Wall:
                    GenerateWallGeometry(pos, module, exteriorParent);
                    break;
                    
                case ModuleType.Window:
                    GenerateWindowGeometry(pos, module, exteriorParent);
                    break;
                    
                case ModuleType.Floor:
                    GenerateFloorGeometry(pos, module, exteriorParent);
                    break;
                    
                case ModuleType.Roof:
                    GenerateRoofGeometry(pos, module, exteriorParent);
                    break;
                    
                case ModuleType.Corner:
                    GenerateCornerGeometry(pos, module, exteriorParent);
                    break;
                    
                case ModuleType.Decoration:
                    GenerateDetailGeometry(pos, module, exteriorParent);
                    break;
            }
        }
        
        // Combine meshes for performance
        CombineMeshes(exteriorParent);
    }
    
    // Generate wall geometry
    private void GenerateWallGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // Create wall object
        GameObject wallObj = new GameObject($"Wall_{pos.x}_{pos.y}_{pos.z}");
        wallObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        wallObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = wallObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = wallObj.AddComponent<MeshRenderer>();
        
        // Determine which sides of the wall to create
        // For exterior-only buildings, we only create outward-facing walls
        bool createXPos = IsExteriorFacing(pos + new Vector3Int(1, 0, 0));
        bool createXNeg = IsExteriorFacing(pos + new Vector3Int(-1, 0, 0));
        bool createZPos = IsExteriorFacing(pos + new Vector3Int(0, 0, 1));
        bool createZNeg = IsExteriorFacing(pos + new Vector3Int(0, 0, -1));
        
        // Create mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size for vertex positions
        float hs = unitSize * 0.5f;
        
        // Create wall sides as needed
        if (createXPos)
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(hs, -hs, hs),
                    Vector3.right, true);
        
        if (createXNeg)
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(-hs, -hs, -hs),
                    Vector3.left, true);
        
        if (createZPos)
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(hs, -hs, hs),
                    Vector3.forward, true);
        
        if (createZNeg)
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(-hs, -hs, -hs),
                    Vector3.back, true);
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = buildingMaterial;
        
        // Add collider
        wallObj.AddComponent<MeshCollider>();
    }
    
    // Generate window geometry
    private void GenerateWindowGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // Create window object
        GameObject windowObj = new GameObject($"Window_{pos.x}_{pos.y}_{pos.z}");
        windowObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        windowObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = windowObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = windowObj.AddComponent<MeshRenderer>();
        
        // Determine which sides have windows
        bool windowXPos = module.Connections[0] == ConnectionType.Window;
        bool windowXNeg = module.Connections[1] == ConnectionType.Window;
        bool windowZPos = module.Connections[4] == ConnectionType.Window;
        bool windowZNeg = module.Connections[5] == ConnectionType.Window;
        
        // Create mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size for vertex positions
        float hs = unitSize * 0.5f;
        // Window size (slightly smaller than the unit)
        float ws = hs * 0.7f;
        // Border size
        float bs = hs - ws;
        
        // Create window sides with frames
        if (windowXPos)
        {
            // Outer wall frame
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(hs, -hs, hs),
                    Vector3.right, true);
                    
            // Window inset (slightly recessed)
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs - 0.05f, ws, ws), new Vector3(hs - 0.05f, ws, -ws), 
                    new Vector3(hs - 0.05f, -ws, -ws), new Vector3(hs - 0.05f, -ws, ws),
                    Vector3.right, false);
        }
        
        if (windowXNeg)
        {
            // Outer wall frame
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(-hs, -hs, -hs),
                    Vector3.left, true);
                    
            // Window inset
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs + 0.05f, ws, -ws), new Vector3(-hs + 0.05f, ws, ws), 
                    new Vector3(-hs + 0.05f, -ws, ws), new Vector3(-hs + 0.05f, -ws, -ws),
                    Vector3.left, false);
        }
        
        if (windowZPos)
        {
            // Outer wall frame
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(hs, -hs, hs),
                    Vector3.forward, true);
                    
            // Window inset
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(ws, ws, hs - 0.05f), new Vector3(-ws, ws, hs - 0.05f), 
                    new Vector3(-ws, -ws, hs - 0.05f), new Vector3(ws, -ws, hs - 0.05f),
                    Vector3.forward, false);
        }
        
        if (windowZNeg)
        {
            // Outer wall frame
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(-hs, -hs, -hs),
                    Vector3.back, true);
                    
            // Window inset
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-ws, ws, -hs + 0.05f), new Vector3(ws, ws, -hs + 0.05f), 
                    new Vector3(ws, -ws, -hs + 0.05f), new Vector3(-ws, -ws, -hs + 0.05f),
                    Vector3.back, false);
        }
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        
        // Create material array for multi-material renderer
        Material[] materials = new Material[] { buildingMaterial, glassMaterial };
        meshRenderer.materials = materials;
        
        // Add collider
        windowObj.AddComponent<MeshCollider>();
    }
    
    // Generate floor geometry (simple flat surface)
    private void GenerateFloorGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // We'll only generate the top face of the floor since the building is viewed from outside
        // and the bottom face would not be visible
        
        // Create floor object
        GameObject floorObj = new GameObject($"Floor_{pos.x}_{pos.y}_{pos.z}");
        floorObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        floorObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = floorObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = floorObj.AddComponent<MeshRenderer>();
        
        // Only create top face
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size
        float hs = unitSize * 0.5f;
        
        // Top face
        AddQuad(vertices, triangles, normals, uvs, 
                new Vector3(-hs, hs, hs), new Vector3(hs, hs, hs), 
                new Vector3(hs, hs, -hs), new Vector3(-hs, hs, -hs),
                Vector3.up, true);
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = buildingMaterial;
    }
    
    // Generate roof geometry
    private void GenerateRoofGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // Create roof object
        GameObject roofObj = new GameObject($"Roof_{pos.x}_{pos.y}_{pos.z}");
        roofObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        roofObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = roofObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = roofObj.AddComponent<MeshRenderer>();
        
        // For a simple flat roof
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size
        float hs = unitSize * 0.5f;
        
        // Top face (slightly raised in the center for drainage)
        Vector3 center = new Vector3(0, hs + 0.2f, 0);
        
        // Create roof with slight slope
        vertices.Add(new Vector3(-hs, hs, hs)); // 0
        vertices.Add(new Vector3(hs, hs, hs));  // 1
        vertices.Add(new Vector3(hs, hs, -hs)); // 2
        vertices.Add(new Vector3(-hs, hs, -hs)); // 3
        vertices.Add(center); // 4 - center point
        
        // Triangles (center to edges)
        triangles.Add(0); triangles.Add(1); triangles.Add(4);
        triangles.Add(1); triangles.Add(2); triangles.Add(4);
        triangles.Add(2); triangles.Add(3); triangles.Add(4);
        triangles.Add(3); triangles.Add(0); triangles.Add(4);
        
        // Normals (approximate)
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        normals.Add(Vector3.up);
        
        // UVs
        uvs.Add(new Vector2(0, 0));
        uvs.Add(new Vector2(1, 0));
        uvs.Add(new Vector2(1, 1));
        uvs.Add(new Vector2(0, 1));
        uvs.Add(new Vector2(0.5f, 0.5f));
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = roofMaterial;
    }
    
    // Generate corner geometry
    private void GenerateCornerGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // Create corner object
        GameObject cornerObj = new GameObject($"Corner_{pos.x}_{pos.y}_{pos.z}");
        cornerObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        cornerObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = cornerObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = cornerObj.AddComponent<MeshRenderer>();
        
        // Determine corner type based on connections
        bool exteriorXPos = module.Connections[0] == ConnectionType.None;
        bool exteriorXNeg = module.Connections[1] == ConnectionType.None;
        bool exteriorZPos = module.Connections[4] == ConnectionType.None;
        bool exteriorZNeg = module.Connections[5] == ConnectionType.None;
        
        // Create mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size
        float hs = unitSize * 0.5f;
        
        // Create corner walls based on configuration
        if (exteriorXPos && exteriorZPos)
        {
            // Corner at X+, Z+
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(hs, -hs, hs),
                    Vector3.right, true);
                    
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(hs, -hs, hs),
                    Vector3.forward, true);
        }
        else if (exteriorXNeg && exteriorZPos)
        {
            // Corner at X-, Z+
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(-hs, -hs, -hs),
                    Vector3.left, true);
                    
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(hs, -hs, hs),
                    Vector3.forward, true);
        }
        else if (exteriorXNeg && exteriorZNeg)
        {
            // Corner at X-, Z-
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(-hs, -hs, -hs),
                    Vector3.left, true);
                    
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(-hs, -hs, -hs),
                    Vector3.back, true);
        }
        else if (exteriorXPos && exteriorZNeg)
        {
            // Corner at X+, Z-
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(hs, -hs, hs),
                    Vector3.right, true);
                    
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(-hs, -hs, -hs),
                    Vector3.back, true);
        }
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = buildingMaterial;
        
        // Add collider
        cornerObj.AddComponent<MeshCollider>();
    }
    
    // Generate detail geometry
    private void GenerateDetailGeometry(Vector3Int pos, BuildingModule module, Transform parent)
    {
        // Create detail object
        GameObject detailObj = new GameObject($"Detail_{pos.x}_{pos.y}_{pos.z}");
        detailObj.transform.SetParent(parent);
        
        // Position in world space
        Vector3 worldPos = new Vector3(pos.x * unitSize, pos.y * unitSize, pos.z * unitSize);
        detailObj.transform.position = worldPos;
        
        // Create mesh
        MeshFilter meshFilter = detailObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = detailObj.AddComponent<MeshRenderer>();
        
        // Determine which sides to put details on (exterior facing)
        bool createXPos = IsExteriorFacing(pos + new Vector3Int(1, 0, 0));
        bool createXNeg = IsExteriorFacing(pos + new Vector3Int(-1, 0, 0));
        bool createZPos = IsExteriorFacing(pos + new Vector3Int(0, 0, 1));
        bool createZNeg = IsExteriorFacing(pos + new Vector3Int(0, 0, -1));
        
        // Create mesh
        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Half size
        float hs = unitSize * 0.5f;
        float detailSize = hs * 0.1f; // Small detail protrusions
        
        // Create simple details on walls
        if (createXPos)
        {
            // Detail on X+ wall
            AddDetailQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs + detailSize, hs * 0.8f, hs * 0.5f), 
                    new Vector3(hs + detailSize, hs * 0.8f, -hs * 0.5f),
                    new Vector3(hs + detailSize, -hs * 0.8f, -hs * 0.5f), 
                    new Vector3(hs + detailSize, -hs * 0.8f, hs * 0.5f),
                    Vector3.right, vertices.Count);
                    
            // Base wall
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(hs, -hs, hs),
                    Vector3.right, true);
        }
        
        if (createXNeg)
        {
            // Detail on X- wall
            AddDetailQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs - detailSize, hs * 0.8f, -hs * 0.5f), 
                    new Vector3(-hs - detailSize, hs * 0.8f, hs * 0.5f),
                    new Vector3(-hs - detailSize, -hs * 0.8f, hs * 0.5f), 
                    new Vector3(-hs - detailSize, -hs * 0.8f, -hs * 0.5f),
                    Vector3.left, vertices.Count);
                    
            // Base wall
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(-hs, -hs, -hs),
                    Vector3.left, true);
        }
        
        if (createZPos)
        {
            // Detail on Z+ wall
            AddDetailQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs * 0.5f, hs * 0.8f, hs + detailSize), 
                    new Vector3(-hs * 0.5f, hs * 0.8f, hs + detailSize),
                    new Vector3(-hs * 0.5f, -hs * 0.8f, hs + detailSize), 
                    new Vector3(hs * 0.5f, -hs * 0.8f, hs + detailSize),
                    Vector3.forward, vertices.Count);
                    
            // Base wall
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(hs, hs, hs), new Vector3(-hs, hs, hs), 
                    new Vector3(-hs, -hs, hs), new Vector3(hs, -hs, hs),
                    Vector3.forward, true);
        }
        
        if (createZNeg)
        {
            // Detail on Z- wall
            AddDetailQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs * 0.5f, hs * 0.8f, -hs - detailSize), 
                    new Vector3(hs * 0.5f, hs * 0.8f, -hs - detailSize),
                    new Vector3(hs * 0.5f, -hs * 0.8f, -hs - detailSize), 
                    new Vector3(-hs * 0.5f, -hs * 0.8f, -hs - detailSize),
                    Vector3.back, vertices.Count);
                    
            // Base wall
            AddQuad(vertices, triangles, normals, uvs, 
                    new Vector3(-hs, hs, -hs), new Vector3(hs, hs, -hs), 
                    new Vector3(hs, -hs, -hs), new Vector3(-hs, -hs, -hs),
                    Vector3.back, true);
        }
        
        // Set mesh data
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = buildingMaterial;
        
        // Add collider
        detailObj.AddComponent<MeshCollider>();
    }
    
    // Check if a position is exterior (outside the building)
    private bool IsExteriorFacing(Vector3Int pos)
    {
        // If the position is outside the grid, it's exterior
        if (pos.x < 0 || pos.x >= buildingDimensions.x ||
            pos.y < 0 || pos.y >= buildingDimensions.y ||
            pos.z < 0 || pos.z >= buildingDimensions.z)
            return true;
            
        // Get the module at this position
        Dictionary<Vector3Int, int> grid = wfc.GetCollapsedGrid();
        if (grid.TryGetValue(pos, out int moduleId))
        {
            // Empty module (0) means exterior
            return moduleId == 0;
        }
        
        return true;
    }
    
    // Add a quad to mesh data
    private void AddQuad(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs,
                          Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft,
                          Vector3 normal, bool isBuildingMaterial)
    {
        int vertexIndex = vertices.Count;
        
        // Add vertices
        vertices.Add(topLeft);
        vertices.Add(topRight);
        vertices.Add(bottomRight);
        vertices.Add(bottomLeft);
        
        // Add triangles (two triangles for quad)
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 1);
        triangles.Add(vertexIndex + 2);
        
        triangles.Add(vertexIndex);
        triangles.Add(vertexIndex + 2);
        triangles.Add(vertexIndex + 3);
        
        // Add normals
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        
        // Add UVs
        if (isBuildingMaterial)
        {
            // Regular wall UVs
            uvs.Add(new Vector2(0, 1));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(0, 0));
        }
        else
        {
            // Window UVs
            uvs.Add(new Vector2(0.1f, 0.9f));
            uvs.Add(new Vector2(0.9f, 0.9f));
            uvs.Add(new Vector2(0.9f, 0.1f));
            uvs.Add(new Vector2(0.1f, 0.1f));
        }
    }
    
    // Add a detailed quad with additional geometry
    private void AddDetailQuad(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs,
                               Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft,
                               Vector3 normal, int startIndex)
    {
        // Add vertices
        vertices.Add(topLeft);
        vertices.Add(topRight);
        vertices.Add(bottomRight);
        vertices.Add(bottomLeft);
        
        // Add triangles
        triangles.Add(startIndex);
        triangles.Add(startIndex + 1);
        triangles.Add(startIndex + 2);
        
        triangles.Add(startIndex);
        triangles.Add(startIndex + 2);
        triangles.Add(startIndex + 3);
        
        // Add normals
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        normals.Add(normal);
        
        // Add UVs
        uvs.Add(new Vector2(0.05f, 0.95f));
        uvs.Add(new Vector2(0.95f, 0.95f));
        uvs.Add(new Vector2(0.95f, 0.05f));
        uvs.Add(new Vector2(0.05f, 0.05f));
    }
    
    // Combine meshes for better performance
    private void CombineMeshes(Transform parent)
    {
        // Find all mesh renderers in the parent
        MeshFilter[] meshFilters = parent.GetComponentsInChildren<MeshFilter>();
        
        // Group meshes by material for better batching
        Dictionary<Material, List<MeshFilter>> materialToMeshes = new Dictionary<Material, List<MeshFilter>>();
        
        foreach (MeshFilter filter in meshFilters)
        {
            MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null) continue;
            
            Material material = renderer.sharedMaterial;
            if (material == null) continue;
            
            if (!materialToMeshes.ContainsKey(material))
            {
                materialToMeshes[material] = new List<MeshFilter>();
            }
            
            materialToMeshes[material].Add(filter);
        }
        
        // Combine meshes for each material group
        foreach (var kvp in materialToMeshes)
        {
            Material material = kvp.Key;
            List<MeshFilter> filters = kvp.Value;
            
            if (filters.Count < 2) continue; // No need to combine single meshes
            
            // Create a combined mesh object
            GameObject combinedObj = new GameObject($"Combined_{material.name}");
            combinedObj.transform.SetParent(parent);
            combinedObj.transform.localPosition = Vector3.zero;
            
            MeshFilter combinedFilter = combinedObj.AddComponent<MeshFilter>();
            MeshRenderer combinedRenderer = combinedObj.AddComponent<MeshRenderer>();
            
            // Prepare the combine instances
            CombineInstance[] combine = new CombineInstance[filters.Count];
            
            for (int i = 0; i < filters.Count; i++)
            {
                combine[i].mesh = filters[i].sharedMesh;
                combine[i].transform = filters[i].transform.localToWorldMatrix;
                
                // Disable the original objects
                filters[i].gameObject.SetActive(false);
            }
            
            // Combine the meshes
            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine);
            combinedFilter.sharedMesh = combinedMesh;
            combinedRenderer.material = material;
            
            // Add a collider
            combinedObj.AddComponent<MeshCollider>();
        }
    }
}

#if UNITY_EDITOR
// Custom inspector
[UnityEditor.CustomEditor(typeof(ProceduralBuildingGenerator))]
public class ProceduralBuildingGeneratorEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ProceduralBuildingGenerator generator = (ProceduralBuildingGenerator)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Generate Building"))
        {
            generator.GenerateBuilding();
        }
        
        if (GUILayout.Button("Generate 5 Random Buildings"))
        {
            for (int i = 0; i < 5; i++)
            {
                generator.seed = UnityEngine.Random.Range(0, 10000);
                generator.GenerateBuilding();
            }
        }
    }
}
#endif