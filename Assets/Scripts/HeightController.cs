using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.PlayerLoop;

public class HeightController : MonoBehaviour
{
    [SerializeField] private Transform planetTransform;
   
    [SerializeField] private  float minRadioPlaneta;
    [SerializeField] private  float maxRadioPlaneta;

    [SerializeField] private LayerMask planetLayerMask;

    [SerializeField] private float MaxTimeOutside = 5f;


    public static float minRadio;
    public static float maxRadio;

    public static float alturaPlayer;

    public static Vector3 playerPos;

    //--------------------------------------
    private float currentTimeOutside = 0f;

    [SerializeField] private PlayerHealth playerHealth;

    private void Awake()
    {
        minRadio = minRadioPlaneta;
        maxRadio = maxRadioPlaneta;
        print("Min:"+minRadio+" Max:"+maxRadio);
    }





    void Update()
    {
        playerPos = transform.position;
        alturaPlayer = Vector3.Distance(transform.position, planetTransform.position);
        if (alturaPlayer < minRadioPlaneta)
        {
            transform.position = (transform.position - planetTransform.position).normalized * minRadioPlaneta;
        }
        else if (Vector3.Distance(transform.position, planetTransform.position) > maxRadioPlaneta)
        {
            print("Fuera de orbita");

            currentTimeOutside += Time.deltaTime;
            if (currentTimeOutside >= MaxTimeOutside)
            {
                
                PlayerHealth.UI.SetActive(false);
                PlayerHealth.alive = false;
                GetComponent<ShipExplote>().ExplotarNave();
                Screen.lockCursor = false;
                PlayerHealth.panelDemuerte.SetActive(true);
                
            }
        }
        else
        {
            currentTimeOutside = 0;
        }
            
    }

  


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(planetTransform.position, minRadioPlaneta);
        Gizmos.DrawWireSphere(planetTransform.position, maxRadioPlaneta);
        Handles.Label(transform.position, currentTimeOutside.ToString());
    }


}
