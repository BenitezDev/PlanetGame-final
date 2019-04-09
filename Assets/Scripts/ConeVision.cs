using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConeVision : MonoBehaviour
{
    public StaticEnemie controller;
  






    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(("Player")))
        {
            controller.firePlayer = true;
         
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag(("Player")))
        {
            controller.firePlayer = false;
      
        }
    }
}
