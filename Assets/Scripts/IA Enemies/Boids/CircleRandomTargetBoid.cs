using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleRandomTargetBoid : MonoBehaviour
{



    [SerializeField] private float radius = 10;
    [SerializeField] private Transform target;



    private void Start()
    {
        StartCoroutine(ChangeTarget());
    }


    IEnumerator ChangeTarget()
    {
        while (gameObject.activeSelf)
        {
            target.localPosition = Random.insideUnitSphere * radius;
            yield return new WaitForSeconds(Random.Range(3f,4f));
        }
    }

    public void ChangeTargetPos()
    {
        target.localPosition = Random.insideUnitSphere * radius;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
