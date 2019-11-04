using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FuelManager : MonoBehaviour
{
    [SerializeField] private float MaxFuelCapacity = 500;
    [SerializeField] public static float currentFuel = 500;
    [SerializeField] private float consumoFuel = 0.1f;
    [SerializeField] public float maxFuelTime = 4f;
    [SerializeField] private float MaxTiempoATope = 3;
    private float currentATOPE = 0;

    [SerializeField] private Image FuelSlider;

    private bool atope = false; // true cuando tienes el motor al maximo 


    public float currentTime = 0;

    private void Awake()
    {
        currentFuel = MaxFuelCapacity;
    }


    private void Update()
    {
        // tocando la W
        if (Input.GetAxis("Right Stick Vertical 1") > 0 && currentFuel > 0)
        {
            currentFuel -= Time.deltaTime * consumoFuel;
            FuelSlider.fillAmount = currentFuel / 500;

            currentTime += Time.deltaTime*0.3f;
            
           

        }
        else if (currentTime > 0)
        {
            currentTime -= 0.25f*Time.deltaTime;
        }


        currentTime = Mathf.Clamp01(currentTime);
       
    }

}
