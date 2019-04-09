using System;
using System.Collections;
using System.Collections.Generic;
using Devdog.SciFiDesign.UI;
using UnityEngine.UI;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Image leftImage;
    [SerializeField] private ImageFillInterpolator leftInterpolator;

    [SerializeField] private Image rightImage;
    [SerializeField] private ImageFillInterpolator rightInterpolator;

    [SerializeField] private FuelManager fuelManager;

    public static float percentageAltitude, percentajeFuel;

    private Color normalcolorR, normalcolorL;
    [SerializeField] private Color red;

    private bool atope = false;
    [SerializeField] private float MaxTiempoATope = 3;
    [SerializeField] private Image HealthBar;

    [SerializeField] private ShipExplote shipExplote;

    private bool naveExplota = false;
    private void Awake()
    {
        fuelManager = GetComponent<FuelManager>();
        this.normalcolorL = this.leftImage.color;
        this.normalcolorR = this.rightImage.color;
        shipExplote = GetComponent<ShipExplote>();
    }


    private void Update()
    {
        // Altitud
        var wat = (HeightController.alturaPlayer - HeightController.minRadio) /
                  (HeightController.maxRadio - HeightController.minRadio);

        var foo = wat * 0.477f + 0.373f;
        //leftImage.color = Color.Lerp(this.normalcolorL,this.red, )

        if (percentageAltitude >= 0.8115f || percentageAltitude <= 0.4115f)
        {
            //this.leftImage.color = Color.red;
            this.leftImage.color = Color.Lerp(this.leftImage.color, this.red, Time.deltaTime);
        }
        
        else
        {
            //this.leftImage.color = this.normalcolorL;
            this.leftImage.color = Color.Lerp(this.leftImage.color, this.normalcolorL, Time.deltaTime);
        }
        
        percentageAltitude = Mathf.Clamp(foo, leftInterpolator.minFrom, leftInterpolator.maxTo);
        leftImage.fillAmount = percentageAltitude;
        
        
        
        
        
        
        // Fuel
        this.rightImage.color = Color.Lerp(this.normalcolorR, red, percentajeFuel);
            
        percentajeFuel = fuelManager.currentTime * 0.464f + 0.1f;
        if (percentajeFuel <= 0.1f)
        {
            this.rightImage.enabled = false;
        }
        else
        {
            this.rightImage.enabled = true;
        }
        rightImage.fillAmount = percentajeFuel;


        // HP
        // PlayerHealth.health va de 100 a 0
        // fillamount va de 0 a 1
        HealthBar.fillAmount = PlayerHealth.health*0.01f;



        if(naveExplota) return;

        if (rightImage.fillAmount >= 0.564f)
        {
            print(("LLEGO!"));
            atope = true;
            Invoke("CheckATope", MaxTiempoATope);
        }
        else if (rightImage.fillAmount <= 0.55f)
        {
            atope = false;
        }


    }

    private void CheckATope()
    {
        print(("TIEMPOATOPPERIOSFGD"));
        if (atope)
        {
            shipExplote.ExplotarNave();
            PlayerHealth.UI.SetActive(false);
            PlayerHealth.alive = false;
            GetComponent<ShipExplote>().ExplotarNave();
            Screen.lockCursor = false;
            PlayerHealth.panelDemuerte.SetActive(true);
            naveExplota = true;
        }
        

    }
}