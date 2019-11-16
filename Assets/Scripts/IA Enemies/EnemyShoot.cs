using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShoot : MonoBehaviour
{
    private bool alive = true;

    [SerializeField] private float firingRate = 0.2f;
    [SerializeField] private float damage = 1f;

    private float currentTimeFire = 0;

    private Transform playerTr;

    [Header("Rotacion:")]
    [SerializeField] private float cannonRotationSpeed = 1f;
    [SerializeField] private Transform cannonPivot;


    private void Awake()
    {
        playerTr = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(CannonFollowPlayer(playerTr));
    }

    private void Update()
    {
        
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
}
