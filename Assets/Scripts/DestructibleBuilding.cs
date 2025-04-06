// DestructibleBuilding.cs
using UnityEngine;

public class DestructibleBuilding : MonoBehaviour
{
    public GameObject destructionEffect;
    private Quaternion _zUp;

    public void Start()
    {
        _zUp = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }

    public void DestroyBuilding()
    {
        // Spawn destruction effect
        if (destructionEffect)
        {
            Instantiate(destructionEffect, transform.position, _zUp);
        }

        // Destroy the building
        Destroy(gameObject);
    }
}