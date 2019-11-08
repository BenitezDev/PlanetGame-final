using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetRings : MonoBehaviour
{

    public Planet planet;

    public Mesh cilinder;

    //public float radius;

    //public float distance_ring = 500;
    //public float ring_width = 100;


    public int max_num_asteroids;
    public GameObject asteroid;
    private GameObject[] asteroids;



    public float min_radius_asteroid;
    public float max_radius_asteroid;



    private void Awake()
    {
        planet = GetComponent<Planet>();
        //radius = planet.shapeSettings.planetRadius;

        asteroids = new GameObject[max_num_asteroids];

        for(int i = 0; i < max_num_asteroids; ++i)
        {
            asteroids[i] = Instantiate(asteroid);

            float dist_x = Random.Range(min_radius_asteroid, max_radius_asteroid);

            float dist_z = Random.Range(min_radius_asteroid, max_radius_asteroid);

            asteroids[i].GetComponent<Asteroid>().planet = transform;

            asteroids[i].transform.position =
                new Vector3(
                    (planet.transform.right * dist_x).x,
                    planet.transform.position.y,
                    (planet.transform.forward * dist_z).z
                    );
        }

        Debug.Break();
    }


    

    private void OnDrawGizmos()
    {
        //Gizmos.color = Color.yellow;
        //Gizmos.DrawWireMesh(cilinder,transform.position, Quaternion.identity, new Vector3(1,0.15f, 1) * radius + new Vector3(distance_ring, 0, distance_ring));

        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireMesh(cilinder, transform.position, Quaternion.identity, new Vector3(1, 0.15f, 1) * radius + new Vector3(distance_ring - ring_width, 0, distance_ring - ring_width));

        
        Debug.DrawLine(transform.position, Vector3.forward * max_radius_asteroid, Color.yellow);

        
        Debug.DrawLine(transform.position, Vector3.forward * min_radius_asteroid, Color.blue);

    }

}




