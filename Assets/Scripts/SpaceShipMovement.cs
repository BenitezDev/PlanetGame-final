using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;

public class SpaceShipMovement : MonoBehaviour
{

    [SerializeField] float MinHeight = 10f;

    public float RadioPlaneta = 100;

    [SerializeField] private float speed = 10;
    [SerializeField] private float rotSpeed = 5;

    [SerializeField] Transform raycasTransform;

    [SerializeField] Transform planetTransform;
    Vector3 dir = new Vector3();

    public LayerMask PlanetLayerMask;

    private float angle = 0;
    private Vector3 sp;

    //---------------
    public float verticalInputAcceleration = 1;
    public float horizontalInputAcceleration = 20;

    public float maxSpeed = 10;
    public float maxRotationSpeed = 100;

    public float velocityDrag = 1;
    public float rotationDrag = 1;

    private Vector3 velocity;
    private float zRotationVelocity;
    //---------------



    private void Awake()
    {
        dir = Vector3.zero;
    }


    void Update()
    {



        // Gravity
        // transform.Translate((planetTransform.position - transform.position).normalized * Time.deltaTime);

        //if (Mathf.Abs(Input.GetAxis("Vertical")) > 0)
        //{
        //    dir += Vector3.up * Input.GetAxis("Vertical");

        //}

        //if (Mathf.Abs(Input.GetAxis("Horizontal")) > 0)
        //{
        //    //dir += Vector3.right * Input.GetAxis("Horizontal");
        //    transform.Rotate(Input.GetAxis("Horizontal") * Vector3.forward * rotSpeed);
        //}



        //if (Vector3.Distance(transform.position, planetTransform.position) < RadioPlaneta)
        //{
        //    // transform.RotateAround(planetTransform.position, transform.right, 10 * Time.deltaTime);
        //    transform.position = (transform.position - planetTransform.position).normalized * RadioPlaneta;
        //    print("Cuidao");
        //}

        //transform.Translate(dir.normalized * speed * Time.deltaTime);
        //transform.Rotate(dir.normalized);

        //transform.LookAt(planetTransform, transform.up);

        // apply forward input
        Vector3 acceleration = Input.GetAxis("Vertical") * verticalInputAcceleration * transform.up;
        velocity += acceleration * Time.deltaTime;

        // apply turn input
        float zTurnAcceleration = -1 * Input.GetAxis("Horizontal") * horizontalInputAcceleration;
        zRotationVelocity += zTurnAcceleration * Time.deltaTime;
    }
    private void FixedUpdate()
    {
        // apply velocity drag
        velocity = velocity * (1 - Time.deltaTime * velocityDrag);

        // clamp to maxSpeed
        velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

        // apply rotation drag
        zRotationVelocity = zRotationVelocity * (1 - Time.deltaTime * rotationDrag);

        // clamp to maxRotationSpeed
        zRotationVelocity = Mathf.Clamp(zRotationVelocity, -maxRotationSpeed, maxRotationSpeed);

        // update transform
        transform.position += velocity * Time.deltaTime;
        transform.Rotate(0, 0, zRotationVelocity * Time.deltaTime);
    }

    //    transform.position = new Vector3(transform.position.x,
    //                                    ((transform.position - planetTransform.position).normalized * transform.position.y).y*RadioPlaneta,
    //                                     transform.position.z);

    //    if (Input.GetKey(KeyCode.W))
    //        transform.Translate(transform.up * Time.deltaTime * 30);
    //    if (Input.GetKey(KeyCode.S))
    //        transform.Translate(-transform.up * Time.deltaTime * 30);

    //    else if (Vector3.Distance(planetTransform.position, transform.position) < RadioPlaneta )
    //    {
    //        print("cuidao");
    //    }

    //    transform.RotateAround(planetTransform.position, transform.right, 10*Time.deltaTime);
    //}

    //void OnDrawGizmos()
    //{
    //    Gizmos.DrawWireSphere(planetTransform.position, RadioPlaneta);
    //}

    //void Update()
    //{

    //    if(Input.GetKey(KeyCode.W)) transform.Translate(transform.forward*Time.deltaTime*30);

    //    // mal
    //    Debug.DrawRay(raycasTransform.position, -raycasTransform.up * 100 , Color.blue, Time.deltaTime);


    //    // bien
    //    Debug.DrawRay(raycasTransform.position, -raycasTransform.up*MinHeight,Color.magenta, Time.deltaTime);
    //    if (Physics.Raycast(raycasTransform.position, -raycasTransform.up, out var hit,Mathf.Infinity, PlanetLayerMask))
    //    {
    //        //var angleWithNormal = Vector3.Angle(hit.normal, transform.position);
    //        angle = Vector3.Angle(hit.normal, transform.position);
    //        print(hit.normal);
    //        transform.RotateAround(hit.point,transform.right, angle);
    //        //if(hit.normal != ne)



    //        // transform.Rotate(angleWithNormal, transform.rotation.x, transform.rotation.x);

    //        //print(angleWithNormal);


    //        sp = hit.normal * MinHeight;
    //        var dir = hit.point - raycasTransform.position;
    //        var sqrDist = Vector3.SqrMagnitude(dir);



    //        if (sqrDist < MinHeight * MinHeight)
    //        {

    //            transform.position = hit.normal * MinHeight;
    //            //transform.position = new Vector3(transform.position.x, transform.position.x, transform.position.x);


    //        }
    //  }

}
