using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject panelPausa, hud;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Screen.lockCursor = !Screen.lockCursor;
            panelPausa.SetActive(!panelPausa.activeSelf);
            hud.SetActive(!hud.activeSelf);
            

            Time.timeScale = Time.timeScale == 1 ? 0 : 1;
        }
    }

    public void BotonReanudar()
    {
        panelPausa.SetActive(false);
        hud.SetActive(true);
        Screen.lockCursor = true;


        Time.timeScale = 1;
    }

    public void Salir()
    {
        Screen.lockCursor = false;

        SceneManager.LoadScene("MainMenu");
    }
}
