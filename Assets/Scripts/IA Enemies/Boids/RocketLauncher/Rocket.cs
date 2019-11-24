using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : MonoBehaviour
{
    [SerializeField] AnimationCurve angularVelocityCurve;
    [SerializeField] AnimationCurve moveVelocityCurve;

    [SerializeField] float movementSpeed = 30f;
    


    [SerializeField] float startSpeed = 1f;
    [SerializeField] float startTime = 2f;

    [SerializeField] GameObject psTrail;
    

    public Transform playerTr;

    [SerializeField] GameObject explosionPs;

    Rigidbody rb;


    public int damage;
    public int health;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //StartCoroutine(TurnOnRocket());
    }

    public IEnumerator TurnOnRocket(float maxTime)
    {
        //while (currentTime <= startTime)
        //{
        //    //transform.Translate(transform.forward * startSpeed * Time.deltaTime);
        //    currentTime += Time.deltaTime;
        //    yield return null;
        //}

        //StartCoroutine(LookPlayer());
        Invoke("DestroyMissile", maxTime);
        yield return new WaitForSeconds(1f);
        psTrail.SetActive(true);
        if(transform != null || !transform.gameObject.activeInHierarchy)
        StartCoroutine(MoveToPlayer());
    }
   

    IEnumerator LookPlayer()
    {
        
        while (gameObject)
        {
            var rotation = Quaternion.RotateTowards(transform.rotation, transform.rotation,10000);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 50);
            yield return null;
        }
    }

    IEnumerator MoveToPlayer()
    {
        
        while (gameObject)
        {
            transform.LookAt(playerTr.position);
            Vector3 dir = (playerTr.position - transform.position).normalized;
            Vector3 deltaPosition = movementSpeed * dir * Time.deltaTime;
            rb.MovePosition(transform.position + deltaPosition);

            yield return null;
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("el misil te dio!");

            transform.gameObject.SetActive(false);
            var ps = Instantiate(explosionPs, transform.position, Quaternion.identity);
            Destroy(ps, 3f);
            Invoke("DestroyEnemy", 3f);
            // TODOOOO


            PlayerHealth.DecrementHealth(damage, Enemy.Rocket);
        }
        
    }

    private void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }

    private void DestroyMissile()
    {
        transform.gameObject.SetActive(false);
        var ps = Instantiate(explosionPs, transform.position, Quaternion.identity);
        Destroy(ps, 3f);
        Invoke("DestroyEnemy", 3f);
    }



    public void ReciveDamage(int dmg)
    {
        var aux = health - dmg;

        if (aux > 0)
        {
            health = aux;
        }
        else // Ha matado al misil
        {
            DestroyMissile();
        }
    }

}
