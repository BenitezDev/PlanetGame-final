using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
   
    public  static int CurrentEnemies = 3;

    public static void KillEnemie()
    {
        CurrentEnemies--;
        if (CurrentEnemies <= 0)
        {
            // FINALD EL JUEGO
        }
    }
}
