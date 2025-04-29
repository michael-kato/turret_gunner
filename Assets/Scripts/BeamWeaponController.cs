// BeamWeapon.cs
using UnityEngine;
using System.Collections.Generic;

public class BeamWeapon :  MonoBehaviour 
{
    public LineRenderer beamLine; // Attach a LineRenderer for the beam
    public float beamRange = 5000f;
    public LayerMask destructibleLayer;
    public GameObject explosionEffect; // Drag your VFX prefab here in the inspector
    public AudioManager audioManager;
    

    private Transform _beamTransform;
    private Quaternion _zUp;
    private Dictionary<int, DestructibleBuilding> _hits;
    
    private void Start()
    {
        _beamTransform = beamLine.transform;
        audioManager = new AudioManager();
        _zUp = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        _hits = new();
    }
    
    private void LateUpdate()
    {
        // Fire beam when left mouse button is pressed
        if (Input.GetMouseButton(0))
        {
            FireBeam();
        }
        else
        {
            if (beamLine.enabled)
            {
                beamLine.enabled = false;
            }
        }
    }

    void FireBeam()
    {
        // Show beam line
        beamLine.enabled = true;
        beamLine.SetPosition(0, _beamTransform.position);

        RaycastHit hit;
        if (Physics.Raycast(_beamTransform.position, _beamTransform.forward, out hit, beamRange, destructibleLayer))
        {
            beamLine.SetPosition(1, hit.point);
            
            // Spawn explosion effect at the hit point
            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, hit.point, _zUp);
                //audioManager.PlayExplosionSound();
            }
            
            // Destroy the hit building
            if (hit.collider.CompareTag("Destructible"))
            {
                GameObject go = hit.collider.gameObject;
                int goId = go.GetInstanceID();
                if (!_hits.TryGetValue(goId, out DestructibleBuilding destructible))
                {
                    destructible = go.GetComponent<DestructibleBuilding>();
                    _hits[goId] = destructible;
                }
                destructible.ApplyDamage();
            }
        }
        else
        {
            beamLine.SetPosition(1, _beamTransform.position + _beamTransform.forward * beamRange);
        }
    }
}