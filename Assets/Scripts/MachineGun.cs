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
        if (Input.GetButtonDown("L1 1")) Debug.Log("JAJAJAJAJ");


        currentTime += Time.deltaTime;

        if(Input.GetAxis("L2 1") > 0 || Input.GetAxis("L2 2") > 0 || Input.GetButtonDown("L1 1") || Input.GetButtonDown("L2 1") || Input.GetKeyDown(KeyCode.A))
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
