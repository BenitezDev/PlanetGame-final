using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int currentHealth;
    [SerializeField] private int MaxHealth;

    [SerializeField] private GameObject explosionPS;

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
            transform.gameObject.SetActive(false);
            Instantiate(explosionPS, transform.position, Quaternion.identity);
            Invoke("DestroyEnemy", 3f);
            // TODOOOOOOOO
        }
    }

    private void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }

}
