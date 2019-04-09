using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void CargarJuego()
    {
        SceneManager.LoadScene("Game");
    }

    public void CargarNivel2()
    {
        SceneManager.LoadScene("Game2");
    }
}
