using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BandidData 
{
    public bool init;
    public int totalActions;
    public int[] count;
    public float[] score;
    public float[] UCB1scores; 
    public int numActions;
    


    public BandidData(Bandit bandit)
    {
        this.init = bandit.init;
        this.totalActions = bandit.totalActions;
        this.count = bandit.count;
        this.score = bandit.score;
        this.UCB1scores = bandit.UCB1scores;
        this.numActions = bandit.numActions;
    }
}
