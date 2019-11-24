using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MachineGun : MonoBehaviour
{
    private Camera cam;

    [SerializeField] private float shootDistance = 500f;
    [SerializeField] private GameObject PShit;
    private float currentTime = 0;

    [SerializeField] private int damage = 10;
    public float fireRate = 0.1f;


    
    private bool leftShoot = true;

    public static Transform crosshair;

    public PlayerLaserManager playerLaserManager;

    private void Awake()
    {
        cam = Camera.main;
        crosshair = GameObject.FindGameObjectWithTag("crosshair").transform;
    }


    void Update()
    {

        currentTime += Time.deltaTime;

        if (!(currentTime >= fireRate)) return;


        if
        (
            Input.GetAxis("L2 1") > 0 || Input.GetAxis("L2 2") > 0 ||
            Input.GetButtonDown("L1 1") || Input.GetButtonDown("L2 1") ||
            Input.GetButton("Fire1")
        )
        {
            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            RaycastHit hit;

            //if (leftShoot) StartCoroutine(lasers[0].Shoot(fireRate));
            //else           StartCoroutine(lasers[1].Shoot());
            //leftShoot = !leftShoot;

            StartCoroutine(playerLaserManager.ShootLaser());
            currentTime = 0;

            if (Physics.Raycast(ray, out hit, shootDistance))
            {
                var ps = Instantiate(PShit, hit.point, Quaternion.identity);
                
                var enemieTarget = hit.transform.GetComponent<EnemyHealth>(); ///////////////////////////////////////////////////////////////////////////////
                if (enemieTarget != null)
                {
                    enemieTarget.ReciveDamage(damage);
                    return;
                }
                var rocket = hit.transform.GetComponent<Rocket>();
                if ( rocket != null)
                {
                    rocket.ReciveDamage(damage);
                }
            }

        }
    }
}
