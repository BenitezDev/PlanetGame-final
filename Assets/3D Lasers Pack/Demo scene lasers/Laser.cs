using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters;
using System;
using UnityEngine;

public class Laser : MonoBehaviour
{
    
    public float HitOffset = 0;

    public float MaxLength;
    

    public float MainTextureLength = 1f;
    public float NoiseTextureLength = 1f;
    private Vector4 Length = new Vector4(1, 1, 1, 1);
    
    
    public ParticleSystem Ps;
    private bool UpdateSaver = false;


    void Start()
    {
        if(Ps != null)

            Ps = GetComponent<ParticleSystem>();
        //if (Laser.material.HasProperty("_SpeedMainTexUVNoiseZW")) LaserStartSpeed = Laser.material.GetVector("_SpeedMainTexUVNoiseZW");
        //Save [1] and [3] textures speed
        //{ DISABLED AFTER UPDATE}
        //LaserSpeed = LaserStartSpeed;
    }

    //void Update()
    //{
    //    //if (Laser.material.HasProperty("_SpeedMainTexUVNoiseZW")) Laser.material.SetVector("_SpeedMainTexUVNoiseZW", LaserSpeed);
    //    //SetVector("_TilingMainTexUVNoiseZW", Length); - old code, _TilingMainTexUVNoiseZW no more exist
    //    //Laser.material.SetTextureScale("_MainTex", new Vector2(Length[0], Length[1]));                    
    //    //Laser.material.SetTextureScale("_Noise", new Vector2(Length[2], Length[3]));
    //    //To set LineRender position
    //    // if (Laser != null && UpdateSaver == false)
    //    {
    //        //   Laser.SetPosition(0, transform.position);
    //        RaycastHit hit; //DELATE THIS IF YOU WANT USE LASERS IN 2D
    //        //ADD THIS IF YOU WANNT TO USE LASERS IN 2D: RaycastHit2D hit = Physics2D.Raycast(transform.position, transform.forward, MaxLength);       
    //        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, MaxLength))//CHANGE THIS IF YOU WANT TO USE LASERRS IN 2D: if (hit.collider != null)
    //        {
               
    //            foreach (var AllPs in Effects)
    //            {
    //                if (!AllPs.isPlaying) AllPs.Play();
    //            }
    //            //Texture tiling
    //            Length[0] = MainTextureLength * (Vector3.Distance(transform.position, hit.point));
    //            Length[2] = NoiseTextureLength * (Vector3.Distance(transform.position, hit.point));
    //            //Texture speed balancer {DISABLED AFTER UPDATE}
    //            //LaserSpeed[0] = (LaserStartSpeed[0] * 4) / (Vector3.Distance(transform.position, hit.point));
    //            //LaserSpeed[2] = (LaserStartSpeed[2] * 4) / (Vector3.Distance(transform.position, hit.point));
    //        }
    //        else
    //        {
    //            //End laser position if doesn't collide with object
    //            var EndPos = transform.position + transform.forward * MaxLength;
    //            //   Laser.SetPosition(1, EndPos);
    //            HitEffect.transform.position = EndPos;
    //            foreach (var AllPs in Effects)
    //            {
    //                if (!AllPs.isPlaying) AllPs.Play();
    //            }
    //            //Texture tiling
    //            Length[0] = MainTextureLength * (Vector3.Distance(transform.position, EndPos));
    //            Length[2] = NoiseTextureLength * (Vector3.Distance(transform.position, EndPos));
    //            //LaserSpeed[0] = (LaserStartSpeed[0] * 4) / (Vector3.Distance(transform.position, EndPos)); {DISABLED AFTER UPDATE}
    //            //LaserSpeed[2] = (LaserStartSpeed[2] * 4) / (Vector3.Distance(transform.position, EndPos)); {DISABLED AFTER UPDATE}
    //        }
    //        //Insurance against the appearance of a laser in the center of coordinates!
    //        //if (Laser.enabled == false && LaserSaver == false)
    //        //{
    //        //    LaserSaver = true;
    //        //    Laser.enabled = true;
    //        //}
    //    }
    //}
    public void Shoot()
    {
        Ps.Play();
    }

    public void DisablePrepare()
    {
        Ps.Stop();
    }


}
