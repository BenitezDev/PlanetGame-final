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
        panelFinal = GameObject.Find("Nivel Superado");
        panelFinal.SetActive(false);


        contadorEnemigos.text = enemigosVivos.ToString();
    }

    private void Update()
    {
        contadorEnemigos.text = enemigosVivos.ToString();
    }


    public static void MatarEnemigo()
    {
        enemigosVivos--;
        
        if (enemigosVivos <= 0)
        {
            new EnemyCountController().EndGame();

        }
    }

    private void UpdateScore()
    {
        contadorEnemigos.text = enemigosVivos.ToString();
    }


    private void EndGame()
    {
        Invoke("auxStop",2);
    }

    private void auxStop()
    {
        panelFinal.SetActive(true);
        Time.timeScale = 0;
        Screen.lockCursor = false;
    }

}
