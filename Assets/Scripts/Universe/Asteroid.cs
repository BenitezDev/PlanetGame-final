using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public float my_radius;
    public float speed;
    public Transform planet;


    private void Start()
    {
        speed = Random.Range(10, 50);
    }


    private void Update()
    {
        transform.RotateAround(planet.position, planet.up, speed * Time.deltaTime);
    }
}
