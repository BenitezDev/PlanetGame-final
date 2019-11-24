using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidImmolate : MonoBehaviour
{
    Enemy enemyType = Enemy.Immolate;

    BoidSettingsImmolate settings;

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
    Transform cachedTransform;


    [SerializeField]Transform target; // Circle Boid Position

    public Transform player;
    public Transform CircleBoid;

    Renderer renderer;
    [SerializeField] float plusPriority = 0;

    public bool reachTarget = false;
    [SerializeField] private GameObject explosionPS;


    [SerializeField] bool AllahuAkbar = false; // if true... the inmolation is inminent. RUN!

    [SerializeField] int inmolationDamge = 20;

    void Awake()
    {
        renderer = GetComponentInChildren<Renderer>();
        material = transform.GetComponentInChildren<MeshRenderer>().material;
        cachedTransform = transform;

        player = GameObject.FindGameObjectWithTag("Player").GetComponentInChildren<MeshRenderer>().transform;
        explosionPS = GetComponent<EnemyHealth>().explosionPS;       
    }

    private void Start()
    {
        StartCoroutine(CheckDistanceTarget());
        StartCoroutine(AddPlusPriority());
    }



    public void Initialize(BoidSettingsImmolate settings, Transform target)
    {
        this.target = target;
        this.settings = settings;

        CircleBoid = target;

        this.settings.maxSteerForce = 3f;
        this.settings.targetWeight = 10f;

        position = cachedTransform.position;
        forward = cachedTransform.forward;

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
        if (cachedTransform == null) return;

        Vector3 acceleration = Vector3.zero;

        //if (reachTarget)
        //{
        //    target = player;
        //}

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

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
    }

    bool IsHeadingForCollision()
    {
        RaycastHit hit;
        if (Physics.SphereCast(position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask))
        {
            return true;
        }
        else { }
        return false;
    }

    Vector3 ObstacleRays()
    {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++)
        {
            Vector3 dir = cachedTransform.TransformDirection(rayDirections[i]);
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

    //IEnumerator CheckDistanceTarget()
    //{
    //    while(!reachTarget)
    //    {
    //        if( Mathf.Abs(Vector3.SqrMagnitude(transform.position - target.position)) <= 0.1f )
    //        {
    //            reachTarget = true;
    //        }
    //        yield return null;
    //    }
        
    //}

    IEnumerator CheckDistanceTarget()
    {
        while (gameObject)
        {
           if(renderer.isVisible)
            {
                yield return new WaitForSeconds(1f);
                if(renderer.isVisible)
                {
                    this.target = player;
                    settings.maxSteerForce = 0.1f;
                    settings.targetWeight = 50f;
                    AllahuAkbar = true;
                }
            }
            else
            {
                yield return new WaitForSeconds(1);
                if(!renderer.isVisible)
                {
                    this.target = CircleBoid;
                    settings.maxSteerForce = 2f;
                    settings.targetWeight = 10f;
                    AllahuAkbar = false;
                }
            }
            settings.targetWeight += plusPriority;
            yield return null;
        }

    }


    IEnumerator AddPlusPriority()
    {
        while(gameObject)
        {
            if(AllahuAkbar)
            {
                plusPriority += 0.5f;
            }
            else
            {
                plusPriority = 0;
            }

            yield return null;
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if(other.gameObject.CompareTag("ShootingArea"))
    //    {
    //        reachTarget = true;
    //        settings.maxSteerForce = 1.5f;
    //        settings.targetWeight = 10f;
    //    }
    //}


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth.DecrementHealth(inmolationDamge, Enemy.Immolate);
            ThisBoidIsDead();
            // TODOOOOOOOO

        }
        else if(collision.gameObject.CompareTag("Mountain"))
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
        Invoke("DestroyEnemy", 3f);
    }


    private void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }

}