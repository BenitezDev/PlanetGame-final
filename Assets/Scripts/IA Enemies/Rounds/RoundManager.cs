using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct Round
{
    public Action enemyTypeRound;
    public int[] damages;

    public Round(Action t)
    {
        enemyTypeRound = t;
        damages = new int[3];
    }
}


public class RoundManager : Singleton<RoundManager>
{
    public List<Round> rounds;
    private int roundIndex = 0;
    public int numEnemiesAlive;
    public Spawner shooterSpawner;
    public SpawnerRocketLauncher rocketSpawner;
    public SpawnerImmolate immolateSpawner;

    private Text text;

    public int RoundIndex { get => this.roundIndex; }

    private void Awake()
    {
        base.Awake();
        text = GetComponentInChildren<Text>();
    }

    private void Start()
    {
        text.gameObject.SetActive(false);
        StartCoroutine(ChangeRound());
        
    }

 
    private void StartRound()
    {
        var action = Bandit.Instance.GetNextActionUCB1();
        Debug.Log(action);

        switch (action)
        {
            case Action.SRI:
                SpawnEnemies(1, 1, 1);
                break;
            case Action.RRS:
                SpawnEnemies(1, 2, 0);
                break;
            case Action.IIS:
                SpawnEnemies(1, 0, 2);
                break;
            case Action.SSR:
                SpawnEnemies(2, 1, 0);
                break;
            case Action.IIR:
                SpawnEnemies(0, 1, 2);
                break;
            case Action.SSI:
                SpawnEnemies(2, 0, 1);
                break;
            case Action.RRI:
                SpawnEnemies(0, 2, 1);
                break;
        }
        var newRound = new Round(action);
        rounds.Add(newRound);
    }

    private void SpawnEnemies(int shooters, int rockets, int immolaters)
    {
        if(shooters > 0)
            shooterSpawner. SpawnEnemies(shooters);
        if(rockets > 0)
            rocketSpawner.  SpawnEnemies(rockets);
        if( immolaters > 0)
            immolateSpawner.SpawnEnemies(immolaters);

        numEnemiesAlive = shooters + rockets + immolaters;
    }

    public void DecreaseActiveEnemies()
    {
        numEnemiesAlive--;

        if (numEnemiesAlive <= 0)
        {
            Debug.Log("Ha termindo la ronda");
            Bandit.Instance.EndRound(rounds[roundIndex].enemyTypeRound);
            roundIndex++;
            StartCoroutine(ChangeRound());
        }

    }

    private IEnumerator ChangeRound()
    {
        Debug.Log("----------------" + roundIndex);
       
        yield return new WaitForSeconds(3f);
        text.gameObject.SetActive(true);
        if(roundIndex != 0)
        {
            text.text = "RONDA COMPLETADA!";
        }
        yield return new WaitForSeconds(1.5f);
        if (roundIndex != 0)
        {
            text.text = "REGENERANDO VIDA";
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(PlayerHealth.RegenerateLife());
            yield return new WaitForSeconds(2f);
        }
        
        text.text = "RONDA " + (int)(RoundManager.Instance.roundIndex + 1);
        yield return new WaitForSeconds(1.5f);
        text.text = "GO!";
        yield return new WaitForSeconds(1.5f);
        text.gameObject.SetActive(false);
        StartRound();
    }
}
