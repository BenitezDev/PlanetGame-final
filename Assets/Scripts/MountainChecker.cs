using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MountainChecker : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.gameObject.CompareTag("Mountain"))
        {
            print("chocando con Montañas");
            PlayerHealth.UI.SetActive(false);
            PlayerHealth.alive = false;
            GetComponent<ShipExplote>().ExplotarNave();
            Screen.lockCursor = false;
            PlayerHealth.panelDemuerte.SetActive(true);
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }


    
}
