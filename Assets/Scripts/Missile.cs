using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Missile : MonoBehaviour
{

    [SerializeField] private float speed = 3;
    [SerializeField] private float rotSpeed = 3;
    [SerializeField] private float explosionForce = 30;
    [SerializeField] private GameObject explosion;
 

    private Rigidbody rb;

    private bool _makeShake = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(PlayerHealth.alive)
        //transform.LookAt(HeightController.playerPos * Time.deltaTime);
        //transform.Translate(transform.forward * Time.deltaTime * speed);

        //rb.AddForce(transform.forward*speed,ForceMode.Acceleration);
        transform.Translate(Vector3.forward * Time.deltaTime * speed);

        var rotation = Quaternion.LookRotation(HeightController.playerPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotSpeed);

        //if(_makeShake) makeShake();

    }

    private void makeShake()
    {
        StartCoroutine(Camera.main.GetComponent<CameraFlightFollow>().Shake(0.6f));
        GetComponent<BoxCollider>().enabled  = false;
        GetComponent<MeshRenderer>().enabled = false;
        Destroy(this.gameObject,.6f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.gameObject.CompareTag("Player"))
        {
            //collision.transform.gameObject.GetComponent<Rigidbody>().
            //    AddForceAtPosition((collision.transform.position - transform.position).normalized * explosionForce, transform.position,
            //        ForceMode.Impulse);


            makeShake();
            Instantiate(explosion, collision.GetContact(0).point, Quaternion.identity);




            //Destroy(this.gameObject);
        }
    }

}
