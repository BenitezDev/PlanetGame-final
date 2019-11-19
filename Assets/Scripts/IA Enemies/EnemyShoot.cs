using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    private bool alive = true;

    [SerializeField]
    bool shooting = false;

    [SerializeField] private float firingRate = 0.2f;
    [SerializeField] private float damage = 1f;

    private float currentTimeFire = 0;

    private Transform playerTr;

    [Header("Rotacion:")]
    [SerializeField] private float cannonRotationSpeed = 1f;
    [SerializeField] private Transform cannonPivot;

    [SerializeField] private GameObject shootPS;


    private void Awake()
    {
        playerTr = GameObject.FindGameObjectWithTag("Player").transform;
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
                Debug.Log("PUUMPUUUUUUUUM");
                shootPS.SetActive(true);
                yield return new WaitForSeconds(0.2f);
                shootPS.SetActive(false);
                yield return new WaitForSeconds(0.1f);
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
            CircleRandomTargetBoid.instance.ChangeTargetPos();
        }
    }
}

