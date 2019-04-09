using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private Transform forewardShip;
    [SerializeField] private Vector3 mouse;

    private void Update()
    {

        mouse = Input.mousePosition;
        
    }



    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(mouse, 0.5f);
    }



}
