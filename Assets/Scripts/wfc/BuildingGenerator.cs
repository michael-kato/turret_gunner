using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaveFunctionCollapse : MonoBehaviour
{
    // Building block prefabs
    public GameObject bottomCornerPrefab;
    public GameObject bottomMiddlePrefab;
    public GameObject middleCornerPrefab;
    public GameObject middleMiddlePrefab;
    public GameObject roofMiddlePrefab;
    public GameObject topCornerPrefab;
    public GameObject topMiddlePrefab;
    
    // Grid dimensions
    public int gridWidth = 10;
    public int gridHeight = 10;
    public int gridDepth = 10;
    
    // Visualization speed
    public float collapseStepDelay = 0.2f;
    
    // Material for highlighting possible states
    public Material possibilityMaterial;
    public Material collapsedMaterial;
    
    // Reference to grid cells
    private Cell[,,] grid;
    
    // Module definitions with connection rules
    private List<Module> modules = new List<Module>();
    
    // Step-by-step visualization control
    private bool isAutoCollapsing = false;
    public bool isStepMode = false;
    public int currentStep = 0;
    
    // Enums for socket types
    public enum SocketType 
    {
        // Vertical connections
        BottomToMiddle,   // Bottom pieces connect upward to middle pieces
        MiddleToBottom,   // Middle pieces connect downward to bottom pieces
        MiddleToTop,      // Middle pieces connect upward to top/roof pieces
        TopToMiddle,      // Top pieces connect downward to middle pieces
        RoofToMiddle,     // Roof pieces connect downward to middle pieces
    
        // Horizontal connections
        BottomSide,       // Side connection for bottom pieces
        MiddleSide,       // Side connection for middle pieces
        TopSide,          // Side connection for top pieces
        RoofSide,         // Side connection for roof pieces
    
        // Corner specific
        BottomCorner,     // Corner connection for bottom pieces
        MiddleCorner,     // Corner connection for middle pieces  
        TopCorner         // Corner connection for top pieces
    }
    
    // Module class with support for multiple socket types per direction
    [System.Serializable]
    public class Module
    {
        public GameObject prefab;
        public string name;
        public float weight = 1.0f; 
        public Dictionary<Direction, List<SocketType>> sockets = new Dictionary<Direction, List<SocketType>>();
    
        // Store valid neighbor modules for each direction
        public Dictionary<Direction, List<int>> validNeighbors = new Dictionary<Direction, List<int>>();
    
        public Module(GameObject prefab, string name)
        {
            this.prefab = prefab;
            this.name = name;
        
            // Initialize directions
            foreach(Direction dir in Enum.GetValues(typeof(Direction)))
            {
                sockets[dir] = new List<SocketType>();
                validNeighbors[dir] = new List<int>();
            }
        }
    
        // Method to add a socket type to a direction
        public void AddSocket(Direction dir, SocketType socketType)
        {
            sockets[dir].Add(socketType);
        }
    
        // Create a rotated copy of this module
        public Module Rotate90()
        {
            Module rotated = new Module(prefab, name + "_r90");
        
            // Rotate sockets 90 degrees clockwise around Y axis
            foreach (var socketPair in sockets)
            {
                Direction newDir = RotateDirection(socketPair.Key);
                rotated.sockets[newDir] = new List<SocketType>(socketPair.Value);
            }
        
            return rotated;
        }
    
        private Direction RotateDirection(Direction dir)
        {
            switch(dir)
            {
                case Direction.North: return Direction.East;
                case Direction.East: return Direction.North;
                case Direction.South: return Direction.West;
                case Direction.West: return Direction.North;
                case Direction.Up: return Direction.Up;
                case Direction.Down: return Direction.Down;
                default: return dir;
            }
        }
    }
    
    // Enum for directions
    public enum Direction
    {
        North,
        East,
        South,
        West,
        Up,
        Down
    }
    
    // Get opposite direction
    private Direction GetOppositeDirection(Direction dir)
    {
        switch(dir)
        {
            case Direction.North: return Direction.South;
            case Direction.East: return Direction.West;
            case Direction.South: return Direction.North;
            case Direction.West: return Direction.East;
            case Direction.Up: return Direction.Down;
            case Direction.Down: return Direction.Up;
            default: return dir;
        }
    }
    
    // Cell class representing a grid position with possible module states
    public class Cell
    {
        public Vector3Int position;
        public int collapsedModuleIndex = -1;
        public GameObject instantiatedObject = null;
        public List<GameObject> possibilityVisuals = new List<GameObject>();
        
        public bool IsCollapsed => collapsedModuleIndex >= 0;

        private HashSet<int> _possibleModules = new HashSet<int>();
        public HashSet<int> possibleModules
        {
            get => _possibleModules;
            set
            {
                _possibleModules = value;
            }
        }
        
        public Cell(Vector3Int pos, int totalModules)
        {
            position = pos;
            
            // Initially all modules are possible
            for (int i = 0; i < totalModules; i++)
            {
                possibleModules.Add(i);
            }
        }
        
        public int GetEntropy()
        {
            return possibleModules.Count;
        }
    }
    
    void Start()
    {
        InitializeModules();
        GenerateCompatibilityRules();
        InitializeGrid();
        
        // would like to collapse the first cell to a corner piece for consistency in debugging (TODO)
        //CollapseCell(new Cell(Vector3Int.zero, 1));
        
        // UI will control whether to auto-collapse or step through
        if (!isStepMode)
        {
            StartCoroutine(CollapseAll());
        }
    }
    
    private void InitializeModules()
    {
        // Create base modules
        Module bottomCorner = CreateBottomCornerModule();
        bottomCorner.weight = 1.0f;
    
        Module bottomMiddle = CreateBottomMiddleModule();
        bottomMiddle.weight = 1.0f;
    
        Module middleCorner = CreateMiddleCornerModule();
        middleCorner.weight = 1.0f;
    
        Module middleMiddle = CreateMiddleMiddleModule();
        middleMiddle.weight = 1.0f;
    
        Module roofMiddle = CreateRoofMiddleModule();
        roofMiddle.weight = 1.0f;
    
        Module topCorner = CreateTopCornerModule();
        topCorner.weight = 1.0f;
    
        Module topMiddle = CreateTopMiddleModule();
        topMiddle.weight = 1.0f; 
        
        // Add base modules
        modules.Add(bottomCorner);
        modules.Add(bottomMiddle);
        modules.Add(middleCorner);
        modules.Add(middleMiddle);
        modules.Add(roofMiddle);
        modules.Add(topCorner);
        modules.Add(topMiddle);
        
        // Create a list of modules that need rotation
        List<Module> toRotate = new List<Module>();
        foreach (Module module in modules)
        {
            // Only rotate modules with asymmetric connections
            // For example, corner pieces need rotation, but a perfectly symmetric
            // middle piece might not need all rotations
            if (NeedsRotation(module))
            {
                toRotate.Add(module);
            }
        }
        
        // When adding rotations, copy the weight too
        foreach (Module module in toRotate)
        {
            Module mod90 = module.Rotate90();
            mod90.weight = module.weight;
        
            Module mod180 = mod90.Rotate90();
            mod180.weight = module.weight;
        
            Module mod270 = mod180.Rotate90();
            mod270.weight = module.weight;
        
            modules.Add(mod90);
            modules.Add(mod180);
            modules.Add(mod270);
        }
        
        // Debug info
        Debug.Log($"Total modules created: {modules.Count}");
        foreach (Module m in modules)
        {
            Debug.Log($"Module: {m.name}");
        }
    }

    // Determine if a module needs rotation
    private bool NeedsRotation(Module module)
    {
        // A module with different socket types in different horizontal directions needs rotation
        List<SocketType> northSockets = module.sockets[Direction.North];
        List<SocketType> eastSockets = module.sockets[Direction.East];
        List<SocketType> southSockets = module.sockets[Direction.South];
        List<SocketType> westSockets = module.sockets[Direction.West];
        
        // Check if any horizontal direction has different socket types
        bool needsRotation = !AreSocketListsEquivalent(northSockets, eastSockets) || 
                             !AreSocketListsEquivalent(northSockets, southSockets) || 
                             !AreSocketListsEquivalent(northSockets, westSockets);
                             
        return needsRotation;
    }

    // Compare two socket lists for equivalence
    private bool AreSocketListsEquivalent(List<SocketType> list1, List<SocketType> list2)
    {
        if (list1.Count != list2.Count)
            return false;
            
        // Check if both lists contain the same elements
        foreach (SocketType socket in list1)
        {
            if (!list2.Contains(socket))
                return false;
        }
        
        return true;
    }
    
    
    // For each module type, define its socket types
    private Module CreateBottomCornerModule()
    {
        Module m = new Module(bottomCornerPrefab, "bottom_corner");
        m.AddSocket(Direction.Up, SocketType.BottomToMiddle);
        m.AddSocket(Direction.North, SocketType.BottomSide);
        m.AddSocket(Direction.East, SocketType.BottomSide);
        return m;
    }

    private Module CreateBottomMiddleModule()
    {
        Module m = new Module(bottomMiddlePrefab, "bottom_middle");
        m.AddSocket(Direction.Up, SocketType.BottomToMiddle);
        m.AddSocket(Direction.East, SocketType.BottomSide);
        m.AddSocket(Direction.West, SocketType.BottomSide);
        return m;
    }

    private Module CreateMiddleCornerModule() 
    {
        Module m = new Module(middleCornerPrefab, "middle_corner");
        m.AddSocket(Direction.Up, SocketType.MiddleToTop);
        m.AddSocket(Direction.Down, SocketType.MiddleToBottom);
        m.AddSocket(Direction.North, SocketType.MiddleSide);
        m.AddSocket(Direction.East, SocketType.MiddleSide);
        return m;
    }

    private Module CreateMiddleMiddleModule()
    {
        Module m = new Module(middleMiddlePrefab, "middle_middle");
        m.AddSocket(Direction.Up, SocketType.MiddleToTop);
        m.AddSocket(Direction.Down, SocketType.MiddleToBottom);
        m.AddSocket(Direction.North, SocketType.MiddleSide);
        return m;
    }

    private Module CreateRoofMiddleModule()
    {
        Module m = new Module(roofMiddlePrefab, "roof_middle");
        m.AddSocket(Direction.Down, SocketType.RoofToMiddle);
        m.AddSocket(Direction.North, SocketType.RoofSide);
        m.AddSocket(Direction.East, SocketType.RoofSide);
        m.AddSocket(Direction.South, SocketType.RoofSide);
        m.AddSocket(Direction.West, SocketType.RoofSide);
        return m;
    }

    private Module CreateTopCornerModule()
    {
        Module m = new Module(topCornerPrefab, "top_corner");
        m.AddSocket(Direction.Down, SocketType.TopToMiddle);
        m.AddSocket(Direction.North, SocketType.TopSide);
        m.AddSocket(Direction.East, SocketType.TopSide);
        return m;
    }

    private Module CreateTopMiddleModule()
    {
        Module m = new Module(topMiddlePrefab, "top_middle");
        m.AddSocket(Direction.Down, SocketType.TopToMiddle);
        m.AddSocket(Direction.North, SocketType.TopSide);
        return m;
    }
    
    // Generate compatibility rules between modules based on socket types
    private void GenerateCompatibilityRules()
    {
        // For each module
        for (int i = 0; i < modules.Count; i++)
        {
            // For each direction from this module
            foreach(Direction dir in Enum.GetValues(typeof(Direction)))
            {
                // Clear previous valid neighbors for this direction
                modules[i].validNeighbors[dir] = new List<int>();
                
                // If this module has sockets in this direction
                if (modules[i].sockets[dir].Count > 0)
                {
                    // For each potential neighbor module
                    for (int j = 0; j < modules.Count; j++)
                    {
                        // Check the opposite direction of the neighbor
                        Direction oppositeDir = GetOppositeDirection(dir);
                        
                        // If neighbor has sockets in the opposite direction
                        if (modules[j].sockets[oppositeDir].Count > 0)
                        {
                            // Check if any socket pairs are compatible
                            bool compatible = false;
                            
                            foreach (SocketType socketType in modules[i].sockets[dir])
                            {
                                foreach (SocketType neighborSocketType in modules[j].sockets[oppositeDir])
                                {
                                    if (AreSocketsCompatible(socketType, neighborSocketType))
                                    {
                                        compatible = true;
                                        break;
                                    }
                                }
                                
                                if (compatible) break;
                            }
                            
                            // If compatible, add as valid neighbor
                            if (compatible)
                            {
                                modules[i].validNeighbors[dir].Add(j);
                            }
                        }
                    }
                }
                
                // Verify we have at least one valid neighbor in directions with sockets
                if (modules[i].sockets[dir].Count > 0 && modules[i].validNeighbors[dir].Count == 0)
                {
                    Debug.LogWarning($"Module {modules[i].name} has no valid neighbors in direction {dir}");
                }
            }
        }
    }
    
    // Define socket compatibility rules
    private bool AreSocketsCompatible(SocketType socket1, SocketType socket2)
    {
        // Vertical connections
        if (socket1 == SocketType.BottomToMiddle && socket2 == SocketType.MiddleToBottom) return true;
        if (socket1 == SocketType.MiddleToBottom && socket2 == SocketType.BottomToMiddle) return true;
        if (socket1 == SocketType.MiddleToTop && socket2 == SocketType.TopToMiddle) return true;
        if (socket1 == SocketType.MiddleToTop && socket2 == SocketType.RoofToMiddle) return true;
        if (socket1 == SocketType.TopToMiddle && socket2 == SocketType.MiddleToTop) return true;
        if (socket1 == SocketType.RoofToMiddle && socket2 == SocketType.MiddleToTop) return true;
    
        // Horizontal connections - only same types can connect
        if (socket1 == SocketType.BottomSide && socket2 == SocketType.BottomSide) return true;
        if (socket1 == SocketType.MiddleSide && socket2 == SocketType.MiddleSide) return true;
        if (socket1 == SocketType.TopSide && socket2 == SocketType.TopSide) return true;
        if (socket1 == SocketType.RoofSide && socket2 == SocketType.RoofSide) return true;
    
        // Corner connections
        if (socket1 == SocketType.BottomCorner && socket2 == SocketType.BottomCorner) return true;
        if (socket1 == SocketType.MiddleCorner && socket2 == SocketType.MiddleCorner) return true;
        if (socket1 == SocketType.TopCorner && socket2 == SocketType.TopCorner) return true;
    
        return false;
    }
    
    // Initialize the grid with all possibilities
    private void InitializeGrid()
    {
        grid = new Cell[gridWidth, gridHeight, gridDepth];
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    grid[x, y, z] = new Cell(new Vector3Int(x, y, z), modules.Count);
                }
            }
        }
        
        // Apply initial constraints (e.g., bottom layer can only be bottom_* modules)
        ApplyInitialConstraints();
    }
    
    // Apply initial constraints to the grid
    private void ApplyInitialConstraints()
    {
        // Bottom layer can only be bottom_* modules
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Cell cell = grid[x, 0, z];
                
                // Find indices of bottom modules
                HashSet<int> bottomModuleIndices = new HashSet<int>();
                for (int i = 0; i < modules.Count; i++)
                {
                    if (modules[i].name.Contains("bottom_"))
                    {
                        bottomModuleIndices.Add(i);
                    }
                }
                
                // Make sure we have valid modules
                if (bottomModuleIndices.Count == 0)
                {
                    Debug.LogError("No bottom modules found!");
                    return;
                }
                
                // Apply constraint
                cell.possibleModules.IntersectWith(bottomModuleIndices);
                
                if (cell.possibleModules.Count == 0)
                {
                    Debug.LogError($"Cell at (${x}, 0, ${z}) has no valid modules after bottom layer constraint!");
                    return;
                }
            }
        }
        
        // Similar constraints for top layer, edges, etc.
        // ...
        
        // Initial propagation
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    Cell cell = grid[x, y, z];
                    if (cell.possibleModules.Count < modules.Count)
                    {
                        TryPropagateConstraints(cell);
                    }
                }
            }
        }
        
        // Verify grid is consistent
        VerifyGridConsistency();
    }

    // Verify that all cells have at least one possible module
    private void VerifyGridConsistency()
    {
        bool isConsistent = true;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    Cell cell = grid[x, y, z];
                    if (cell.possibleModules.Count == 0)
                    {
                        Debug.LogError($"Inconsistent grid: Cell at ({x}, {y}, {z}) has no valid modules!");
                        isConsistent = false;
                    }
                }
            }
        }
        
        if (isConsistent)
        {
            Debug.Log("Grid verified: All cells have at least one possible module.");
        }
    }
    
    // Coroutine to collapse all cells
    private IEnumerator CollapseAll()
    {
        isAutoCollapsing = true;
        
        while (true)
        {
            // Find uncollapsed cell with minimum entropy
            Cell minEntropyCell = FindCellWithMinimumEntropy();
            
            // If all cells are collapsed, we're done
            if (minEntropyCell == null)
                break;
            
            // Collapse this cell and propagate constraints
            CollapseCell(minEntropyCell);
            yield return new WaitForSeconds(collapseStepDelay);
        }
        
        isAutoCollapsing = false;
    }
    
    // Step-by-step collapse (called by UI)
    public void StepCollapse()
    {
        // Find uncollapsed cell with minimum entropy
        Cell minEntropyCell = FindCellWithMinimumEntropy();
        
        // If all cells are collapsed, we're done
        if (minEntropyCell == null)
            return;
        
        // Collapse this cell and propagate constraints
        CollapseCell(minEntropyCell);
        currentStep++;
    }
    
    // Find cell with minimum entropy (fewest possibilities)
    private Cell FindCellWithMinimumEntropy()
    {
        Cell minEntropyCell = null;
        int minEntropy = int.MaxValue;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    Cell cell = grid[x, y, z];
                    
                    // Skip already collapsed cells
                    if (cell.IsCollapsed)
                        continue;
                    
                    int entropy = cell.GetEntropy();
                    
                    // If cell has no possibilities, the algorithm has failed
                    if (entropy == 0)
                    {
                        Debug.LogWarning("Contradiction found! Cell has no possible modules.");
                        return null;
                    }
                    
                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        minEntropyCell = cell;
                    }
                }
            }
        }
        
        return minEntropyCell;
    }
    
    // Backtracking system to handle contradictions
    private Stack<BacktrackState> backtrackStack = new Stack<BacktrackState>();

    // State for backtracking
    private class BacktrackState
    {
        public Vector3Int position;
        public HashSet<int> originalPossibilities;
        public int chosenModule;
        
        public BacktrackState(Vector3Int pos, HashSet<int> possibilities, int chosen)
        {
            position = pos;
            originalPossibilities = new HashSet<int>(possibilities);
            chosenModule = chosen;
        }
    }

    // Modified collapse method with backtracking
    private bool CollapseCell(Cell cell)
    {
        // Make a copy of original possibilities for backtracking
        HashSet<int> originalPossibilities = new HashSet<int>(cell.possibleModules);
        
        // Choose a random module from possible ones
        int moduleIndex = ChooseRandomModule(cell.possibleModules);
        
        // Save state for backtracking
        backtrackStack.Push(new BacktrackState(cell.position, originalPossibilities, moduleIndex));
        
        // Collapse cell to this module
        cell.collapsedModuleIndex = moduleIndex;
        cell.possibleModules.Clear();
        cell.possibleModules.Add(moduleIndex);
        
        // Instantiate the chosen module
        InstantiateModule(cell);
        
        // Clear possibility visualizations
        ClearPossibilityVisuals(cell);
        
        // Propagate constraints to neighbors
        if (!TryPropagateConstraints(cell))
        {
            // Contradiction occurred - backtrack
            Debug.Log("Contradiction occurred - backtracking...");
            Backtrack();
            return false;
        }
        
        return true;
    }

    // Modified propagation that returns success/failure
    private bool TryPropagateConstraints(Cell startCell)
    {
        // Queue of cells to process
        Queue<Vector3Int> cellsToProcess = new Queue<Vector3Int>();
        cellsToProcess.Enqueue(startCell.position);
        
        // Keep track of visited cells to avoid infinite loops
        HashSet<Vector3Int> visitedCells = new HashSet<Vector3Int>();
        
        // Process until queue is empty
        while (cellsToProcess.Count > 0)
        {
            Vector3Int pos = cellsToProcess.Dequeue();
            
            // Skip if already visited
            if (!visitedCells.Add(pos))  // Returns false if already in set
                continue;
                
            Cell cell = grid[pos.x, pos.y, pos.z];
            
            // For each direction
            foreach(Direction dir in Enum.GetValues(typeof(Direction)))
            {
                // Get neighbor position
                Vector3Int neighborPos = GetNeighborPosition(pos, dir);
                
                // Skip if out of bounds
                if (!IsInBounds(neighborPos))
                    continue;
                
                Cell neighborCell = grid[neighborPos.x, neighborPos.y, neighborPos.z];
                
                // If neighbor is already collapsed, skip it
                if (neighborCell.IsCollapsed)
                    continue;
                    
                // Get compatible modules for this direction
                HashSet<int> supportedNeighborModules = new HashSet<int>();
                
                foreach (int moduleIndex in cell.possibleModules)
                {
                    if (modules[moduleIndex].validNeighbors.TryGetValue(dir, out List<int> validNeighbors))
                    {
                        foreach (int neighborIndex in validNeighbors)
                        {
                            supportedNeighborModules.Add(neighborIndex);
                        }
                    }
                }
                
                // Calculate the intersection with neighbor's current possibilities
                int countBefore = neighborCell.possibleModules.Count;
                
                HashSet<int> intersection = new HashSet<int>(neighborCell.possibleModules);
                intersection.IntersectWith(supportedNeighborModules);
                
                // Check for contradiction
                if (intersection.Count == 0)
                {
                    // This is a genuine contradiction - no valid modules remain
                    if (supportedNeighborModules.Count > 0 && neighborCell.possibleModules.Count > 0)
                    {
                        Debug.LogWarning($"Contradiction: Cell at {neighborPos} has no valid modules after constraint from {pos}");
                        return false;
                    }
                }
                else
                {
                    // Update possibilities
                    if (neighborCell.possibleModules.Count != intersection.Count)
                    {
                        neighborCell.possibleModules = intersection;
                    }
                    
                    // If neighbor's possibilities changed, add it to the queue
                    if (countBefore != intersection.Count)
                    {
                        cellsToProcess.Enqueue(neighborPos);
                    }
                }
            }
        }
        
        return true;
    }

    // Backtrack to previous state when contradiction occurs
    private void Backtrack()
    {
        if (backtrackStack.Count == 0)
        {
            Debug.LogError("Backtracking failed - no states to restore!");
            Reset(); // Complete reset as fallback
            return;
        }
        
        // Get last state
        BacktrackState state = backtrackStack.Pop();
        Vector3Int pos = state.position;
        Cell cell = grid[pos.x, pos.y, pos.z];
        
        // Remove the last chosen module from possibilities
        HashSet<int> newPossibilities = new HashSet<int>(state.originalPossibilities);
        newPossibilities.Remove(state.chosenModule);
        
        // Check if we still have possibilities
        if (newPossibilities.Count == 0)
        {
            Debug.LogWarning("No more possibilities during backtracking - trying deeper backtrack");
            Backtrack(); // Recursive backtrack
            return;
        }
        
        // Reset the cell
        cell.collapsedModuleIndex = -1;
        cell.possibleModules = newPossibilities;
        
        // Clear visuals
        if (cell.instantiatedObject != null)
        {
            Destroy(cell.instantiatedObject);
            cell.instantiatedObject = null;
        }
        ClearPossibilityVisuals(cell);
        
        // Reset all cells affected by this one
        // This is a simplification - in a real implementation, you would
        // save and restore the exact state of all affected cells
        Reset(false); // Partial reset - keep backtracking stack
        
        // Re-initialize the grid and propagate initial constraints
        InitializeGrid();
        
        // Force a new collapse to continue the algorithm
        Cell minEntropyCell = FindCellWithMinimumEntropy();
        if (minEntropyCell != null)
        {
            CollapseCell(minEntropyCell);
        }
    }

    // Modified reset method with option to keep backtracking stack
    private void Reset(bool clearBacktrackStack = true)
    {
        // Stop any running processes
        StopAllCoroutines();
        isAutoCollapsing = false;
        currentStep = 0;
        
        if (clearBacktrackStack)
        {
            backtrackStack.Clear();
        }
        
        // Clear the grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        ClearPossibilityVisuals(grid[x, y, z]);
                        if (grid[x, y, z].instantiatedObject != null)
                        {
                            Destroy(grid[x, y, z].instantiatedObject);
                        }
                    }
                }
            }
        }
        
        // Reinitialize
        InitializeGrid();
    }
    

    // Choose random module with weighting
    private int ChooseRandomModule(HashSet<int> possibilities)
    {
        List<int> possibilityList = possibilities.ToList();
    
        // Calculate total weight
        float totalWeight = 0;
        foreach (int index in possibilityList)
        {
            totalWeight += modules[index].weight;
        }
    
        // Choose random weighted value
        float randomValue = UnityEngine.Random.Range(0, totalWeight);
    
        // Find which module this value corresponds to
        float cumulativeWeight = 0;
        foreach (int index in possibilityList)
        {
            cumulativeWeight += modules[index].weight;
            if (randomValue <= cumulativeWeight)
            {
                return index;
            }
        }
    
        // Fallback (should never happen)
        return possibilityList[0];
    }
    
    // Instantiate the module for a collapsed cell
    private void InstantiateModule(Cell cell)
    {
        // Remove old object if it exists
        if (cell.instantiatedObject != null)
        {
            Destroy(cell.instantiatedObject);
        }
        
        // Create new object
        Module module = modules[cell.collapsedModuleIndex];
        Vector3 position = new Vector3(cell.position.x, cell.position.y, cell.position.z);
        
        cell.instantiatedObject = Instantiate(module.prefab, position, Quaternion.identity);
        
        // Apply rotation based on module variant
        string name = module.name;
        if (name.Contains("_r90"))
        {
            cell.instantiatedObject.transform.rotation = Quaternion.Euler(0, 90, 0);
            cell.instantiatedObject.name += "_r90";
        }
        else if (name.Contains("_r180"))
        {
            cell.instantiatedObject.transform.rotation = Quaternion.Euler(0, 180, 0);
            cell.instantiatedObject.name += "_r180";
        }
        else if (name.Contains("_r270"))
        {
            cell.instantiatedObject.transform.rotation = Quaternion.Euler(0, 270, 0);
            cell.instantiatedObject.name += "_r270";
        }
        // Apply material to show it's collapsed
        ApplyMaterialToGameObject(cell.instantiatedObject, collapsedMaterial);
    }
    
    // Apply material to all renderers in a GameObject hierarchy
    private void ApplyMaterialToGameObject(GameObject obj, Material material)
    {
        if (material == null) return;
        
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.materials = materials;
        }
    }
    
    // Clear possibility visualizations for a cell
    private void ClearPossibilityVisuals(Cell cell)
    {
        foreach (GameObject obj in cell.possibilityVisuals)
        {
            Destroy(obj);
        }
        cell.possibilityVisuals.Clear();
    }
    
    // Update possibility visualizations for all cells (called by UI)
    public void UpdateAllPossibilityVisuals()
    {
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    Cell cell = grid[x, y, z];
                    
                    // Skip collapsed cells
                    if (cell.IsCollapsed)
                        continue;
                    
                    UpdateCellPossibilityVisuals(cell);
                }
            }
        }
    }
    
    // Update possibility visualizations for a specific cell
    private void UpdateCellPossibilityVisuals(Cell cell)
    {
        // Clear old visualizations
        ClearPossibilityVisuals(cell);
        
        // Don't show for collapsed cells
        if (cell.IsCollapsed)
            return;
        
        // Show a small version of each possible module
        Vector3 position = new Vector3(cell.position.x, cell.position.y, cell.position.z);
        float scale = 0.2f;
        
        int possibilityCount = cell.possibleModules.Count;
        int i = 0;
        
        foreach (int moduleIndex in cell.possibleModules)
        {
            // Calculate position offset based on possibility index
            float angleStep = 360f / possibilityCount;
            float angle = i * angleStep;
            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * 0.3f,
                0.5f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * 0.3f
            );
            
            // Create visualization
            GameObject visual = Instantiate(
                modules[moduleIndex].prefab, 
                position + offset, 
                Quaternion.identity
            );
            
            // Apply scaling
            visual.transform.localScale = new Vector3(scale, scale, scale);
            
            // Apply rotation based on module variant
            string name = modules[moduleIndex].name;
            if (name.Contains("_r90"))
                visual.transform.rotation = Quaternion.Euler(0, 90, 0);
            else if (name.Contains("_r180"))
                visual.transform.rotation = Quaternion.Euler(0, 180, 0);
            else if (name.Contains("_r270"))
                visual.transform.rotation = Quaternion.Euler(0, 270, 0);
            
            // Apply special material
            ApplyMaterialToGameObject(visual, possibilityMaterial);
            
            // Add to list
            cell.possibilityVisuals.Add(visual);
            
            i++;
        }
    }
    
    // Get position of neighbor in a specific direction
    private Vector3Int GetNeighborPosition(Vector3Int pos, Direction dir)
    {
        switch(dir)
        {
            case Direction.North: return new Vector3Int(pos.x, pos.y, pos.z + 1);
            case Direction.East: return new Vector3Int(pos.x + 1, pos.y, pos.z);
            case Direction.South: return new Vector3Int(pos.x, pos.y, pos.z - 1);
            case Direction.West: return new Vector3Int(pos.x - 1, pos.y, pos.z);
            case Direction.Up: return new Vector3Int(pos.x, pos.y + 1, pos.z);
            case Direction.Down: return new Vector3Int(pos.x, pos.y - 1, pos.z);
            default: return pos;
        }
    }
    
    // Check if position is within grid bounds
    private bool IsInBounds(Vector3Int pos)
    {
        return pos.x >= 0 && pos.x < gridWidth &&
               pos.y >= 0 && pos.y < gridHeight &&
               pos.z >= 0 && pos.z < gridDepth;
    }
    
    // UI Functions for step-by-step visualization
    public void ToggleStepMode()
    {
        isStepMode = !isStepMode;
        
        if (!isStepMode && !isAutoCollapsing)
        {
            // Switching from step mode to auto mode
            StartCoroutine(CollapseAll());
        }
        else if (isStepMode && isAutoCollapsing)
        {
            // Switching from auto mode to step mode
            StopAllCoroutines();
            isAutoCollapsing = false;
        }
    }
    
    public void Reset()
    {
        // Stop any running processes
        StopAllCoroutines();
        isAutoCollapsing = false;
        currentStep = 0;
        
        // Clear the grid
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    if (grid[x, y, z] != null)
                    {
                        ClearPossibilityVisuals(grid[x, y, z]);
                        if (grid[x, y, z].instantiatedObject != null)
                        {
                            Destroy(grid[x, y, z].instantiatedObject);
                        }
                    }
                }
            }
        }
        
        // Reinitialize
        InitializeGrid();
    }
}