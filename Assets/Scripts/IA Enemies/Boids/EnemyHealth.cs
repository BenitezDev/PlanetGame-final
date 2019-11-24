using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] public int currentHealth;
    [SerializeField] private int MaxHealth;

    public GameObject explosionPS;

    private void Awake()
    {
        currentHealth = MaxHealth;
    }

    public void ReciveDamage(int dmg)
    {
        var aux = currentHealth - dmg;
        
        if (aux > 0)
        {
            currentHealth = aux;
        }
        else // Ha muerto el enemigo
        {
            ThisBoidIsDead();
            // TODOOOOOOOO
        }
    }

    public void ThisBoidIsDead()
    {
        RoundManager.Instance.DecreaseActiveEnemies();
        transform.gameObject.SetActive(false);
        var ps = Instantiate(explosionPS, transform.position, Quaternion.identity);
        Destroy(ps, 3f);
        DestroyEnemy();
        //Invoke("DestroyEnemy", 3f);
    }

    private void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }

}
