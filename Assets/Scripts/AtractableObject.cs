using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AtractableObject : MonoBehaviour
{
    private Rigidbody rb;

    private Vector3 gravityDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        gravityDirection = Vector3.zero;
    }

    private void Update()
    {
        gravityDirection = (GameManager.instance.PlaneTransform.position - transform.position).normalized;
        gravityDirection *= GameManager.Gravity;
        rb.AddForce(gravityDirection,ForceMode.Acceleration);
    }
   
  
}
