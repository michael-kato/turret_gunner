using System.Collections.Generic;
using UnityEngine;
using WFCBuildingGenerator;

// This component handles building visualization and presentation
public class BuildingVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public Material defaultMaterial;
    public bool applyRandomColors = true;
    public bool animateBuildingGeneration = true;
    public float buildingAnimationSpeed = 0.1f;
    
    [Header("Materials")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material windowMaterial;
    public Material doorMaterial;
    public Material roofMaterial;
    
    [Header("Post-processing")]
    public bool applyAmbientOcclusion = true;
    public bool generateLightingData = true;
    public Light mainLight;

    // Dictionary of materials by module type
    private Dictionary<ModuleType, Material> materialsByType = new Dictionary<ModuleType, Material>();
    
    // Original transformations for animation
    private Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();
    private Dictionary<Transform, Vector3> targetPositions = new Dictionary<Transform, Vector3>();
    private List<Transform> moduleTransforms = new List<Transform>();
    private bool isAnimating = false;
    private float animationProgress = 0f;

    private void Awake()
    {
        InitializeMaterials();
    }

    // Set up materials for each module type
    private void InitializeMaterials()
    {
        if (floorMaterial != null) materialsByType[ModuleType.Floor] = floorMaterial;
        if (wallMaterial != null) materialsByType[ModuleType.Wall] = wallMaterial;
        if (windowMaterial != null) materialsByType[ModuleType.Window] = windowMaterial;
        if (doorMaterial != null) materialsByType[ModuleType.Door] = doorMaterial;
        if (roofMaterial != null) materialsByType[ModuleType.Roof] = roofMaterial;
        
        // Add additional materials as needed
        if (defaultMaterial == null)
        {
            defaultMaterial = new Material(Shader.Find("Standard"));
            defaultMaterial.color = Color.white;
        }
    }

    // Apply visual enhancements to a generated building
    public void EnhanceBuilding(Transform buildingTransform, Dictionary<Vector3Int, ModuleType> moduleTypeGrid)
    {
        if (buildingTransform == null)
            return;
            
        // Clear previous data
        originalPositions.Clear();
        targetPositions.Clear();
        moduleTransforms.Clear();
        
        // Apply materials based on module type
        ApplyMaterials(buildingTransform, moduleTypeGrid);
        
        // Apply post-processing effects
        if (applyAmbientOcclusion)
            ApplyAmbientOcclusion(buildingTransform);
            
        if (generateLightingData)
            GenerateLightingData(buildingTransform);
            
        // Setup animation if enabled
        if (animateBuildingGeneration)
        {
            SetupBuildingAnimation(buildingTransform);
            StartAnimation();
        }
    }

    // Apply materials to all building modules based on their type
    private void ApplyMaterials(Transform buildingTransform, Dictionary<Vector3Int, ModuleType> moduleTypeGrid)
    {
        foreach (Transform child in buildingTransform)
        {
            // Try to extract position from name (format: "Module_X_Y_Z")
            string[] parts = child.name.Split('_');
            if (parts.Length >= 4)
            {
                if (int.TryParse(parts[1], out int x) &&
                    int.TryParse(parts[2], out int y) &&
                    int.TryParse(parts[3], out int z))
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (moduleTypeGrid.TryGetValue(pos, out ModuleType type))
                    {
                        ApplyMaterialToModule(child, type);
                    }
                }
            }
            
            // If we failed to get position/type data, apply a random color if enabled
            if (applyRandomColors && !child.name.Contains("Applied"))
            {
                ApplyRandomColor(child);
                child.name += "_Applied";
            }
        }
    }

    // Apply material to a module based on its type
    private void ApplyMaterialToModule(Transform module, ModuleType type)
    {
        Renderer[] renderers = module.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            if (materialsByType.TryGetValue(type, out Material material))
            {
                renderer.material = material;
            }
            else
            {
                renderer.material = defaultMaterial;
                
                // Apply random color variation if enabled
                if (applyRandomColors)
                {
                    ApplyRandomColor(module);
                }
            }
        }
    }

    // Apply a random color to a module
    private void ApplyRandomColor(Transform module)
    {
        Renderer[] renderers = module.GetComponentsInChildren<Renderer>();
        
        // Generate a random pastel color
        Color randomColor = new Color(
            Random.Range(0.5f, 0.9f),
            Random.Range(0.5f, 0.9f),
            Random.Range(0.5f, 0.9f)
        );
        
        foreach (Renderer renderer in renderers)
        {
            Material tempMaterial = new Material(renderer.material);
            tempMaterial.color = randomColor;
            renderer.material = tempMaterial;
        }
    }

    // Apply ambient occlusion to the building
    private void ApplyAmbientOcclusion(Transform buildingTransform)
    {
        // In a real implementation, this would use Unity's post-processing stack
        // or a custom ambient occlusion solution. This is a placeholder.
        Debug.Log("Ambient occlusion would be applied here in a real implementation");
    }

    // Generate lighting data for the building
    private void GenerateLightingData(Transform buildingTransform)
    {
        if (mainLight == null)
        {
            // Create a directional light if none is assigned
            GameObject lightObj = new GameObject("Building_DirectionalLight");
            lightObj.transform.SetParent(buildingTransform);
            
            mainLight = lightObj.AddComponent<Light>();
            mainLight.type = LightType.Directional;
            mainLight.intensity = 1.0f;
            mainLight.shadows = LightShadows.Soft;
            
            // Set a nice angle for the light
            lightObj.transform.rotation = Quaternion.Euler(50f, 30f, 0f);
        }
        
        // In a real implementation, this would bake lighting or set up real-time GI
        // This is a placeholder for demonstration
    }

    // Set up the animation for building generation
    private void SetupBuildingAnimation(Transform buildingTransform)
    {
        foreach (Transform child in buildingTransform)
        {
            moduleTransforms.Add(child);
            
            // Save target position (current position)
            targetPositions[child] = child.position;
            
            // Set initial position (below the building)
            Vector3 startPos = child.position;
            startPos.y -= 10f;
            child.position = startPos;
            
            originalPositions[child] = startPos;
        }
        
        // Sort modules by height for better animation effect
        moduleTransforms.Sort((a, b) => a.position.y.CompareTo(b.position.y));
        
        isAnimating = true;
        animationProgress = 0f;
    }

    // Start the building generation animation
    private void StartAnimation()
    {
        isAnimating = true;
        animationProgress = 0f;
    }

    private void Update()
    {
        if (isAnimating)
        {
            AnimateBuildingGeneration();
        }
    }

    // Animate the building generation
    private void AnimateBuildingGeneration()
    {
        if (animationProgress >= 1.0f)
        {
            isAnimating = false;
            return;
        }
        
        animationProgress += Time.deltaTime * buildingAnimationSpeed;
        
        float progress = Mathf.Clamp01(animationProgress);
        
        // Animate each module with a slight delay based on height
        foreach (Transform module in moduleTransforms)
        {
            if (originalPositions.TryGetValue(module, out Vector3 startPos) &&
                targetPositions.TryGetValue(module, out Vector3 endPos))
            {
                // Calculate a delay based on y-position (height)
                float heightRatio = Mathf.InverseLerp(0f, 10f, endPos.y);
                float delayedProgress = Mathf.Clamp01((progress - heightRatio * 0.5f) * 2f);
                
                // Apply easing for smoother animation
                float easedProgress = EaseOutBounce(delayedProgress);
                
                // Update position
                module.position = Vector3.Lerp(startPos, endPos, easedProgress);
            }
        }
    }

    // Easing function for animation
    private float EaseOutBounce(float x)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        
        if (x < 1 / d1)
        {
            return n1 * x * x;
        }
        else if (x < 2 / d1)
        {
            return n1 * (x -= 1.5f / d1) * x + 0.75f;
        }
        else if (x < 2.5 / d1)
        {
            return n1 * (x -= 2.25f / d1) * x + 0.9375f;
        }
        else
        {
            return n1 * (x -= 2.625f / d1) * x + 0.984375f;
        }
    }

    // Stop the animation and snap all modules to their final positions
    public void CompleteAnimation()
    {
        if (!isAnimating)
            return;
            
        foreach (Transform module in moduleTransforms)
        {
            if (targetPositions.TryGetValue(module, out Vector3 targetPos))
            {
                module.position = targetPos;
            }
        }
        
        isAnimating = false;
        animationProgress = 1.0f;
    }
}

#if UNITY_EDITOR
// Custom inspector
[UnityEditor.CustomEditor(typeof(BuildingVisualizer))]
public class BuildingVisualizerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        BuildingVisualizer visualizer = (BuildingVisualizer)target;
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Complete Animation"))
        {
            visualizer.CompleteAnimation();
        }
    }
}
#endif
