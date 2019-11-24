using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidRocketLauncher : MonoBehaviour {

    public BoidSettingsRocketLauncher settings;

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
    public Transform target;

    Transform nearPlayer;
    Transform finalPos;

    [SerializeField] private GameObject explosionPS;

    public Renderer renderer;
    private EnemyShootRocket shootRocket;

    

    void Awake () {
        

        

        shootRocket = GetComponent<EnemyShootRocket>();
        material = transform.GetComponentInChildren<MeshRenderer> ().material;
        renderer = GetComponentInChildren<Renderer>();
        cachedTransform = transform;
        explosionPS = GetComponent<EnemyHealth>().explosionPS;

    }

    public void Initialize (BoidSettingsRocketLauncher settings, Transform target, Transform nearPlayer)
    {
        finalPos = target;
        this.target = target;
        this.settings = Instantiate(settings);
        this.nearPlayer = nearPlayer;

        position = cachedTransform.position;
        forward = cachedTransform.forward;

        

        float startSpeed = (settings.minSpeed + settings.maxSpeed) / 2;
        velocity = transform.forward * startSpeed;

        StartCoroutine(Behaviour());

    }

    public void SetColour (Color col) {
        if (material != null) {
            material.color = col;
        }
    }

    public void UpdateBoid () {
        if (cachedTransform == null) return;

        Vector3 acceleration = Vector3.zero;

        if (target != null) {
            Vector3 offsetToTarget = (target.position - position);
            acceleration = SteerTowards (offsetToTarget) * settings.targetWeight;
        }

        if (numPerceivedFlockmates != 0) {
            centreOfFlockmates /= numPerceivedFlockmates;

            Vector3 offsetToFlockmatesCentre = (centreOfFlockmates - position);

            var alignmentForce = SteerTowards (avgFlockHeading) * settings.alignWeight;
            var cohesionForce = SteerTowards (offsetToFlockmatesCentre) * settings.cohesionWeight;
            var seperationForce = SteerTowards (avgAvoidanceHeading) * settings.seperateWeight;

            acceleration += alignmentForce;
            acceleration += cohesionForce;
            acceleration += seperationForce;
        }

        if (IsHeadingForCollision ()) {
            Vector3 collisionAvoidDir = ObstacleRays ();
            Vector3 collisionAvoidForce = SteerTowards (collisionAvoidDir) * settings.avoidCollisionWeight;
            acceleration += collisionAvoidForce;
        }

        velocity += acceleration * Time.deltaTime;
        float speed = velocity.magnitude;
        Vector3 dir = velocity / speed;
        speed = Mathf.Clamp (speed, settings.minSpeed, settings.maxSpeed);
        velocity = dir * speed;

        cachedTransform.position += velocity * Time.deltaTime;
        cachedTransform.forward = dir;
        position = cachedTransform.position;
        forward = dir;
    }

    bool IsHeadingForCollision () {
        RaycastHit hit;
        if (Physics.SphereCast (position, settings.boundsRadius, forward, out hit, settings.collisionAvoidDst, settings.obstacleMask)) {
            return true;
        } else { }
        return false;
    }

    Vector3 ObstacleRays () {
        Vector3[] rayDirections = BoidHelper.directions;

        for (int i = 0; i < rayDirections.Length; i++) {
            Vector3 dir = cachedTransform.TransformDirection (rayDirections[i]);
            Ray ray = new Ray (position, dir);
            if (!Physics.SphereCast (ray, settings.boundsRadius, settings.collisionAvoidDst, settings.obstacleMask)) {
                return dir;
            }
        }

        return forward;
    }

    Vector3 SteerTowards (Vector3 vector) {
        Vector3 v = vector.normalized * settings.maxSpeed - velocity;
        return Vector3.ClampMagnitude (v, settings.maxSteerForce);
    }


    IEnumerator Behaviour()
    {
        while(gameObject)
        {
            if (renderer.isVisible)
            {
                yield return new WaitForSeconds(2f);
                if(renderer.isVisible)
                {
                    shootRocket.ShootRocket();
                    yield return new WaitForSeconds(2f);
                    GetRandomPointNearPlayer();
                    target = nearPlayer;
                }
            }
            else
            {
                yield return new WaitForSeconds(2f);
                if(!renderer.isVisible)
                {
                    target = finalPos;
                }
            }
            yield return null;
        }
    }

    private void GetRandomPointNearPlayer()
    {
        nearPlayer.position += Random.insideUnitSphere * 30f;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("inmolacion realizad!");

            transform.gameObject.SetActive(false);
            var ps = Instantiate(explosionPS, transform.position, Quaternion.identity);
            Destroy(ps, 3f);
            Invoke("DestroyEnemy", 3f);
            // TODOOOOOOOO
        }
        else if (collision.gameObject.CompareTag("Mountain"))
        {
            Debug.Log("Se la ha pegado el rocketLauncher con " + collision.gameObject.name );

            transform.gameObject.SetActive(false);
            var ps = Instantiate(explosionPS, transform.position, Quaternion.identity);
            Destroy(ps, 3f);
            Invoke("DestroyEnemy", 3f);
            // TODOOOO
        }

        Debug.Log(collision.gameObject.name);
    }

    private void DestroyEnemy()
    {
        Destroy(this.gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(nearPlayer.position, 1);
    }
}
