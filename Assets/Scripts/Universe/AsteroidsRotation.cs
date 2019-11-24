using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidsRotation : MonoBehaviour
{
    public float rotSpeed = 10f;
    

    public Vector3 axis;
    
    private void Update()
    {
        transform.Rotate(axis, rotSpeed * Time.deltaTime);
    }
}
