using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLaser : MonoBehaviour
{
   
    public GameObject LaserPrefabs;

    private GameObject Instance;
    private EGA_Laser LaserScript;

    

    
    private void endShoot()
    {
        LaserScript.DisablePrepare();
        Destroy(Instance, 1);
        
    }

    private void beginShoot()
    {
        Destroy(Instance);
        Instance = Instantiate(LaserPrefabs, transform.position, transform.rotation);
        LaserScript = Instance.GetComponent<EGA_Laser>();
        LaserScript.ShootLaser();
    }

    private void beginShoot(Vector3 hitPos)
    {
        Destroy(Instance);
        Instance = Instantiate(LaserPrefabs, hitPos, Quaternion.identity);
        LaserScript = Instance.GetComponent<EGA_Laser>();
        LaserScript.ShootLaser(hitPos);
    }


    public IEnumerator Shoot(float fireRate)
    {
    
        //beginShoot();
        

        yield return new WaitForSeconds(fireRate);

        endShoot();
    }
}
