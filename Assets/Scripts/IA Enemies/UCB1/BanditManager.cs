using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BanditManager : MonoBehaviour
{
    public Text[] textos;
    

    private void Update()
    {
        //int globaldmg = 0;
        //foreach (var dmg in RoundManager.Instance.rounds)
        //{
        //    globaldmg += dmg.damages[0];
        //    globaldmg += dmg.damages[1];
        //    globaldmg += dmg.damages[2];
        //}
        //int currentdmg = 0;
        //foreach (var dmg in RoundManager.Instance.rounds[RoundManager.Instance.RoundIndex].damages)
        //{
          
        //    currentdmg += dmg;
        //}

        textos[0].text = "SRI: " + Bandit.Instance.UCB1scores[0];
        textos[1].text = "RRS: " + Bandit.Instance.UCB1scores[1];
        textos[2].text = "IIS: " + Bandit.Instance.UCB1scores[2];
        textos[3].text = "SSR: " + Bandit.Instance.UCB1scores[3];
        textos[4].text = "IIR: " + Bandit.Instance.UCB1scores[4];
        textos[5].text = "SSI: " + Bandit.Instance.UCB1scores[5];
        textos[6].text = "RRI: " + Bandit.Instance.UCB1scores[6];
        //textos[7].text = "Global damage:  " + globaldmg;
        //textos[8].text = "Current damage: " + currentdmg;

        
    }
}
