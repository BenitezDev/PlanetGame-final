using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidTargetPriority : MonoBehaviour
{
    public BoidSettings boidSettings;
    public float minTargetWeight = 5;
    public float maxTargetWeight = 50;
    private void OnTriggerEnter(Collider other)
    {
        boidSettings.targetWeight = minTargetWeight;
    }

    private void OnTriggerExit(Collider other)
    {
        boidSettings.targetWeight = maxTargetWeight;
    }
}
