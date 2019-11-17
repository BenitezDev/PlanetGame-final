using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLaserManager : MonoBehaviour
{
    private bool leftLaser;
    
    public GameObject[] lasers;

    public Transform crosshair;

    private float fireRate;

    private void Awake()
    {
        fireRate = GetComponentInParent<MachineGun>().fireRate;
        foreach( var laser in lasers)
        {
            laser.transform.LookAt(crosshair);
        }
    }

    public IEnumerator ShootLaser()
    {
        byte i = 0;
        if (!leftLaser) i = 1;

        lasers[i].SetActive(true);
        
        yield return new WaitForSeconds(0.1f);
        lasers[i].SetActive(false);

        leftLaser = !leftLaser;
    }
}
