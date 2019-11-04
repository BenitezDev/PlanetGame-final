
using UnityEngine;

public class ShipVisuals : MonoBehaviour
{


    [Header("Particle systems")]
    public ParticleSystem small;
    public ParticleSystem medium;

    protected virtual void Start()
    {

        small.Play();
    }

    protected void Update()
    {
        // pulsa W
        if (Input.GetAxis("Right Stick Vertical 1") > 0 && FuelManager.currentFuel > 0)
        {
            StartBoost();
        }
        else if (Input.GetAxis("Right Stick Vertical 1") <= 0)
        {
            StopBoost();
        }

    }

    protected virtual void StartBoost()
    {
        medium.Play();

    }

    protected virtual void StopBoost()
    {
        medium.Stop();

    }
}
