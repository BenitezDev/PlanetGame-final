using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class ObjData
{
    public Vector3 pos;
    public Quaternion rot;
    public Vector3 scale;

    public Matrix4x4 matrix
    {
        get
        {
            return Matrix4x4.TRS(pos, rot, scale);
        }
    }

    public ObjData(Vector3 pos, Quaternion rot, Vector3 scale)
    {
        this.pos = pos;
        this.rot = rot;
        this.scale = scale;
    }


}

public class SpawnerGPU : MonoBehaviour
{
    public int instances;
    public Vector3 maxPos;

    public Mesh objMesh;
    public Material objMat;

    private List<List<ObjData>> batches = new List<List<ObjData>>();


    private void Start()
    {
        int batchIndexNum = 0;
        List<ObjData> currBatch = new List<ObjData>();

        for (int i = 0; i < instances; ++i)
        {
            AddObj(currBatch, i);
            batchIndexNum++;

            if(batchIndexNum >= 1000)
            {
                batches.Add(currBatch);
                currBatch = BuildNewBatch();
                batchIndexNum = 0;
            }

        }

    }

    private void Update()
    {
        RenderBatches();
    }

    private void RenderBatches()
    {
        foreach(var batch in batches)
        {
            Graphics.DrawMeshInstanced(objMesh, 0, objMat, batch.Select((a) => a.matrix).ToList());
        }
    }

    private void AddObj(List<ObjData> currBatch, int i)
    {
        Vector3 position = new Vector3(Random.Range(-maxPos.x, maxPos.x), Random.Range(-maxPos.y, maxPos.y), Random.Range(-maxPos.z, maxPos.z));
        currBatch.Add(new ObjData(position, Quaternion.identity, Vector3.one * Random.Range(3.0f, 10.0f)));
    }

    private List<ObjData> BuildNewBatch()
    {
        return new List<ObjData>();
    }

}
