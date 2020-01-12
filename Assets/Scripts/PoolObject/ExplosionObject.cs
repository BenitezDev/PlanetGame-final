using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionObject : MonoBehaviour
{
    public void OnParticleSystemStopped()
    {
        Debug.Log("Stop");
        PoolManager.ReleaseObject(this.gameObject);
    }
}
