using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : MonoBehaviour
{
    public bool alive = true;
    [SerializeField] int baseDamageCollision = 10;



    BoidSettings settings;

    // State
    [HideInInspector]
    public Vector3 position;
    [HideInInspector]
    public Vector3 forward;
    Vector3 velocity;

    // To update:
    Vector3 acceleration;
    [HideInInspector]
    public Vector3 avgFlockHeading;
    [HideInInspector]
    public Vector3 avgAvoidanceHeading;
    [HideInInspector]
    public Vector3 centreOfFlockmates;
    [HideInInspector]
    public int numPerceivedFlockmates;

    // Cached
    Material material;
    //Transform cachedTransform;
    Transform target;

    [SerializeField] private GameObject explosionPS;

    EnemyHealth enemyHealth;

    void Awake()
    {
        material = transform.GetComponentInChildren<MeshRenderer>().material;
        //cachedTransform = transform;
        explosionPS = GetComponent<EnemyHealth>().explosionPS;
        enemyHealth = GetComponent<EnemyHealth>();
    }

    public void Initialize(BoidSettings settings, Transform target)
    {
        this.target = target;
        this.settings = settings;

        position = transform.position;
        forward = transform.forward;

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;
    }

    public void SetColour(Color col)
    {
        if (material != null)
        {
            material.color = col;
        }
    }

    public void UpdateBoid()
    {
        if (transform == null) return;
        if (enemyHealth.currentHealth <= 0) return;


        Vector3 acceleration = Vector3.zero;

        if (target != null)
        {
            Vector3 offsetToTarget = (target.position - position);
            acceleration = SteerTowards(offsetToTarget) * settings.targetWeight;
        }

        if (numPerceivedFlockmates != 0)
        {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

            var alignmentForce = SteerTowards(avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards(offsetToFlockmatesCentre) * settings.cohesionWeight;
            var seperationForce = SteerTowards(avgAvoidanceHeading) * settings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }

        if (IsHeadingForCollision())
        {
            Vector3 collisionAvoidDir = ObstacleRays();
            Vector3 collisionAvoidForce = SteerTowards(collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp(speed, settings.minSpeed, settings.maxSpeed);
        velocity = dir * speed;


        transform.position += velocity * Time.deltaTime;
        transform.forward = dir;
        position = transform.position;
        forward = dir;
    }

    bool IsHeadingForCollision()
    {
        //RaycastHit hit;
        //if (position != null && settings != null && transform != null) return false;
        //if (Physics.SphereCast(position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask) && gameObject)
        //{
        //    return true;
        //}
        //else
        //{
        //}
        return false;
    }

    Vector3 ObstacleRays()
    {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 dir = transform.TransformDirection(rayDirections[i]);
            Ray ray = new Ray(position, dir);
            if (!Physics.SphereCast(ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask))
            {
                return dir;
            }
        }

        return forward;
    }

    Vector3 SteerTowards(Vector3 vector)
    {
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude(v, settings.maxSteerForce);
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth.DecrementHealth(baseDamageCollision, Enemy.Shooter);
            ThisBoidIsDead();
            // TODOOOOOOOO
        }
        else if (collision.gameObject.CompareTag("Mountain"))
        {

            ThisBoidIsDead();
            // TODOOOO
        }

    }

    public void ThisBoidIsDead()
    {
        alive = false;
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
