// DestructibleBuilding.cs
using UnityEngine;

public class DestructibleBuilding : MonoBehaviour
{
    public GameObject destructionEffect; // Particle effect prefab

    public void DestroyBuilding()
    {
        // Spawn destruction effect
        if (destructionEffect)
        {
            Instantiate(destructionEffect, transform.position, Quaternion.identity);
        }

        // Destroy the building
        Destroy(gameObject);
    }
}