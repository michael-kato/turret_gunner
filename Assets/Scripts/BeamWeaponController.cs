// BeamWeapon.cs
using UnityEngine;

public class BeamWeapon :  MonoBehaviour 
{
    public LineRenderer beamLine; // Attach a LineRenderer for the beam
    public float beamRange = 5000f;
    public LayerMask destructibleLayer;
    public GameObject explosionEffect; // Drag your VFX prefab here in the inspector
    public AudioManager audioManager;

    private Transform _beamTransform;

    private void Start()
    {
        _beamTransform = beamLine.transform;
        audioManager = new AudioManager();
    }
    
    private void Update()
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
                Instantiate(explosionEffect, hit.point, Quaternion.identity);
                //audioManager.PlayExplosionSound();
            }
            
            // Destroy the hit building
            if (hit.collider.CompareTag("Destructible"))
            {
                Destroy(hit.collider.gameObject);
            }
        }
        else
        {
            beamLine.SetPosition(1, _beamTransform.position + _beamTransform.forward * beamRange);
        }
    }
}