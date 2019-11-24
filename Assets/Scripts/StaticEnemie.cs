using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StaticEnemie : MonoBehaviour
{
    [SerializeField] private float Health = 100f;
    [SerializeField] private Transform rockeTransform1;
    [SerializeField] private Transform rockeTransform2;
    [SerializeField] private GameObject explosion;
    private bool rocket1 = true;

    [SerializeField] private GameObject PSFire;
    [SerializeField] private GameObject Rocket;

    [SerializeField] private Transform PSLocation;


    private bool seePlayer = false;
    public bool firePlayer = false;
    [SerializeField] private float firingRate = 0.2f;
    [SerializeField] private float firingRateRocket = 5f;
    [SerializeField] private float damage = 1f;

    private float currentTimeFire = 0, currentTimeRocket = 0;



    [Header("Rotacion:")]
    [SerializeField] private Transform cannon;

    [SerializeField] private Transform meshTransform;
    [SerializeField] private Transform ejez;




    

    private void Start()
    {
       
        var dir = GameObject.Find("Planet").transform.position - transform.position;
        //transform.position = dir.normalized * 250;
        // (HeightController.minRadio + Random.Range(0,150)
        // transform.LookAt(Vector3.zero);

        transform.LookAt(GameObject.Find("Planet").transform.position, Vector3.up);
    }

    private void Update()
    {
       // transform.LookAt(Vector3.zero);
        if (seePlayer)
        {
            //meshTransform.LookAt(HeightController.playerPos);

            //mesh.LookAt(new Vector3(mesh.position.x, mesh.position.y, HeightController.playerPos.z));

            // cannon.LookAt(HeightController.playerPos);

           
            currentTimeRocket += Time.deltaTime;

            

            if (currentTimeRocket >= firingRateRocket)
            {
                ShootRocket();
                currentTimeRocket = 0;
            }
        }

        if (firePlayer)
        {
            meshTransform.LookAt(new Vector3( HeightController.playerPos.x, meshTransform.position.y, HeightController.playerPos.z));
            //meshTransform.LookAt(HeightController.playerPos);
            //meshTransform.localRotation = Quaternion.Euler(new Vector3(meshTransform.position.x, -90, 90));
            currentTimeFire += Time.deltaTime;

            if (currentTimeFire >= firingRate)
            {
                Fire();
                currentTimeFire = 0;
            }
            cannon.LookAt(HeightController.playerPos);
        }
        else
        {
            //meshTransform.Rotate(meshTransform.up* Time.deltaTime*10);
            meshTransform.Rotate(0,1,0);
            //meshTransform.localRotation = Quaternion.Euler(meshTransform.rotation.x +10,
            //    meshTransform.rotation.y, meshTransform.rotation.z);
        }
    }


    private void Fire()
    {
        if (Random.Range(0, 2) == 0)
        {
            // sistema de particulas de disparo
            //PlayerHealth.DecrementHealth(damage);
            print(("pum " + PlayerHealth.health));
        }

        Instantiate(this.PSFire, this.PSLocation.position, Quaternion.identity);
    }

    private void ShootRocket()
    {
        if (rocket1)
        {
            Instantiate(Rocket, rockeTransform1.position, Quaternion.identity);
        }
        else
        {
            Instantiate(Rocket, rockeTransform2.position, Quaternion.identity);
        }
        rocket1 = !rocket1;
    }



    private void OnTriggerExit(Collider other)
    {
        if (other.transform.gameObject.CompareTag("Player"))
        {
            seePlayer = false;

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.CompareTag("Player"))
        {
            seePlayer = true;
        }
    }

    public void ReciveDamage(float dmg)
    {

        var aux = Health - dmg;
        print(aux);
        if (aux > 0)
        {
            Health = aux;
        }
        else
        {
            Instantiate(explosion, transform.position + Vector3.up*2, Quaternion.identity);
            print("muerte matao");
            EnemyCountController.MatarEnemigo();
            transform.gameObject.SetActive(false);
        }
    }

}
