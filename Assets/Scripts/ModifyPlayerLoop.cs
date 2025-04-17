using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;

public class ModifyPlayerLoop : MonoBehaviour
{
    public void Start()
    {
        // Get the current player loop
        PlayerLoopSystem playerLoop = PlayerLoop.GetCurrentPlayerLoop();
        
        // Modify the player loop
        PlayerLoopSystem modifiedPlayerLoop = ModifyVFXUpdateSystem(playerLoop);
        
        // Set the modified player loop
        PlayerLoop.SetPlayerLoop(modifiedPlayerLoop);
        
        // Debug log to verify changes
        Debug.Log("Modified player loop - targeted VFXUpdate subsystem");
    }
    
    private PlayerLoopSystem ModifyVFXUpdateSystem(PlayerLoopSystem rootSystem)
    {
        // Process all subsystems at current level
        if (rootSystem.subSystemList != null)
        {
            for (int i = 0; i < rootSystem.subSystemList.Length; i++)
            {
                // Check if this is the PostLateUpdate system
                if (rootSystem.subSystemList[i].type == typeof(PostLateUpdate))
                {
                    PlayerLoopSystem postLateUpdateSystem = rootSystem.subSystemList[i];
                    
                    // Process PostLateUpdate subsystems
                    if (postLateUpdateSystem.subSystemList != null)
                    {
                        for (int j = 0; j < postLateUpdateSystem.subSystemList.Length; j++)
                        {
                            // Find the VFXUpdate subsystem specifically
                            if (postLateUpdateSystem.subSystemList[j].type == typeof(PostLateUpdate.VFXUpdate))
                            {
                                // Store the original VFXUpdate system
                                PlayerLoopSystem vfxUpdateSystem = postLateUpdateSystem.subSystemList[j];
                                
                                // Create our custom before/after systems
                                var beforeVFXSystem = new PlayerLoopSystem
                                {
                                    type = typeof(BeforeVFXUpdateSystem),
                                    updateDelegate = BeforeVFXUpdateFunction
                                };
                                
                                var afterVFXSystem = new PlayerLoopSystem
                                {
                                    type = typeof(AfterVFXUpdateSystem),
                                    updateDelegate = AfterVFXUpdateFunction
                                };
                                
                                // Create a new wrapped VFX system
                                var wrappedVFXSystem = new PlayerLoopSystem
                                {
                                    type = typeof(WrappedVFXUpdateSystem),
                                    updateDelegate = null,
                                    subSystemList = new PlayerLoopSystem[]
                                    {
                                        beforeVFXSystem,
                                        vfxUpdateSystem,
                                        afterVFXSystem
                                    }
                                };
                                
                                // Replace the original VFXUpdate with our wrapped version
                                postLateUpdateSystem.subSystemList[j] = wrappedVFXSystem;
                                rootSystem.subSystemList[i].subSystemList = postLateUpdateSystem.subSystemList;
                                
                                Debug.Log("Successfully wrapped VFXUpdate system!");
                                return rootSystem;
                            }
                        }
                    }
                }
                
                // Recursively process this subsystem's children
                if (rootSystem.subSystemList[i].subSystemList != null && 
                    rootSystem.subSystemList[i].subSystemList.Length > 0)
                {
                    rootSystem.subSystemList[i] = ModifyVFXUpdateSystem(rootSystem.subSystemList[i]);
                }
            }
        }
        
        return rootSystem;
    }
    
    // Define system types
    private class BeforeVFXUpdateSystem { }
    private class AfterVFXUpdateSystem { }
    private class WrappedVFXUpdateSystem { }
    
    // Define our custom system functions
    static void BeforeVFXUpdateFunction()
    {
        // Logic to execute before the VFX update
        Debug.Log("Running before VFXUpdate");
        
        // Example: Prepare for VFX processing
        VFXMonitor.StartProfiling();
    }
    
    static void AfterVFXUpdateFunction()
    {
        // Logic to execute after the VFX update
        VFXStats stats = VFXMonitor.EndProfiling();
        
        // Example: Process VFX metrics
        if (stats.particleCount > 10000)
        {
            Debug.LogWarning($"High particle count detected: {stats.particleCount}");
        }
        
        if (stats.executionTimeMs > 5.0f)
        {
            Debug.LogWarning($"VFX processing took too long: {stats.executionTimeMs}ms");
        }
        
        Debug.Log("VFX update complete - " +
                 $"Active systems: {stats.activeSystemCount}, " +
                 $"Particles: {stats.particleCount}, " +
                 $"Time: {stats.executionTimeMs}ms");
    }
}

// VFX monitoring utility
public static class VFXMonitor
{
    private static float startTime;
    private static readonly VFXStats stats = new VFXStats();
    
    public static void StartProfiling()
    {
        startTime = Time.realtimeSinceStartup;
        
        // Reset stats for this frame
        stats.activeSystemCount = 0;
        stats.particleCount = 0;
        
        // Count active particle systems
        ParticleSystem[] activeSystems = GameObject.FindObjectsOfType<ParticleSystem>();
        stats.activeSystemCount = activeSystems.Length;
        
        // Count total particles
        foreach (ParticleSystem ps in activeSystems)
        {
            stats.particleCount += ps.particleCount;
        }
    }
    
    public static VFXStats EndProfiling()
    {
        stats.executionTimeMs = (Time.realtimeSinceStartup - startTime) * 1000f;
        return stats;
    }
}

// Data structure for VFX statistics
public class VFXStats
{
    public int activeSystemCount;
    public int particleCount;
    public float executionTimeMs;
}