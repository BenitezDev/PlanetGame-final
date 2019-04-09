using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CanvasManager : MonoBehaviour
{
    public void GoToGame1()
    {
        SceneManager.LoadScene("Game1");
    }
    public void GoToGame2()
    {
        SceneManager.LoadScene("Game2");
    }
    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }


    
}
