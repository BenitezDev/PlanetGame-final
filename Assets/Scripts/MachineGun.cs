using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : MonoBehaviour
{
    private Camera cam;

    [SerializeField] private float shootDistance = 500f;
    [SerializeField] private GameObject PShit;
    private float currentTime = 0;
    [SerializeField] private float damage = 10;
    [SerializeField] private float fireRate = 0.1f;

    private void Start()
    {
        cam = Camera.main;
    }


    void Update()
    {
        currentTime += Time.deltaTime;
        if(Input.GetButton("Fire1"))
        {
            
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, shootDistance) && currentTime >= fireRate)
            {
                currentTime = 0;
                var ps = Instantiate(PShit, hit.point, Quaternion.identity);
                Destroy(ps, ps.GetComponent<ParticleSystem>().main.duration);
                var enemieTarget = hit.transform.GetComponent<StaticEnemie>();
                if (enemieTarget != null)
                {
                    enemieTarget.ReciveDamage(damage);
                }
            }
        }
    }
}
