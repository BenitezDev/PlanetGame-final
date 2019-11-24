using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    private bool alive = true;

    [SerializeField]
    bool shooting = false;

    [SerializeField] private float firingRate = 0.2f;
    [Range(0,1)]
    [SerializeField] private float precisionPercentage = 0.5f;
    [SerializeField] private int damage = 1;

    private float currentTimeFire = 0;

    private Transform playerTr;

    [Header("Rotacion:")]
    [SerializeField] private float cannonRotationSpeed = 1f;
    [SerializeField] private Transform cannonPivot;

    [SerializeField] private GameObject shootPS;
    CircleRandomTargetBoid circleRandomTarget;


    private void Awake()
    {
        playerTr = GameObject.FindGameObjectWithTag("Player").transform;
        circleRandomTarget = GameObject.FindGameObjectWithTag("ShootingArea").GetComponent<CircleRandomTargetBoid>();
        StartCoroutine(CannonFollowPlayer(playerTr));
        StartCoroutine(Fire());
    }



    IEnumerator CannonFollowPlayer(Transform player)
    {
        while(alive)
        {

            Quaternion rotation = Quaternion.LookRotation(playerTr.position - cannonPivot.position);

            cannonPivot.rotation = Quaternion.Slerp(cannonPivot.rotation, rotation, Time.deltaTime * cannonRotationSpeed);
            
            yield return null;
        }
    }

    IEnumerator Fire()
    {
        while (alive)
        { 
            while (shooting)
            {
                shootPS.SetActive(true);
                yield return new WaitForSeconds(0.2f);
                if (Random.Range(0.0f, 1.0f) < precisionPercentage)
                {
                    PlayerHealth.DecrementHealth(damage, Enemy.Shooter);
                }
                    
                shootPS.SetActive(false);
                
            }
            yield return null;

        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("ShootingArea"))
        {
            shooting = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("ShootingArea"))
        {
            shooting = false;
            shootPS.SetActive(false);
            circleRandomTarget.ChangeTargetPos();
            //CircleRandomTargetBoid.instance.ChangeTargetPos();
        }
    }
}

