// DestructibleBuilding.cs
using UnityEngine;

public class DestructibleBuilding : MonoBehaviour
{
    private int _health = 10;
    public GameObject destructionEffect;
    private Quaternion _zUp;

    public void Start()
    {
        _zUp = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }

    public void ApplyDamage()
    {
        _health -= 1;
        if (_health == 0)
        {
            DestroyBuilding();
        }
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