using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum Action
{
    SRI,
    RRS,
    IIS,
    SSR,
    IIR,
    SSI,
    RRI
}

public enum Enemy
{
    Immolate,
    Rocket,
    Shooter
}

public class Bandit : Singleton<Bandit>
{

    public bool init = false;
    public int totalActions;
    public int[] count;
    public float[] score;// Para cada jugada, si el jugador ha ganado (-1) o ha perdido (-1). Cantidad de daño que ha hecho
    public float[] UCB1scores; // Cálculo intermedio de cada acción de función a maximizar
    public int numActions;
    public Action lastAction;

    public bool LoadedFromfile = false;

    private void Awake()
    {
        InitUCB1();
    }

    public void InitUCB1()
    {

        //if (init)
        //{
        //    LoadBandid();
        //    return;
        //}

        //LoadBandid();

        if(LoadBandid()) return;
        



        totalActions = 0;
        numActions = System.Enum.GetNames(typeof(Action)).Length;
        count = new int[numActions];
        score = new float[numActions];
        UCB1scores = new float[numActions];
        int i;
        for (i = 0; i < numActions; i++)
        {
            count[i] = 0;
            score[i] = 0f;
        }
        init = true;
    }

    public Action GetNextActionUCB1()
    {
        int i, best;
        float bestScore;
        float tempScore;
        // Las primeras numActions veces solo va probando cada una de las acciones.
        // Sería mejor hacer un Random de todas las acciones que aún no ha probado.
        for (i = 0; i < numActions; i++)
        {
            if (count[i] == 0)
            {
                lastAction = (Action)i;
                return lastAction;
            }
        }

        // Si ya ha probado todas las acciones entonces aplica UCB1.
        best = -1;
        bestScore = int.MinValue;
        for (i = 0; i < numActions; i++)
        {
            tempScore = UCB1(score[i] / count[i], count[i], totalActions);
            UCB1scores[i] = tempScore;
            if (tempScore > bestScore)
            {
                best = i;
                bestScore = tempScore;
            }
        }
        lastAction = (Action)best;
        return lastAction;
    }

    private float UCB1(float averageUtility, float count, float totalActions)
    {
        return averageUtility + Mathf.Sqrt(2 + Mathf.Log10(totalActions) / count);
    }


    public int GetUtility(int round)
    {
        // buscar en un array :
        /*
         jugada:    daño hecho/ por quien
         0              50 / rocket
         */
        int i = 0, totalDmg = 0;
        while( i < 3)
        {
            totalDmg += RoundManager.Instance.rounds[round].damages[i];
            i++;
        }
        Debug.Log("TOTAL DMG:" + totalDmg);
        return totalDmg;
    }

    public void EndRound(Action action)
    {
        int utility;
        utility = GetUtility(RoundManager.Instance.RoundIndex);
        score[(int)lastAction] += utility;
        count[(int)lastAction]++;

        totalActions++;
    }

    public Bandit GetBandit()
    {
        return this;
    }
    public void SaveBandid()
    {
        SaveSystem.SaveBandit(this);
    }

    public bool LoadBandid()
    {
        BandidData data =  SaveSystem.LoadBandid();
        if (data != null)
        {

            init = data.init;
            totalActions = data.totalActions;
            count = data.count;
            score = data.score;
            UCB1scores = data.UCB1scores;
            numActions = data.numActions;

            LoadedFromfile = true;
            return true;
        }
        LoadedFromfile = false;
        return false;
    }
}
