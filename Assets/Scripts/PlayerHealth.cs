using System.Collections;
using System.Collections.Generic;
using Devdog.General;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
   public static float health = 100f;
   public static GameObject panelDemuerte;
   public static GameObject UI;

   [SerializeField] private GameObject PSexplosion;

   private static ShipExplote shipExplote;

   public static bool alive = true;

    private void Awake()
    {
        health = 100f;
        UI = GameObject.Find("UI");
        panelDemuerte = GameObject.Find("Has muerto");

        Time.timeScale = 1;
        UI.SetActive(true);
        panelDemuerte.SetActive(false);

        shipExplote = GetComponent<ShipExplote>();
    }



    public static void DecrementHealth(float dmg)
   {
       var aux = health - dmg;

       if (aux >= 0)
       {
           health = aux;
       }
       else
       {
           // has muerto:
           UI.SetActive(false);
           panelDemuerte.SetActive(true);
           alive = false;
           shipExplote.ExplotarNave();
           Screen.lockCursor = false;
           panelDemuerte.SetActive(true);
            //new PlayerHealth().CreateExplosion();
           
        }
    }

    //private void CreateExplosion()
    //{
    //    shipExplote.ExplotarNave();
    //    Screen.lockCursor = false;
    //    panelDemuerte.SetActive(true);
    //    Time.timeScale = 0;
    //}

    



   
}
