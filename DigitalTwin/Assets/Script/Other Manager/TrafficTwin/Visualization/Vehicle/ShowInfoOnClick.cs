using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class ShowInfoOnClick : MonoBehaviour
{
    //Camera follow script
    private CameraFollowOnClick cameraFollow;
    private TripInfo _vehicleTripInfo;

    public void Initialize(TripInfo vehicleTripInfo)
    {
        _vehicleTripInfo = vehicleTripInfo;
    }

    private void Start()
    {
        //Instatiate the main camera
        cameraFollow = Camera.main.GetComponent<CameraFollowOnClick>();
    }

    private void OnMouseDown()
    {
        if (cameraFollow != null)
        {
            cameraFollow.Initialize(_vehicleTripInfo);
            cameraFollow.SetTarget(transform);
            TrafficTwinManager.Instance.SetActiveByName("Canvas Simulation", false);
        }
    }
}
