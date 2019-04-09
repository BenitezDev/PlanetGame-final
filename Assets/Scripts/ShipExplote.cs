using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class ShipExplote : MonoBehaviour
{

    [SerializeField] private GameObject explosionPS;



    public void ExplotarNave()
    {
        transform.gameObject.SetActive(false);
        Instantiate(explosionPS, HeightController.playerPos, Quaternion.identity);
    }
    
}
