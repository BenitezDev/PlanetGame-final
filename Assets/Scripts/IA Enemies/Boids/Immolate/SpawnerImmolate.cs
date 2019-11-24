using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerImmolate : MonoBehaviour {

    public enum GizmoType { Never, SelectedOnly, Always }

    public BoidImmolate prefab;
    public float spawnRadius = 10;
    public int spawnCount = 10;
    public Color colour;
    public GizmoType showSpawnRegion;

    public List<BoidImmolate> boids;


    void Awake () {
        
    }

    public void SpawnEnemies(int enemyCount)
    {
        if (enemyCount <= 0) return;
        boids.Clear();
        for (int i = 0; i < enemyCount; i++)
        {
            Vector3 pos = transform.position + Random.insideUnitSphere * spawnRadius;
            BoidImmolate boid = Instantiate(prefab);
            boid.transform.position = pos;
            boid.transform.forward = Random.insideUnitSphere;

            boid.SetColour(colour);

            boids.Add(boid);
        }
        if (enemyCount > 0)
            GetComponent<BoidManagerImmolate>().CreateBoids(boids);
    }

    private void OnDrawGizmos () {
        if (showSpawnRegion == GizmoType.Always) {
            DrawGizmos ();
        }
    }

    void OnDrawGizmosSelected () {
        if (showSpawnRegion == GizmoType.SelectedOnly) {
            DrawGizmos ();
        }
    }

    void DrawGizmos () {

        Gizmos.color = new Color (colour.r, colour.g, colour.b, 0.3f);
        Gizmos.DrawSphere (transform.position, spawnRadius);
    }

}