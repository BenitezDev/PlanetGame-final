using System.Collections;
using System.Collections.Generic;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.UI;

public class WarningController : MonoBehaviour
{
    [SerializeField] private Animator animWarning;
    [SerializeField] private Text warningText;

    [SerializeField] private string textFuel;

    [SerializeField] private string textAltitude;

    private void Update()
    {
        if (PlayerUI.percentageAltitude >= 0.8f)
        {
            warningText.text = "";
            animWarning.SetBool("Warning", true);
        }
        else if (PlayerUI.percentageAltitude < 0.8f && PlayerUI.percentageAltitude >= 0.40f)
        {
            warningText.text = "";
            animWarning.SetBool("Warning", false);
        }
        else if(PlayerUI.percentageAltitude <0.40f)
        {
            warningText.text = "";
            animWarning.SetBool("Warning", true);
        }

        
    }

}
