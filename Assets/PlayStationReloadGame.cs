using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayStationReloadGame : MonoBehaviour
{
    public static RectTransform tr;
    public static float speed = 200f;
    public float jajajaja = 10f;


    private void Awake()
    {

        tr = GameObject.FindWithTag("FakeCursor").GetComponent<RectTransform>();
    }

    private void Update()
    {
        float h = Input.GetAxis("Left Stick Horizontal 1");
        float v = Input.GetAxis("Left Stick Vertical 1");

        tr.position = new Vector3(h*speed + Screen.width / 2, v*speed + +Screen.height / 2, 0);

      
        if (Input.GetButtonDown("Start 1")) UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}
