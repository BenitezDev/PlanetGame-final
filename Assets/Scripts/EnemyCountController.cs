using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnemyCountController : MonoBehaviour
{
    [SerializeField] private Text contadorEnemigos;
    [SerializeField] private int numeroMaximoDeEnemigos;


    private static GameObject panelFinal;
    private static int enemigosVivos;

    private void Awake()
    {
        enemigosVivos = numeroMaximoDeEnemigos;
        panelFinal = GameObject.Find("NextLevelCanvas");
        panelFinal.SetActive(false);
        MatarEnemigo();
    }

    public static void MatarEnemigo()
    {
        enemigosVivos--;
        ////////// TODO
        if (enemigosVivos <= 0)
        {
            panelFinal.SetActive(true);
            Time.timeScale = 0;
            Screen.lockCursor = false;

        }
    }





}
