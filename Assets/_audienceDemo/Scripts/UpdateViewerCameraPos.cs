using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class UpdateViewerCameraPos : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform target;
    // Update is called once per frame
    private Vector3 oriPos;
    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        oriPos = transform.position;
    }
    void Update()
    {
        if (SceneManager.GetActiveScene().name == "TestScene")
        {
            if (target == null)
            {
                GameObject[] tmp = GameObject.FindGameObjectsWithTag("PlayerRoot");
                target = tmp[0].gameObject.transform;
            }
            else
            {
                this.transform.position = new Vector3(
                    target.position.x + oriPos.x,
                    target.position.y + oriPos.y,
                    target.position.z + oriPos.z);
            }
        }
        else
        {
            target = null;
        }

    }
}