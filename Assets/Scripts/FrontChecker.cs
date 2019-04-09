using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontChecker : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.CompareTag("Planet"))
        {
            print("chocando con planeta");
            Debug.Break();
        }
    }

}
