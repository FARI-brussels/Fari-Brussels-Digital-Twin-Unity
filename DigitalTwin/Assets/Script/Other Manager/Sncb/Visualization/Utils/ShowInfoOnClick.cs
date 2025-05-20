using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShowInfoOnClickSncb : MonoBehaviour
{
    //Camera follow script
    private CameraFollowOnClickSncb cameraFollow;

    private object _feature;

    public void Initialize( object feature)
    {
        _feature = feature;
    }

    private void Start()
    {
        //Instatiate the main camera
        cameraFollow = Camera.main.GetComponent<CameraFollowOnClickSncb>();
    }

    private void OnMouseDown()
    {
        Debug.Log("Hey");
        if (cameraFollow != null)
        {
            cameraFollow.Initialize(_feature);
            cameraFollow.SetTarget(transform);
        }
    }
}
