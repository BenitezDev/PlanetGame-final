using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShootRocket : MonoBehaviour
{
    

    [SerializeField] bool shoot_in_CD = false;
    [SerializeField] private float shoot_cd = 3f;
    private Transform playerTr;
    [Header("Bullet info")]
    [SerializeField] private int damage = 10;
    [SerializeField] private int maxRocketTimeLife = 7;

    int currentLife = 1;
    [SerializeField] private int maxLife = 1;

    

    [Header("Rotacion:")]
    [SerializeField] private float cannonRotationSpeed = 1f;
    [SerializeField] private Transform cannonPivot;

    [SerializeField] private Transform muzzle;

    public Renderer render;

    [SerializeField] private GameObject Rocket;


    private void Awake()
    {
        currentLife = maxLife;
        playerTr = GameObject.FindGameObjectWithTag("Player").transform;
        StartCoroutine(CannonFollowPlayer(playerTr));
        render = GetComponentInChildren<Renderer>();
    }

    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            ShootRocket();
        }
    }

    

    public void ShootRocket()
    {
        if (shoot_in_CD) return;

        var rocket = Instantiate(Rocket, cannonPivot.transform.position, Quaternion.LookRotation(playerTr.position - cannonPivot.position));
        var component = rocket.GetComponent<Rocket>();
        component.playerTr = playerTr;
        component.damage = damage;
        component.health = maxLife;
        StartCoroutine(component.TurnOnRocket(maxRocketTimeLife));
        StartCoroutine(StartCDrocket());
    }

    IEnumerator StartCDrocket()
    {
        shoot_in_CD = true;
        yield return new WaitForSeconds(shoot_cd);
        shoot_in_CD = false;
    }

    IEnumerator CannonFollowPlayer(Transform player)
    {
        while (gameObject)
        {

            Quaternion rotation = Quaternion.LookRotation(playerTr.position - cannonPivot.position);

            cannonPivot.rotation = Quaternion.Slerp(cannonPivot.rotation, rotation, Time.deltaTime * cannonRotationSpeed);

            yield return null;
        }
    }

}
