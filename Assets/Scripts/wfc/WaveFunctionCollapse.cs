using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WFCBuildingGenerator
{
    // Core Wave Function Collapse implementation
    public class WaveFunctionCollapse
    {
        // The grid of cells for our building
        private Cell[,,] grid;
        private Vector3Int dimensions;
        private System.Random random;
        private List<int> cellsToCollapse = new List<int>();
        private BuildingModuleManager moduleManager;

        public WaveFunctionCollapse(Vector3Int dimensions, BuildingModuleManager moduleManager, int seed = 0)
        {
            this.dimensions = dimensions;
            this.moduleManager = moduleManager;
            random = seed == 0 ? new System.Random() : new System.Random(seed);
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            grid = new Cell[dimensions.x, dimensions.y, dimensions.z];
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int z = 0; z < dimensions.z; z++)
                    {
                        // Initialize each cell with all possible modules
                        grid[x, y, z] = new Cell(moduleManager.GetAllModuleIds());
                    }
                }
            }
        }

        // Find the cell with lowest entropy (fewest possible states)
        private Vector3Int? GetMinEntropyCell()
        {
            int minEntropy = int.MaxValue;
            List<Vector3Int> minEntropyCells = new List<Vector3Int>();

            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int z = 0; z < dimensions.z; z++)
                    {
                        Cell cell = grid[x, y, z];
                        if (!cell.IsCollapsed && cell.PossibleModules.Count < minEntropy && cell.PossibleModules.Count > 0)
                        {
                            minEntropy = cell.PossibleModules.Count;
                            minEntropyCells.Clear();
                            minEntropyCells.Add(new Vector3Int(x, y, z));
                        }
                        else if (!cell.IsCollapsed && cell.PossibleModules.Count == minEntropy && cell.PossibleModules.Count > 0)
                        {
                            minEntropyCells.Add(new Vector3Int(x, y, z));
                        }
                    }
                }
            }

            if (minEntropyCells.Count == 0)
                return null;

            // Choose a random cell among those with minimum entropy
            return minEntropyCells[random.Next(minEntropyCells.Count)];
        }

        // Collapse a cell to a single state
        private void CollapseCell(Vector3Int position)
        {
            Cell cell = grid[position.x, position.y, position.z];
            
            // Weight the modules by their frequency in the module set
            List<float> weights = new List<float>();
            foreach (int moduleId in cell.PossibleModules)
            {
                weights.Add(moduleManager.GetModuleWeight(moduleId));
            }

            // Choose a random module based on weights
            float totalWeight = weights.Sum();
            float randomValue = (float)random.NextDouble() * totalWeight;
            float cumulativeWeight = 0;
            
            int selectedModuleIndex = 0;
            for (int i = 0; i < weights.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (cumulativeWeight >= randomValue)
                {
                    selectedModuleIndex = i;
                    break;
                }
            }

            int selectedModule = cell.PossibleModules[selectedModuleIndex];
            cell.Collapse(selectedModule);

            // Propagate constraints to neighboring cells
            PropagateConstraints(position);
        }

        // Propagate constraints from the current cell to adjacent cells
        private void PropagateConstraints(Vector3Int position)
        {
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(position);

            // Process all positions in the queue until no more updates are needed
            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();
                Cell currentCell = grid[current.x, current.y, current.z];

                // Check each of the six neighboring cells (X+, X-, Y+, Y-, Z+, Z-)
                for (int d = 0; d < 6; d++)
                {
                    Vector3Int offset = Direction.GetOffset(d);
                    Vector3Int neighbor = current + offset;

                    // Skip if neighbor is outside grid bounds
                    if (neighbor.x < 0 || neighbor.x >= dimensions.x ||
                        neighbor.y < 0 || neighbor.y >= dimensions.y || 
                        neighbor.z < 0 || neighbor.z >= dimensions.z)
                        continue;

                    Cell neighborCell = grid[neighbor.x, neighbor.y, neighbor.z];
                    if (neighborCell.IsCollapsed)
                        continue;

                    bool cellChanged = false;
                    List<int> validModules = new List<int>();

                    // For each possible module in the neighbor
                    foreach (int neighborModule in neighborCell.PossibleModules)
                    {
                        bool isValid = false;

                        // For each module in the current cell, check if the constraints match
                        foreach (int currentModule in currentCell.PossibleModules)
                        {
                            if (moduleManager.AreCompatible(currentModule, neighborModule, d))
                            {
                                isValid = true;
                                break;
                            }
                        }

                        if (isValid)
                            validModules.Add(neighborModule);
                    }

                    // If we've eliminated some possible modules, update and propagate
                    if (validModules.Count < neighborCell.PossibleModules.Count)
                    {
                        neighborCell.PossibleModules = validModules;
                        cellChanged = true;
                    }

                    // If we've contradicted the rules (no valid modules), we need to restart
                    if (validModules.Count == 0)
                    {
                        Debug.LogWarning("Contradiction found at " + neighbor + " - you may need to restart generation");
                    }

                    // If we changed the neighbor cell, we need to propagate to its neighbors
                    if (cellChanged)
                        queue.Enqueue(neighbor);
                }
            }
        }

        // Run the WFC algorithm until all cells are collapsed
        public bool RunAlgorithm()
        {
            try
            {
                int iterations = 0;
                int maxIterations = dimensions.x * dimensions.y * dimensions.z * 10; // Safety limit
                
                while (iterations < maxIterations)
                {
                    // Get the cell with minimum entropy (most constrained)
                    Vector3Int? minEntropyCell = GetMinEntropyCell();
                    if (minEntropyCell == null)
                        break; // We're done!

                    // Collapse the cell with minimum entropy
                    CollapseCell(minEntropyCell.Value);
                    iterations++;
                }

                // Check if all cells have been successfully collapsed
                for (int x = 0; x < dimensions.x; x++)
                {
                    for (int y = 0; y < dimensions.y; y++)
                    {
                        for (int z = 0; z < dimensions.z; z++)
                        {
                            if (!grid[x, y, z].IsCollapsed || grid[x, y, z].PossibleModules.Count == 0)
                                return false; // Failed to solve
                        }
                    }
                }
                
                return true; // Success!
            }
            catch (Exception e)
            {
                Debug.LogError("Error in WFC algorithm: " + e.Message);
                return false;
            }
        }

        // Get the final collapsed grid for building generation
        public Dictionary<Vector3Int, int> GetCollapsedGrid()
        {
            Dictionary<Vector3Int, int> result = new Dictionary<Vector3Int, int>();
            
            for (int x = 0; x < dimensions.x; x++)
            {
                for (int y = 0; y < dimensions.y; y++)
                {
                    for (int z = 0; z < dimensions.z; z++)
                    {
                        Cell cell = grid[x, y, z];
                        if (cell.IsCollapsed && cell.PossibleModules.Count > 0)
                        {
                            result.Add(new Vector3Int(x, y, z), cell.PossibleModules[0]);
                        }
                    }
                }
            }
            
            return result;
        }
    }

    // Cell class for the WFC grid
    public class Cell
    {
        public List<int> PossibleModules { get; set; }
        public bool IsCollapsed => PossibleModules.Count <= 1;

        public Cell(List<int> possibleModules)
        {
            PossibleModules = new List<int>(possibleModules);
        }

        public void Collapse(int moduleId)
        {
            PossibleModules.Clear();
            PossibleModules.Add(moduleId);
        }
    }

    // Helper class for directions
    public static class Direction
    {
        // Directions: X+, X-, Y+, Y-, Z+, Z-
        private static readonly Vector3Int[] Offsets = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),
            new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(0, -1, 0),
            new Vector3Int(0, 0, 1),
            new Vector3Int(0, 0, -1)
        };

        // Get the opposite direction
        public static int GetOpposite(int direction)
        {
            return direction % 2 == 0 ? direction + 1 : direction - 1;
        }

        // Get the offset for a given direction
        public static Vector3Int GetOffset(int direction)
        {
            return Offsets[direction];
        }
    }

    // Building Module Manager that handles module compatibility and instantiation
    public class BuildingModuleManager
    {
        private List<BuildingModule> modules = new List<BuildingModule>();
        private Dictionary<int, GameObject> modulePrefabs = new Dictionary<int, GameObject>();
        private Dictionary<int, float> moduleWeights = new Dictionary<int, float>();
        
        // Compatibility matrix: [moduleA, moduleB, direction] => bool (can moduleA connect to moduleB in direction)
        private Dictionary<(int, int, int), bool> compatibilityRules = new Dictionary<(int, int, int), bool>();

        public void AddModule(BuildingModule module, GameObject prefab, float weight = 1.0f)
        {
            int moduleId = modules.Count;
            modules.Add(module);
            modulePrefabs[moduleId] = prefab;
            moduleWeights[moduleId] = weight;
        }
        
        public List<int> GetAllModuleIds()
        {
            List<int> ids = new List<int>();
            for (int i = 0; i < modules.Count; i++)
            {
                ids.Add(i);
            }
            return ids;
        }

        public float GetModuleWeight(int moduleId)
        {
            return moduleWeights.ContainsKey(moduleId) ? moduleWeights[moduleId] : 1.0f;
        }

        public void SetCompatibilityRule(int moduleA, int moduleB, int direction, bool compatible)
        {
            compatibilityRules[(moduleA, moduleB, direction)] = compatible;
        }

        public bool AreCompatible(int moduleA, int moduleB, int direction)
        {
            if (compatibilityRules.TryGetValue((moduleA, moduleB, direction), out bool result))
                return result;
                
            // By default, assume incompatible
            return false;
        }

        public GameObject GetModulePrefab(int moduleId)
        {
            return modulePrefabs.ContainsKey(moduleId) ? modulePrefabs[moduleId] : null;
        }
        
        public BuildingModule GetModule(int moduleId)
        {
            return moduleId >= 0 && moduleId < modules.Count ? modules[moduleId] : null;
        }
    }

    // Building module class representing a single building component
    [System.Serializable]
    public class BuildingModule
    {
        public string Name;
        public ModuleType Type;
        public ConnectionType[] Connections = new ConnectionType[6]; // X+, X-, Y+, Y-, Z+, Z-

        public BuildingModule(string name, ModuleType type)
        {
            Name = name;
            Type = type;
            
            // Initialize connections
            for (int i = 0; i < 6; i++)
            {
                Connections[i] = ConnectionType.None;
            }
        }
    }

    // Module types for organizing building elements
    public enum ModuleType
    {
        Floor,
        Wall,
        Window,
        Door,
        Roof,
        Corner,
        Column,
        Stairs,
        Decoration,
        Special
    }

    // Connection types that define how modules can connect
    public enum ConnectionType
    {
        None,
        Floor,
        Wall,
        Window,
        Door,
        Roof
    }

    // Building generator class that coordinates the generation process
    public class BuildingGenerator : MonoBehaviour
    {
        [Header("Generation Settings")]
        public Vector3Int buildingDimensions = new Vector3Int(5, 5, 5);
        public int seed = 0;
        public bool autoGenerate = false;
        public bool useGroundFloor = true;
        public bool useRoof = true;
        
        [Header("Module Prefabs")]
        public GameObject[] floorPrefabs;
        public GameObject[] wallPrefabs;
        public GameObject[] windowPrefabs;
        public GameObject[] doorPrefabs;
        public GameObject[] roofPrefabs;
        public GameObject[] cornerPrefabs;
        public GameObject[] stairPrefabs;
        
        private BuildingModuleManager moduleManager;
        private WaveFunctionCollapse wfc;
        private Transform buildingContainer;
        
        private void Start()
        {
            if (autoGenerate)
                GenerateBuilding();
        }
        
        public void GenerateBuilding()
        {
            CleanupExistingBuilding();
            InitializeModuleManager();
            
            // Create building container
            buildingContainer = new GameObject("Building").transform;
            buildingContainer.SetParent(transform);
            
            // Apply special constraints for ground floor and roof if needed
            ApplySpecialConstraints();
            
            // Create and run WFC algorithm
            wfc = new WaveFunctionCollapse(buildingDimensions, moduleManager, seed);
            bool success = wfc.RunAlgorithm();
            
            if (success)
            {
                InstantiateBuilding();
            }
            else
            {
                Debug.LogError("Building generation failed. Try a different seed or check module compatibility.");
            }
        }
        
        private void CleanupExistingBuilding()
        {
            // Destroy any existing building
            if (buildingContainer != null)
                Destroy(buildingContainer.gameObject);
        }
        
        private void InitializeModuleManager()
        {
            moduleManager = new BuildingModuleManager();
            
            // Register all module types and define compatibility rules
            RegisterModules();
            DefineCompatibilityRules();
        }
        
        private void RegisterModules()
        {
            // This method would set up all your building modules
            // Here's a simplified example for just a few module types
            
            // Floor modules
            for (int i = 0; i < floorPrefabs.Length; i++)
            {
                BuildingModule floorModule = new BuildingModule($"Floor_{i}", ModuleType.Floor);
                floorModule.Connections[0] = ConnectionType.Floor; // X+
                floorModule.Connections[1] = ConnectionType.Floor; // X-
                floorModule.Connections[2] = ConnectionType.Wall;  // Y+
                floorModule.Connections[3] = ConnectionType.Wall;  // Y-
                floorModule.Connections[4] = ConnectionType.Floor; // Z+
                floorModule.Connections[5] = ConnectionType.Floor; // Z-
                
                moduleManager.AddModule(floorModule, floorPrefabs[i], 1.0f);
            }
            
            // Wall modules
            for (int i = 0; i < wallPrefabs.Length; i++)
            {
                BuildingModule wallModule = new BuildingModule($"Wall_{i}", ModuleType.Wall);
                wallModule.Connections[0] = ConnectionType.Wall; // X+
                wallModule.Connections[1] = ConnectionType.Wall; // X-
                wallModule.Connections[2] = ConnectionType.Wall; // Y+
                wallModule.Connections[3] = ConnectionType.Floor; // Y-
                wallModule.Connections[4] = ConnectionType.Wall; // Z+
                wallModule.Connections[5] = ConnectionType.Wall; // Z-
                
                moduleManager.AddModule(wallModule, wallPrefabs[i], 0.8f);
            }
            
            // Window modules
            for (int i = 0; i < windowPrefabs.Length; i++)
            {
                BuildingModule windowModule = new BuildingModule($"Window_{i}", ModuleType.Window);
                windowModule.Connections[0] = ConnectionType.Wall; // X+
                windowModule.Connections[1] = ConnectionType.Wall; // X-
                windowModule.Connections[2] = ConnectionType.Wall; // Y+
                windowModule.Connections[3] = ConnectionType.Floor; // Y-
                windowModule.Connections[4] = ConnectionType.Window; // Z+
                windowModule.Connections[5] = ConnectionType.Window; // Z-
                
                moduleManager.AddModule(windowModule, windowPrefabs[i], 0.4f);
            }
            
            // Continue for other module types...
        }
        
        private void DefineCompatibilityRules()
        {
            // Define which modules can connect to each other
            // This would be an extensive set of rules based on your module design
            
            // For example, wall modules can connect to other wall modules, window modules, door modules, etc.
            List<int> allModuleIds = moduleManager.GetAllModuleIds();
            
            foreach (int moduleA in allModuleIds)
            {
                BuildingModule a = moduleManager.GetModule(moduleA);
                
                foreach (int moduleB in allModuleIds)
                {
                    BuildingModule b = moduleManager.GetModule(moduleB);
                    
                    // Check all 6 directions
                    for (int dir = 0; dir < 6; dir++)
                    {
                        ConnectionType connectionA = a.Connections[dir];
                        ConnectionType connectionB = b.Connections[Direction.GetOpposite(dir)];
                        
                        // Modules are compatible if they have the same connection type
                        bool compatible = connectionA != ConnectionType.None && connectionA == connectionB;
                        moduleManager.SetCompatibilityRule(moduleA, moduleB, dir, compatible);
                    }
                }
            }
        }
        
        private void ApplySpecialConstraints()
        {
            // Apply special constraints for ground floor, roof, etc.
            // This would force certain module types at specific positions
            
            // Ground floor and roof implementation would go here
        }
        
        private void InstantiateBuilding()
        {
            // Get the collapsed grid from WFC
            Dictionary<Vector3Int, int> collapsedGrid = wfc.GetCollapsedGrid();
            
            // Instantiate prefabs at each position
            foreach (var entry in collapsedGrid)
            {
                Vector3Int pos = entry.Key;
                int moduleId = entry.Value;
                
                GameObject prefab = moduleManager.GetModulePrefab(moduleId);
                if (prefab != null)
                {
                    Vector3 worldPos = new Vector3(pos.x, pos.y, pos.z);
                    GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, buildingContainer);
                    instance.name = $"Module_{pos.x}_{pos.y}_{pos.z}";
                }
            }
        }
    }

    // Editor for the building generator
    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BuildingGenerator))]
    public class BuildingGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            BuildingGenerator generator = (BuildingGenerator)target;
            
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
}
