using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerShip : MonoBehaviour
{
    private Rigidbody rb;
    public float speed = 100;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Mathf.Abs(Input.GetAxis("Vertical")) > 0)
        {
            rb.velocity = speed*transform.forward* Input.GetAxis("Vertical");
        }
    }
}
